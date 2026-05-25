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

    [Header("Drift Settings")]
    [SerializeField] private float driftMutliplier;
    [SerializeField] private float minSpeedFactor;

    [Space(10)]

    // Used to slow down ship when no input
    [Header("Drag Settings")]
    [SerializeField] private float dragSpeed;
    [SerializeField] private float targetDrag;

    private Rigidbody rb;
    private PlayerController currentPlayer;

    private float accelerationInput = 0f;
    private float steeringInput = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (steeringClientId.Value != NetworkManager.Singleton.LocalClientId)
            return;
        
        if (currentPlayer != null)
            currentPlayer.ToggleInput(false);
        float steer = Input.GetAxis("Horizontal");
        float accel = Input.GetAxis("Vertical");

        SubmitInputRpc(steer, accel);
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
        // Only server can run physics
        if (!IsServer)
            return;

        if (!HasDriver)
            return;

        HandleSailing();
        HandleSteering();
        ReduceDrift();
    }

    void HandleSailing()
    {
        // How much we are going forward
        float forwardVelocity = Vector3.Dot(transform.forward, rb.linearVelocity);

        float speedThreshold = 0.1f;
        bool isMoving = rb.linearVelocity.sqrMagnitude > speedThreshold * speedThreshold;

        // Limits max speed in forward direction
        if (forwardVelocity > maxSpeed && accelerationInput > 0) return;

        // Limits max speed in backwards direction
        if (forwardVelocity < -maxSpeed * 0.5f && accelerationInput < 0) return;

        // Limits max speed in any direction
        if (rb.linearVelocity.sqrMagnitude > maxSpeed * maxSpeed && accelerationInput > 0) return;

        // Slows down ship if no input
        if (accelerationInput == 0) rb.linearDamping = Mathf.Lerp(rb.linearDamping, targetDrag, dragSpeed * Time.deltaTime);
        else rb.linearDamping = 0;

        Vector3 sailForce = transform.forward * accelerationInput * acceleration;

        // Applies force, pushing ship forward
        rb.AddForce(sailForce, ForceMode.Force);
    }

    void HandleSteering()
    {
        // Limits turning ability when going slow
        float minTurnSpeed = rb.linearVelocity.magnitude / minSpeedFactor;
        minTurnSpeed = Mathf.Clamp01(minTurnSpeed);

        float turn = steeringInput * turnSpeed * minTurnSpeed * Time.fixedDeltaTime;

        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turn, 0f));
    }

    void ReduceDrift()
    {
        Vector3 forwardVelocity = transform.forward * Vector3.Dot(rb.linearVelocity, transform.forward);
        Vector3 rightVelocity = transform.right * Vector3.Dot(rb.linearVelocity, transform.right); 

        rb.linearVelocity = forwardVelocity + rightVelocity * driftMutliplier;
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
