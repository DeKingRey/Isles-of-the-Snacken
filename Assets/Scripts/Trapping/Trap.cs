using UnityEngine;
using Unity.Netcode;
using Unity.AI;
using System.Collections.Generic;

public abstract class Trap : MonoBehaviour
{
    [SerializeField] private GameObject harvestUI;

    [HideInInspector] public bool canCapture;
    private Animator anim;
    [HideInInspector] public TrapGun gun;

    private List<GameObject> contents = new List<GameObject>();
    [HideInInspector] public bool canHarvest = false;

    void Start()
    {
        anim = GetComponent<Animator>();
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
