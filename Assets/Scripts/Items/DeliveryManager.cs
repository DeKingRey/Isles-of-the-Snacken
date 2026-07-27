using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Security.Cryptography;
using TMPro;

public class DeliveryManager : NetworkBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Transform deliverySpawnPoint;
    public GameObject deliveryUI;
    public Image progressRing;

    [Header("Tracker")]
    [Tooltip("How many nommians are currently on the ship")]
    [SerializeField] private TextMeshProUGUI deliveredNommiansText;
    
    private NetworkVariable<int> totalNommians = new(0);
    private NetworkVariable<float> totalProfit = new NetworkVariable<float>();

    public override void OnNetworkSpawn()
    {
        totalNommians.OnValueChanged += OnNommiansChanged;
    }   

    public override void OnNetworkDespawn()
    {
        totalNommians.OnValueChanged -= OnNommiansChanged;
    }  

    [Rpc(SendTo.Server)]
    public void DeliverItemRpc(int itemId, bool fromGameManager = false)
    {
        ItemData item = GameManager.Instance.itemDatabase[itemId];
        totalProfit.Value += item.value;
        totalNommians.Value ++;

        GameObject newItem = Instantiate(item.itemModel, deliverySpawnPoint.position, Quaternion.identity);
        newItem.GetComponent<NetworkObject>().Spawn();

        // COULD CHANGE THIS TO CAN COLLECT LATER SO THAT PLAYERS CAN REMOVE DELIVERED ITEMS
        newItem.GetComponent<Item>().canCollect = false;

        if (!fromGameManager)
            GameManager.Instance.AddDeliveredNommianRpc(itemId);
    }

    [Rpc(SendTo.Server)]
    public void FeedSnackenRpc()
    {
        
    }

    private void OnNommiansChanged(int previous, int current)
    {
        deliveredNommiansText.text = $"{totalNommians.Value}";
    }
}
