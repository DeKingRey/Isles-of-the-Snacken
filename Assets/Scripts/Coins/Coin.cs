using Unity.Netcode;
using UnityEngine;

public class Coin : NetworkBehaviour
{
    void OnTriggerEnter(Collider obj)
    {
        if (!IsServer) return; // Server authorative

        // Collects coin
        if (obj.CompareTag("Ship"))
        {
            QuotaManager.Instance.coinAmount--;
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
