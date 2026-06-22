using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.Rendering.UI;

public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed;
    [SerializeField] private float defaultSprintSpeed;
    [SerializeField] private float climbSpeed;
    [SerializeField] private float slideStrength = 8f;

    [Tooltip("Larger this is, the faster the player slows down from weight. Must be < 1")]
    [SerializeField] private float weightMultiplier = 0.75f;

    [Space(10)]

    [Header("Crouch Settings")]
    [SerializeField] private float crouchSpeed;

    [Tooltip("Controller heights when crouched/uncrouched, 0 is standing, 1 is crouched")]
    [SerializeField] private float[] crouchHeights;

    [Tooltip("Camera y pos's when crouched/uncrouched, 0 is standing, 1 is crouched")]
    [SerializeField] private float[] crouchCameraY;
    [SerializeField] private Transform camHolder;

    [Tooltip("How much smaller the player gets when crouching")]
    [SerializeField] private float crouchScaleY;

    [Space(10)]

    [Header("Stamina Settings")]

    [Tooltip("The max stamina - the y intercept")]
    public float maxStamina;
    
    [Tooltip("Delay before stamina starts regenerating")]
    [SerializeField] private float regainStaminaDelay;

    [Tooltip("Change in stamina per second when losing stamina")]
    [SerializeField] private float staminaDrainRate;

    [Tooltip("Change in stamina per second when gaining stamina")]
    [SerializeField] private float staminaRegenRate;
    [SerializeField] private float sliderSmoothSpeed = 10f;

    [Space(10)]

    [Header("Jump Settings")]
    [SerializeField] private float jumpPower = 40f;
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private float fallMultiplier;
    [SerializeField] private float jumpStaminaLossMultiplier;

    [Space(10)]

    [Header("Sound Effects")]
    [SerializeField] private AudioSource walkSourceSfx;
    [SerializeField] private AudioClip[] walkSfxs;
    [SerializeField] private AudioClip[] sprintSfxs;
    [SerializeField] private AudioClip jumpSfx;
    [SerializeField] private AudioClip landSfx;

    private CharacterController controller;

    private Vector3 moveDirection;
    private float currentStamina;
    [HideInInspector] public float smoothedSprintValue;
    
    private bool staminaDelayActive = false;
    private bool canRegainStamina = false;

    private bool canMove = true;
    private bool isSprinting;
    private bool isCrouching;
    private bool isMoving;
    private bool isFalling;
    private bool canPlayLandSfx;
    private float fallTime = 0f;
    private float walkSfxTimer;

    public bool inputEnabled = true;

    private PlayerCam cam;
    private bool isSteering;
    private SteeringWheel wheelInRange;
    private GameObject currentShip;
    private Vector3 lastShipPos;
    private Quaternion lastShipRot;

    private PlayerInventory inv;

    private Animator animator;
    private Ladder currentLadder;
    private float climbProgress;
    private bool onLadder = false;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        SceneEventBus.SceneChanged += RebindScene;
        
        RebindScene();
    }

    private void RebindScene()
    {
        PlayerUI ui = FindAnyObjectByType<PlayerUI>();
        
        if (ui != null)
        {
            ui.BindPlayer(this);
        }

        cam = GetComponent<PlayerCam>();
        inv = GetComponent<PlayerInventory>();
        animator = GetComponentInChildren<Animator>();
        controller = GetComponent<CharacterController>();

        currentStamina = maxStamina;
    }

    void Update()
    {
        if (!IsOwner) return;

        if (!onLadder) HandleMovement();

        HandleInput();

        HandleClimbing();
    }

    public void ToggleInput(bool enabled)
    {
        inputEnabled = enabled;
    }

    void HandleInput()
    {
        // Enables/disables steering depending on whether player is steering or not
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!isSteering && wheelInRange != null && inputEnabled)
            {
                wheelInRange.TrySteerShip(this);
            }
            else if (isSteering && currentShip != null)
            {
                currentShip.GetComponentInParent<ShipController>().StopSteerRpc(OwnerClientId);
            } else if (currentLadder != null)
            {
                if (onLadder)
                    ExitLadder();
                else EnterLadder();
            }
        }
    }

    void HandleMovement()
    {
        #region Handles Movement
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        // Left Shift to run, left control to crouch
        if (inputEnabled)
        {
            isSprinting = Input.GetKey(KeyCode.LeftShift);
            isCrouching = Input.GetKey(KeyCode.LeftControl);
        }

        // Ensures you don't do two movement techniques at once
        if (isSprinting) isCrouching = false;
        if (isCrouching) isSprinting = false;

        float sprintSpeed = defaultSprintSpeed;
        if (currentStamina <= 0.25f) sprintSpeed = walkSpeed;

        // Current speed is dependent on whether the player is sprinting/crouching (speed is then multiplied by input)
        float inputX = inputEnabled ? Input.GetAxis("Vertical") : 0;
        float inputZ = inputEnabled ? Input.GetAxis("Horizontal") : 0;

        float currentSpeedX = canMove ? (isSprinting ? sprintSpeed : isCrouching ? crouchSpeed : walkSpeed) 
                                            * inputX : 0;
        float currentSpeedZ = canMove ? (isSprinting ? sprintSpeed : isCrouching ? crouchSpeed : walkSpeed)
                                            * inputZ : 0;
        float movementDirectionY = moveDirection.y;
        
        // Weight Slowdown
        float slowdownFactor = 1 - (weightMultiplier * inv.weightPercent);

        moveDirection = (forward * currentSpeedX * slowdownFactor) + (right * currentSpeedZ * slowdownFactor);

        isMoving = currentSpeedX != 0 || currentSpeedZ != 0;

        #endregion

        #region Walk SFX
        
        if (isMoving && controller.isGrounded)
        {
            walkSfxTimer -= Time.deltaTime;
            if (walkSfxTimer <= 0)
            {
                // Changes audio depending on whether the player is sprinting, crouching, or walking
                if (isSprinting) 
                    walkSourceSfx.clip = sprintSfxs[Random.Range(0, sprintSfxs.Length)];
                else 
                    walkSourceSfx.clip = walkSfxs[Random.Range(0, walkSfxs.Length)];
                if (isCrouching)
                    walkSourceSfx.volume = 0.1f;
                else 
                    walkSourceSfx.volume = 0.3f;
    
                walkSfxTimer = walkSourceSfx.clip.length;
                walkSourceSfx.Play();
            }
        }
        else
        {
            walkSfxTimer = 0f;
            walkSourceSfx.Stop();
        }
        #endregion

        HandleCrouch();

        #region Handles Sprinting

        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);

        // Decreases stamina while sprinting
        if (isSprinting && currentStamina > 0f)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;

            if (isMoving) UpdateAnimator(false, false, true); // Sets anim to run
            else UpdateAnimator(true, false, false); // Sets anim to idle
        } 
        else
        {
            if (isMoving) UpdateAnimator(false, true, false); // Sets anim to walk
            else UpdateAnimator(true, false, false); // Sets anim to idle

            // Regains stamina after a short delay, stops if stamina has reached max
            if (!staminaDelayActive && currentStamina < maxStamina)
                StartCoroutine(RegainStaminaDelay());

            if (canRegainStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;

                // Stops gaining stamina when current has reached max
                if (currentStamina >= maxStamina)
                {
                    currentStamina = maxStamina;
                    canRegainStamina = false;
                }
            }
        }

        // Smoothly increases the stamina slider
        smoothedSprintValue = Mathf.Lerp(smoothedSprintValue, currentStamina, sliderSmoothSpeed * Time.deltaTime);

        #endregion

        #region Handles Slopes

        Vector3 slideDir;
        if (controller.isGrounded && IsOnSteepSlope(out slideDir))
        {
            moveDirection += slideDir * slideStrength;
        }

        #endregion

        #region Handles Jumping
        if (Input.GetButton("Jump") && canMove && controller.isGrounded && currentStamina >= 0.5f && inputEnabled)
        {
            moveDirection.y = jumpPower;
            currentStamina -= staminaDrainRate * jumpStaminaLossMultiplier;

            SoundManager.Instance.PlayAudio(jumpSfx, 1f, transform);
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }

        isFalling = !controller.isGrounded && moveDirection.y < 0; // if at the peak of the jump
        
        // Applies gravity when in air, increases speed if falling  
        if (!controller.isGrounded)
        {
            moveDirection.y -= gravity * (isFalling ? fallMultiplier : 1f) * Time.deltaTime;
            canPlayLandSfx = true;
            fallTime += Time.deltaTime;
        } else 
        {
            isFalling = false;
        }

        if (controller.isGrounded && canPlayLandSfx)
        {
            // Volume and shake mag. of land is dependent on how long the players been falling
            float landVolume = 0f;

            landVolume = Mathf.Clamp01(fallTime * 0.5f);
            SoundManager.Instance.PlayAudio(landSfx, landVolume, transform, 0);

            canPlayLandSfx = false;
            fallTime = 0f;
        }

        #endregion

        // Allows player to move with ship
        Vector3 shipDelta = currentShip != null ? GetShipMovementDelta() : Vector3.zero;
        shipDelta.y = 0f;

        controller.Move(moveDirection * Time.deltaTime);

        if (currentShip != null)
        {
            transform.position += shipDelta;
        }
    }

    void HandleCrouch()
    {
        // Gets target height/camera y pos depending on whether crouching or not
        float targetHeight = isCrouching ? crouchHeights[1] : crouchHeights[0];
        float targetCameraY = isCrouching ? crouchCameraY[1] : crouchCameraY[0];

        // Smoothly updates controller height
        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * 10f);

        // Smoothly moves the camera
        Vector3 camLocal = camHolder.localPosition;
        camLocal.y = Mathf.Lerp(camLocal.y, targetCameraY, Time.deltaTime * 10f);
        camHolder.localPosition = camLocal;
    }
    
    void HandleClimbing()
    {
        if (currentLadder == null || !onLadder) return;
        Debug.Log("climbing");

        float input = Input.GetAxis("Vertical");

        climbProgress += input * climbSpeed * Time.deltaTime;
        climbProgress = Mathf.Clamp01(climbProgress);

        transform.position = Vector3.Lerp(currentLadder.ladderBottom.position, currentLadder.ladderTop.position, climbProgress);

        if (climbProgress >= 1f || climbProgress <= 0f)
        {
            ExitLadder();
        }
    }

    IEnumerator RegainStaminaDelay()
    {
        float elapsedTime = 0f;
        staminaDelayActive = true;

        while (elapsedTime <= regainStaminaDelay)
        {
            if (isSprinting)
            {
                staminaDelayActive = false;
                break;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        staminaDelayActive = false;
        canRegainStamina = true;
    }

    /// Checks if the player is on a steep slope
    /// If so the player will slide down it (depending on direction)
    bool IsOnSteepSlope(out Vector3 slideDirection)
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 2f))
        {
            float angle = Vector3.Angle(hit.normal, Vector3.up);
            
            if (angle > controller.slopeLimit)
            {
                slideDirection = Vector3.ProjectOnPlane(Vector3.down, hit.normal);
                return true;
            }
        }

        slideDirection = Vector3.zero;
        return false;
    }

    void UpdateAnimator(bool isIdle, bool isWalking, bool isRunning)
    {
        animator.SetBool("isIdle", isIdle);
        animator.SetBool("isWalking", isWalking);
        animator.SetBool("isRunning", isRunning);
    }

    public void StartSteering()
    {
        isSteering = true;
        inputEnabled = false;

        cam.EnableThirdPerson();
    }

    public void StopSteering()
    {
        isSteering = false;
        inputEnabled = true;

        cam.EnableFirstPerson();
    }
    private void EnterLadder()
    {
        if (currentLadder == null) return;

        currentLadder.hasPlayer = true;
        climbProgress = 0.25f;
        inputEnabled = false;
        onLadder = true;
        controller.enabled = false;
        cam.EnableThirdPerson();
    }

    private void ExitLadder()
    {
        currentLadder.hasPlayer = false;
        currentLadder = null;
        onLadder = false;
        inputEnabled = true;
        controller.enabled = true;
        cam.EnableFirstPerson();
    }

    Vector3 GetShipMovementDelta()
    {
        if (currentShip == null) return Vector3.zero;
        
        // Gets change in pos and rot
        Vector3 positionDelta = currentShip.transform.position - lastShipPos;
        Quaternion rotationDelta = currentShip.transform.rotation * Quaternion.Inverse(lastShipRot);

        // Updates last pos and rot
        lastShipPos = currentShip.transform.position;
        lastShipRot = currentShip.transform.rotation;

        // Apply rotation around ship center
        Vector3 offset = transform.position - currentShip.transform.position;
        offset = rotationDelta * offset;

        Vector3 rotatedPosition = currentShip.transform.position + offset;

        return (rotatedPosition - transform.position) + positionDelta; 
    }

    private void OnTriggerEnter(Collider obj)
    {
        if (!IsOwner) return;

        if (obj.CompareTag("SteeringWheel"))
        {
            wheelInRange = obj.GetComponent<SteeringWheel>();
        }

        if (obj.CompareTag("Ship"))
        {
            currentShip = obj.gameObject;
            lastShipPos = currentShip.transform.position;
            lastShipRot = currentShip.transform.rotation;
        }

        if (obj.TryGetComponent<Ladder>(out var ladder))
        {
            currentLadder = ladder;
        }
    }

    private void OnTriggerExit(Collider obj)
    {
        if (obj.CompareTag("SteeringWheel"))
        {
            wheelInRange = null;
        }

        if (obj.CompareTag("Ship"))
        {
            currentShip = null;
        }

        if (obj.TryGetComponent<Ladder>(out Ladder ladder))
        {
            ExitLadder();
        }
    }
}