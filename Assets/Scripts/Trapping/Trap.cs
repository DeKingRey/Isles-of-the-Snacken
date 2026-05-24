using Unity.Netcode;
using Unity.AI;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public abstract class Trap : NetworkBehaviour
{
    [Header("Harvest UI")]
    [SerializeField] private GameObject harvestUI;
    [SerializeField] private Image progressRing;

    [Space(10)]

    [HideInInspector] public bool canCapture;
    [HideInInspector] public TrapGun gun;
    [HideInInspector] public bool canHarvest = false;
    
    private List<GameObject> contents = new List<GameObject>();
    private Animator anim;
    private Camera cam;

    private float elapsedHoldTime;
    private bool hasHarvested;

    private bool serverHasHarvested = false;

    private Interactable interaction;

    void Start()
    {
        anim = GetComponent<Animator>();
        cam = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponentInChildren<Camera>();

        // Assigns interaction
        interaction = GetComponent<Interactable>();
        interaction.OnInteractComplete += CollectContents;
        interaction.AssignVariables(harvestUI, progressRing, cam);
    }

    void Update()
    {
        if (!canHarvest || !IsOwner) return;

        if (cam == null)
        {
            cam = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponentInChildren<Camera>();

            if (cam == null) return;
        }

        if (!hasHarvested) interaction.canInteract = true;
        else interaction.canInteract = false;
    }

    private void CollectContents()
    {
        hasHarvested = true;
        RequestHarvestRpc();
    }

    [Rpc(SendTo.Server)]
    private void RequestHarvestRpc(RpcParams rpcParams = default)
    {
        if (!canHarvest || serverHasHarvested)
            return;

        serverHasHarvested = true;

        ulong clientId = rpcParams.Receive.SenderClientId;
        NetworkObject playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);

        if (playerObj == null)
            return;
        
        PlayerInventory inventory = playerObj.GetComponent<PlayerInventory>();

        // Adds contents to players inventory if possible
        foreach (GameObject obj in contents)
        {
            bool hasCollected = inventory.TryAddItem(obj.GetComponent<Item>().itemData);

            // Despawns obj if collection was successful
            if (hasCollected)
            {
                obj.GetComponent<NetworkObject>()?.Despawn(true);
            }
        }
        GetComponent<NetworkObject>().Despawn(true);
    }

    public virtual void Activate()
    {
        anim.SetTrigger("Activate");
    }

    /// Adds whatever is within the trap to its harvestable contents
    public void AddContent(GameObject content)
    {
        contents.Add(content);
        if (canHarvest) return;

        // Makes the trap a solid obstacle
        GetComponentInChildren<UnityEngine.AI.NavMeshObstacle>().enabled = true;
        harvestUI.SetActive(true);

        canHarvest = true;
    }
}
