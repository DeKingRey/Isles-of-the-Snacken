using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.UI;

public class PlayerBag : NetworkBehaviour
{
    [SerializeField] private int maxCapacity = 1;

    [Header("Delivery Settings")]
    [SerializeField] private float deliveryHoldTime = 1f;
    [SerializeField] private float rayRadius = 0.5f;
    [SerializeField] private float rayDistance = 5f;
    [SerializeField] private LayerMask deliveryLayer;

    [Space(5)]

    [SerializeField] private GameObject deliveryUI;
    [SerializeField] private Image progressRing;

    private List<ItemData> items = new List<ItemData>();
    private int capacity = 0;
    private float totalWeight = 0;

    private DeliveryManager deliveryManager;
    private float elapsedHoldTime = 0f;

    private Camera cam;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            cam = GetComponentInChildren<Camera>();
        }
    }

    void Update()
    {
        if (deliveryManager == null || !IsOwner) return;

        deliveryUI = deliveryManager.deliveryUI;
        progressRing = deliveryManager.progressRing;

        HandleDelivery();
    }

    void HandleDelivery()
    {
        RaycastHit hit;

        // Only shows UI if the player has items to deliver
        if (capacity > 0)
        {
            deliveryUI.SetActive(true);
            progressRing.fillAmount = elapsedHoldTime / deliveryHoldTime;
        }
        else
        {
            deliveryUI.SetActive(false);
            return;
        }

        if (!Physics.SphereCast(cam.transform.position, rayRadius, cam.transform.forward, out hit, rayDistance, deliveryLayer))
        {
            elapsedHoldTime = 0f;
            return;
        }

        // Hold down to deliver contents of bag
        if (Input.GetKey(KeyCode.E))
        {
            elapsedHoldTime += Time.deltaTime;

            // Delivers contents
            if (elapsedHoldTime >= deliveryHoldTime && capacity > 0)
            {
                // Loop backwards to safely delete entries
                for (int i = items.Count - 1; i >= 0; i--)
                {
                    int id = GameManager.Instance.GetItemId(items[i]);
                    deliveryManager.DeliverItemRpc(id);
                    RemoveItem(i);
                }
            }
        } else
        {
            elapsedHoldTime -= Time.deltaTime;
            if (elapsedHoldTime < 0) elapsedHoldTime = 0f;
        }
    }

    private void AddItem(ItemData data)
    {
        items.Add(data);
        totalWeight += data.weight;
        capacity++;
    }

    public bool TryAddItem(ItemData data)
    {
        // Bag is full
        if (capacity + 1 > maxCapacity)
            return false;
        
        AddItem(data);
        return true;
    }

    private void RemoveItem(int dataIndex)
    {
        totalWeight -= items[dataIndex].weight;
        capacity--;
        items.RemoveAt(dataIndex);
    }

    void OnTriggerEnter(Collider obj)
    {
        if (obj.CompareTag("DeliveryPoint"))
        {
            deliveryManager = obj.GetComponentInParent<DeliveryManager>();
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
