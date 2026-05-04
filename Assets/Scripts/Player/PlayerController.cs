using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed;
    [SerializeField] private float defaultSprintSpeed;
    [SerializeField] private float slideStrength = 8f;

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
    [SerializeField] private float maxStamina;
    
    [Tooltip("Delay before stamina starts regenerating")]
    [SerializeField] private float regainStaminaDelay;

    [Tooltip("Change in stamina per second when losing stamina")]
    [SerializeField] private float staminaDrainRate;

    [Tooltip("Change in stamina per second when gaining stamina")]
    [SerializeField] private float staminaRegenRate;
    [SerializeField] private float sliderSmoothSpeed = 10f;

    [Space(10)]

    [Header("Jump Settings")]
    [SerializeField] private float jumpMultiplier = 40f;
    [SerializeField] private float maxJumpTime = 0.25f;
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

    private Slider staminaSlider; 
    private float currentStamina;
    [HideInInspector] public float smoothedSprintValue;
    
    private bool staminaDelayActive = false;
    private bool canRegainStamina = false;

    private bool canMove = true;
    private bool isSprinting;
    private bool isCrouching;
    private bool isMoving;
    private bool isJumping;
    private bool isFalling;

    private float jumpPower;
    private bool canPlayLandSfx;
    private float fallTime = 0f;
    private float sprintTime = 0f;
    private float walkSfxTimer;

    private bool inputEnabled = true;

    private bool isSteering;
    private SteeringWheel wheelInRange;
    private ShipController currentShip;
    private Vector3 lastShipPos;
    private Quaternion lastShipRot;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        
        PlayerUI ui = FindAnyObjectByType<PlayerUI>();
        ui.BindPlayer(this);
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();

        currentStamina = maxStamina;
    }

    void Update()
    {
        if (!IsOwner) return;

        HandleMovement();

        HandleInput();
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
                currentShip.StopSteerRpc(OwnerClientId);
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
        moveDirection = (forward * currentSpeedX) + (right * currentSpeedZ);

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

            sprintTime += Time.deltaTime;
        } 
        else
        {
            sprintTime = 0f;

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
            isJumping = true;
            SoundManager.Instance.PlayAudio(jumpSfx, 1f, transform);
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }

        // Lets player hold jump to jump higher
        if (isJumping)
        {
            jumpPower += Time.deltaTime;
            moveDirection.y = jumpPower * jumpMultiplier;

            currentStamina -= staminaDrainRate * jumpStaminaLossMultiplier * Time.deltaTime;

            if (jumpPower >= maxJumpTime || !Input.GetButton("Jump") || currentStamina <= 0.25f)
            {
                isFalling = true;
                isJumping = false;
                jumpPower = 0;
            }
        }
        
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
        Vector3 shipDelta = isSteering && currentShip != null ? GetShipMovementDelta() : Vector3.zero;
        moveDirection += shipDelta / Time.deltaTime;

        controller.Move(moveDirection * Time.deltaTime);
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

    public void StartSteering(ShipController ship)
    {
        isSteering = true;
        inputEnabled = false;
        currentShip = ship;

        lastShipPos = ship.transform.position;
        lastShipRot = ship.transform.rotation;
    }

    public void StopSteering()
    {
        isSteering = false;
        inputEnabled = true;
        currentShip = null;
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
        if (obj.CompareTag("SteeringWheel"))
        {
            wheelInRange = obj.GetComponent<SteeringWheel>();
        }
    }

    private void OnTriggerExit(Collider obj)
    {
        if (obj.CompareTag("SteeringWheel"))
        {
            wheelInRange = null;
        }
    }
}