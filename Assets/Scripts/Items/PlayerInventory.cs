using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

public class PlayerInventory : NetworkBehaviour
{
    [HideInInspector] public float weightPercent; // Current weight / total weight

    [SerializeField] private int maxCapacity = 1;
    [SerializeField] private float maxWeight = 5f;

    private List<ItemData> items = new List<ItemData>();
    private int capacity = 0;
    public float currentWeight = 0;

    private DeliveryManager deliveryManager;
    private PlayerUI ui;
    private Interactable interaction;
    
    private Camera cam;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        cam = GetComponentInChildren<Camera>();

        SceneEventBus.SceneChanged += RebindScene;
        
        RebindScene();
    }

    private void RebindScene()
    {
        // Assigns interaction
        interaction = GetComponent<Interactable>();
        interaction.OnInteractComplete += DeliverItems;
        
        ui = FindAnyObjectByType<PlayerUI>();
        
        if (ui != null)
        {
            ui.BindInventory(this);
        }
    }

    public void ResetInventory()
    {
        // Remove all stored items
        items.Clear();

        capacity = 0;
        currentWeight = 0f;
        weightPercent = 0f;

        // Reset scene-specific references
        deliveryManager = null;

        if (interaction != null)
            interaction.canInteract = false;
    }

    void Update()
    {
        if (!IsOwner) return;
        weightPercent = currentWeight / maxWeight;

        if (deliveryManager == null) return;

        // Can only interact if the player has items
        if (capacity > 0)
        {
            interaction.canInteract = true;
        }
        else
        {
            interaction.canInteract = false;
            return;
        }
    }

    private void DeliverItems()
    {
        // Feeds the snacken upon interaction if within the Snacken
        if (SceneManager.GetActiveScene().name == "Snacken")
        {
            deliveryManager.FeedSnackenRpc();
            return;
        }

        int itemCount = items.Count;
        // Loop backwards to safely delete entries
        for (int i = itemCount - 1; i >= 0; i--)
        {
            int id = GameManager.Instance.GetItemId(items[i]);
            Debug.Log($"Delivering index {i}: {items[i].name}, ID: {id}");
            deliveryManager.DeliverItemRpc(id);
            RemoveItem(i);
        }
    }

    private void AddItem(ItemData data)
    {
        items.Add(data);

        if (IsOwner && ui != null)
        {
            ui.AddItemUI(data.itemSprite);
        }

        currentWeight += data.weight;
        capacity++;
    }

    public bool TryAddItem(ItemData data)
    {
        // Inventory is full
        if (capacity + 1 > maxCapacity || currentWeight + data.weight > maxWeight)
            return false;
        
        AddItem(data);
        return true;
    }

    public void RemoveItem(int dataIndex)
    {
        currentWeight -= items[dataIndex].weight;
        capacity--;

        if (IsOwner && ui != null)
        {
            ui.RemoveItemUI(dataIndex);
        }

        items.RemoveAt(dataIndex);
    }

    public void DropItem(int index)
    {
        int id = GameManager.Instance.GetItemId(items[index]);
        RemoveItem(index);
        
        ItemData item = GameManager.Instance.itemDatabase[id];

        GameObject newItem = Instantiate(item.itemModel, transform.position, Quaternion.identity);
        newItem.GetComponent<NetworkObject>().Spawn();

        // COULD CHANGE THIS TO CAN COLLECT LATER SO THAT PLAYERS CAN REMOVE DELIVERED ITEMS
        newItem.GetComponent<Item>().canCollect = true;
    }

    void OnTriggerEnter(Collider obj)
    {
        if (obj.CompareTag("DeliveryPoint"))
        {
            deliveryManager = obj.GetComponentInParent<DeliveryManager>();
            interaction.AssignVariables(deliveryManager.deliveryUI, deliveryManager.progressRing, cam);
        }
    }

    void OnTriggerExit(Collider obj)
    {
        if (obj.CompareTag("DeliveryPoint"))
        {
            deliveryManager = null;
        }
    }
}
