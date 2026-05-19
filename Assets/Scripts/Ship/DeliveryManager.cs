using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class DeliveryManager : MonoBehaviour
{
    [SerializeField] private Transform deliverySpawnPoint;

    //private NetworkList<ItemData> items = new NetworkList<ItemData>();

    public void DeliverItem(ItemData data)
    {
        //items.Add(data);

        GameObject newItem = Instantiate(data.itemModel, deliverySpawnPoint, Quaternion.identity);
        newItem.GetComponent<NetworkObject>().Spawn();
    }
}
