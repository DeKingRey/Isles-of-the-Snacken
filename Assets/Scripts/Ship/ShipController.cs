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

    void Update()
    {
        if (steeringClientId.Value != NetworkManager.Singleton.LocalClientId)
            return;
    }

    public bool CanClientSteer(ulong clientId)
    {
        return steeringClientId.Value == clientId;
    }

    [ServerRpc]
    public void RequestSteerServerRpc(ulong clientId)
    {
        if (HasDriver) return; // Ship is already being steered

        steeringClientId.Value = clientId;
        StartSteeringClientRpc(clientId);
    }

    [ServerRpc]
    public void StopSteerServerRpc(ulong clientId)
    {
        if (steeringClientId.Value != clientId) return; // Ship being steered

        steeringClientId.Value = ulong.MaxValue;

        StopSteeringClientRpc(clientId);
    }

    [ClientRpc]
    public void StartSteeringClientRpc(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId) return;

        var player = FindAnyObjectByType<PlayerController>();
        player.StartSteering(this);
    }

    [ClientRpc]
    public void StopSteeringClientRpc(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId) return;

        var player = FindAnyObjectByType<PlayerController>();
        player.StopSteering();
    }
}
