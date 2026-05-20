using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.UI;

public class DeliveryManager : NetworkBehaviour
{
    [SerializeField] private Transform deliverySpawnPoint;
    public GameObject deliveryUI;
    public Image progressRing;

    private NetworkVariable<float> totalProfit = new NetworkVariable<float>();

    [Rpc(SendTo.Server)]
    public void DeliverItemRpc(int itemId)
    {
        ItemData item = GameManager.Instance.itemDatabase[itemId];
        totalProfit.Value += item.value;

        GameObject newItem = Instantiate(item.itemModel, deliverySpawnPoint.position, Quaternion.identity);
        newItem.GetComponent<NetworkObject>().Spawn();
    }
}
