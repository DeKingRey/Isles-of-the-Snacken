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

    [Header("Settings")]
    [SerializeField] private float damage = 100f;
    
    [Tooltip("Will determine where contents spawn")]
    [SerializeField] private Transform trapCentre;

    [Tooltip("Whether the trap is manually activated or not")]
    public bool isManual = true;

    [HideInInspector] public bool canCapture;
    [HideInInspector] public TrapGun gun;
    [HideInInspector] public bool canHarvest = false;
    
    private List<GameObject> contents = new List<GameObject>();
    private Animator anim;
    private Camera cam;

    private bool hasHarvested;

    private bool serverHasHarvested = false;

    private Interactable interaction;
    private bool contentEscaped = false;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        
        GetComponent<NetworkObject>().DestroyWithScene = true;
    }   

    public virtual void Start()
    {
        anim = GetComponentInChildren<Animator>();
        cam = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponentInChildren<Camera>();

        // Assigns interaction
        interaction = GetComponent<Interactable>();
        interaction.OnInteractComplete += CollectContents;
        interaction.AssignVariables(harvestUI, progressRing, cam);

        if (!isManual) canCapture = true; // Can always capture if trap is automatic
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

        // Sorts by weight (lowest to highest) so that player can always take the most items possible
        // Must be sorted, as if the total weight exceeds the capacity then all items after will be ignored
        contents.Sort((a, b) => a.GetComponent<Item>().itemData.weight.CompareTo(b.GetComponent<Item>().itemData.weight));

        // Adds contents to players inventory if possible
        foreach (GameObject obj in contents)
        {
            bool hasCollected = inventory.TryAddItem(obj.GetComponent<Item>().itemData);

            // Despawns obj if collection was successful
            if (hasCollected)
            {
                obj.GetComponent<NetworkObject>()?.Despawn(true);
            } else
            {
                // Allows content to escape otherwise - for example if content was too heavy
                RemoveContent();
                break; // Ignores the rest of the items, weight has been exceeded
            }
        }

        // Only destroys if content didn't escape - otherwise RemoveContent() handles it
        if (!contentEscaped) GetComponent<NetworkObject>().Despawn(true);
    }

    public virtual void Activate(float contentWeight = 1f)
    {
        anim.SetTrigger("Activate");
    }

    /// Adds whatever is within the trap to its harvestable contents
    public void AddContent(GameObject content)
    {
        contents.Add(content);
        if (canHarvest) return;

        content.transform.position = trapCentre.position;

        // Makes the trap a solid obstacle
        GetComponentInChildren<UnityEngine.AI.NavMeshObstacle>().enabled = true;
        harvestUI.SetActive(true);

        canHarvest = true;
    }

    // For content that escapes
    public void RemoveContent()
    {
        if (contents.Count == 0) return;

        contentEscaped = true;

        foreach (GameObject content in contents)
        {
            // Stops struggling, though the struggle func ignores if the entity is already dead
            content.GetComponent<HealthManager>().Struggle(false);
        }

        contents.Clear(); // Clears the list after running through content to avoid errors
        GetComponent<NetworkObject>().Despawn(true);
    }

    public float GetTrapDamage()
    {
        return damage;
    }
}
