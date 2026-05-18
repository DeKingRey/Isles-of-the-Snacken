using Unity.Netcode;
using Unity.AI;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public abstract class Trap : NetworkBehaviour
{
    [Header("Harvest Settings")]
    [SerializeField] private float harvestHoldTime = 1f;
    [SerializeField] private float rayRadius = 0.5f;
    [SerializeField] private float rayDistance = 5f;
    [SerializeField] private LayerMask trapLayer;

    [Space(5)]

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

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (!canHarvest || !IsOwner) return;

        if (cam == null)
        {
            cam = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponentInChildren<Camera>();

            if (cam == null) return;
        }

        HandleHarvest();
    }

    void HandleHarvest()
    {
        RaycastHit hit;

        if (!hasHarvested) progressRing.fillAmount = elapsedHoldTime / harvestHoldTime;

        if (!Physics.SphereCast(cam.transform.position, rayRadius, cam.transform.forward, out hit, rayDistance, trapLayer))
        {
            elapsedHoldTime = 0f;
            return;
        }

        // Hold down to harvest contents of trap
        if (Input.GetKey(KeyCode.E))
        {
            elapsedHoldTime += Time.deltaTime;

            // Collects contents
            if (elapsedHoldTime >= harvestHoldTime && !hasHarvested)
            {
                hasHarvested = true;
                PlayerBag bag = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponentInChildren<PlayerBag>();

                // Adds contents to players bag if possible
                foreach (GameObject obj in contents)
                {
                    bool hasCollected = bag.TryAddItem(obj.GetComponent<Item>());

                    // Despawns obj if collection was successful
                    if (hasCollected)
                    {
                        obj.GetComponent<NetworkObject>()?.Despawn(true);
                    }
                }
                GetComponent<NetworkObject>().Despawn(true);
            }
        } else
        {
            elapsedHoldTime -= Time.deltaTime;
            if (elapsedHoldTime < 0) elapsedHoldTime = 0f;
        }
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
