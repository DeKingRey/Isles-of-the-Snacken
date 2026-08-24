using UnityEngine;
using Unity.Netcode;

public class ShipController : NetworkBehaviour
{
    public NetworkVariable<ulong> steeringClientId = 
        new NetworkVariable<ulong>(
            ulong.MaxValue,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public bool HasDriver => steeringClientId.Value != ulong.MaxValue;

    [Header("Movement Settings")]
    [SerializeField] private float acceleration;
    [SerializeField] private float turnSpeed;
    [SerializeField] private float maxSpeed;

    [Space(10)]

    [Header("Turning Settings")]
    [SerializeField] private float driftMutliplier;
    [Tooltip("The min speed at which the ship will have the max turning potential")]
    [SerializeField] private float minSpeedFactor;

    [Space(10)]

    // Used to slow down ship when no input
    [Header("Drag Settings")]
    [SerializeField] private float dragSpeed;
    [SerializeField] private float targetDrag;

    [Space(10)]

    [Header("References")]
    [SerializeField] private Transform steerPosition;

    [Space(10)]
    [Header("Island Collision")]
    [SerializeField] private float collisionRadius = 20f;
    [SerializeField] private LayerMask islandLayer;

    private PlayerController currentPlayer;

    private float currentSpeed = 0f;
    private float accelerationInput = 0f;
    private float steeringInput = 0f;
    private float cachedAccelInput = 0f;
    private float cachedSteerInput = 0f;

    void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return;

        if (steeringClientId.Value != NetworkManager.Singleton.LocalClientId)
            return;
        
        if (currentPlayer != null)
            currentPlayer.ToggleInput(false);

        cachedSteerInput = Input.GetAxis("Horizontal");
        cachedAccelInput = Input.GetAxis("Vertical");
    }

    [Rpc(SendTo.Server)]
    private void SubmitInputRpc(float steer, float accel, RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != steeringClientId.Value)
            return;
        
        steeringInput = steer;
        accelerationInput = accel;
    }

    void FixedUpdate()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return;

        if (GameManager.Instance.State.Value == GameManager.GameState.SnackenEating)
            return;
            
        // Validates input if the input comes from the current driver
        if (IsOwner && steeringClientId.Value == NetworkManager.Singleton.LocalClientId)
        {
            SubmitInputRpc(cachedSteerInput, cachedAccelInput);
        }

        // Only server can run movement
        if (!IsServer || !HasDriver)
            return;

        HandleSailing();
        HandleSteering();
    }

    void HandleSailing()
    {
        if (accelerationInput != 0)
        {
            // Increases speed
            currentSpeed += accelerationInput * acceleration * Time.fixedDeltaTime;
        } else
        {
            // Decreases speed when no accel
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, targetDrag * Time.fixedDeltaTime);
        }

        // The ship can only go half as fast backwards as it goes forward
        currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed * 0.5f, maxSpeed);

        Vector3 movement = transform.forward * currentSpeed * Time.fixedDeltaTime;

        // Moves the ship
        if (CanMove(movement))
        {
            transform.position += transform.forward * currentSpeed * Time.fixedDeltaTime;
        }
    }

    void HandleSteering()
    {
        // How fast the ship turns is based on speed 
        float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / minSpeedFactor);

        float turn = steeringInput * turnSpeed * speedFactor * Time.fixedDeltaTime;

        transform.Rotate(0f, turn, 0f);
    }

    // Determines whether the ship is about to hit an island
    private bool CanMove(Vector3 movement)
    {
        if (movement.sqrMagnitude < 0.0001f)
            return true;
        
        float distance = movement.magnitude;

        return !Physics.SphereCast(transform.position, collisionRadius, movement.normalized, out RaycastHit hit, distance, islandLayer, QueryTriggerInteraction.Ignore);
    }

    #region Enable Steering
    public bool CanClientSteer(ulong clientId)
    {
        return steeringClientId.Value == clientId;
    }

    [Rpc(SendTo.Server)]
    public void RequestSteerRpc(ulong clientId)
    {
        if (HasDriver) return; // Ship is already being steered

        steeringClientId.Value = clientId;

        // Calls the start steering rpc on just the client
        ClientStartSteeringRpc(RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.Server)]
    public void StopSteerRpc(ulong clientId)
    {
        if (steeringClientId.Value != clientId) return; // Ship being steered

        steeringClientId.Value = ulong.MaxValue;

        // Calls the start steering rpc on just the client
        ClientStopSteeringRpc(RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void ClientStartSteeringRpc(RpcParams rpcParams = default)
    {
        currentPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerController>();
        currentPlayer.transform.position = steerPosition.position;
        currentPlayer.StartSteering();
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void ClientStopSteeringRpc(RpcParams rpcParams = default)
    {
        currentPlayer.StopSteering();
        currentPlayer = null;
    }
    #endregion
}
