using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.UI;
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
    private NetworkVariable<float> totalProfit = new NetworkVariable<float>(); // Make this int later
    private List<NetworkObject> spawnedItems = new List<NetworkObject>();

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
        spawnedItems.Add(newItem.GetComponent<NetworkObject>());

        // COULD CHANGE THIS TO CAN COLLECT LATER SO THAT PLAYERS CAN REMOVE DELIVERED ITEMS
        newItem.GetComponent<Item>().canCollect = false;

        if (!fromGameManager)
            GameManager.Instance.AddDeliveredNommianRpc(itemId);
    }

    [Rpc(SendTo.Server)]
    public void FeedSnackenRpc()
    {
        if (spawnedItems.Count <= 0) return;

        QuotaManager.Instance.StartCoroutine(QuotaManager.Instance.CheckQuotaReached(spawnedItems.Count, totalProfit.Value));

        for (int i = spawnedItems.Count - 1; i >= 0; i++)
        {
            NetworkObject item = spawnedItems[i];
            item.Despawn();
            spawnedItems.Remove(item);
        }

        spawnedItems.Clear();
    }

    private void OnNommiansChanged(int previous, int current)
    {
        deliveredNommiansText.text = $"{totalNommians.Value}";
    }
}
