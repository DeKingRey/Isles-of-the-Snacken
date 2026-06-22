using UnityEngine;
using Unity.Netcode;

public class TrapBullet : NetworkBehaviour
{
    [SerializeField] private float detectionDistance = 0.25f;
    [SerializeField] private LayerMask groundLayer;

    [HideInInspector] public GameObject trapToDeploy;
    [HideInInspector] public ulong ownerClientId;
    [HideInInspector] public Camera bulletCam;

    private bool deployed = false;

    void Update()
    {
        if (!IsServer) return;

        if (deployed) return;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, detectionDistance, groundLayer))
        {
            deployed = true;
            DeployTrap(hit);
        }
    }

    void DeployTrap(RaycastHit hit)
    {
        Vector3 up = hit.normal; // Will give correct vertical rotation
        Vector3 forward = Vector3.ProjectOnPlane(bulletCam.transform.forward, up).normalized; // Ensures forward is on a flat surface
        
        // Trap will face away from the player and be correcly on the surface
        Quaternion rotation = Quaternion.LookRotation(forward, up);
        GameObject trap = Instantiate(trapToDeploy, hit.point, rotation);
        trap.GetComponent<NetworkObject>().Spawn();

        // Find the player that owns the bullet and assign the trap
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId != ownerClientId)
                continue;
            
            TrapGun gun = client.PlayerObject.GetComponent<TrapGun>();

            // Ensures old trap is removed
            if (gun.currentTrap.Value.TryGet(out NetworkObject oldTrap))
            {
                if (oldTrap != null && oldTrap.IsSpawned && !oldTrap.GetComponent<Trap>().canHarvest)
                {
                    oldTrap.Despawn(true);
                }
            }

            gun.currentTrap.Value = trap.GetComponent<NetworkObject>();

            break;
        }

        GetComponent<NetworkObject>().Despawn(true);
    }
}
