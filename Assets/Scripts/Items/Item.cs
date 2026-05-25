using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System;

public enum EntityType
{
    Player,
    Nommian
}

public class Item : NetworkBehaviour
{
    [Header("Data")]
    public ItemData itemData;
    public EntityType type;

    [Space(10)]
    
    [Header("Collection Settings")]
    public bool canCollect = false;
    [SerializeField] private float interactRadius = 10f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private GameObject collectUI;
    [SerializeField] private Image progressRing;
    
    private Camera cam;
    private Interactable interaction;

    private bool hasCollected = false;
    private bool serverHasCollected = false;

    public override void OnNetworkSpawn()
    {
        cam = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponentInChildren<Camera>();

        // Assigns interaction
        interaction = GetComponent<Interactable>();
        interaction.OnInteractComplete += CollectItem;
        interaction.AssignVariables(collectUI, progressRing, cam);
    }

    void Update()
    {
        if (!IsOwner) return;
        
        // Only activates interaction when player is in range
        if (canCollect && Physics.CheckSphere(transform.position, interactRadius, playerLayer))
        {
            interaction.canInteract = true;
        } else {
            interaction.canInteract = false;
        }
    }

    void CollectItem()
    {
        canCollect = false;
        hasCollected = true;
        RequestCollectRpc();
    }

    [Rpc(SendTo.Server)]
    private void RequestCollectRpc(RpcParams rpcParams = default)
    {
        if (!hasCollected || serverHasCollected)
            return;

        serverHasCollected = true;

        ulong clientId = rpcParams.Receive.SenderClientId;
        NetworkObject playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);

        if (playerObj == null)
            return;
        
        PlayerInventory inventory = playerObj.GetComponent<PlayerInventory>();

        bool collectSuccess = inventory.TryAddItem(itemData);

        // Despawns item if collection was successful
        if (collectSuccess)
        {
            GetComponent<NetworkObject>()?.Despawn(true);
        }
    }
}
