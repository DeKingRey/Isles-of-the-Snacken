using Unity.Netcode;
using UnityEngine;

public class SnackenHead : NetworkBehaviour
{
    private Transform ship;
    private Animator anim;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            ship = FindAnyObjectByType<ShipController>().transform;
        }

        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!IsServer) return;

        // Head will look in the direction of the ship
        Vector3 direction = ship.position - transform.position;
        // Multiply by rot as mesh is backwards
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(0, 180f, 0);

        Vector3 euler = targetRotation.eulerAngles;
        
        // Converts 0-360 range to -180 to 180 for negative angles
        if (euler.x > 180f)
            euler.x -= 360f;

        // Clamps x rotation to 0 or less to prevent head from tilting forward
        euler.x = Mathf.Min(euler.x, 0f);

        transform.rotation = Quaternion.Euler(euler);
    }

    void OnTriggerEnter(Collider obj)
    {
        if (!IsServer) return;

        if (obj.CompareTag("Ship"))
        {
            anim.SetBool("mouthOpen", true);
        }
    }

    void OnTriggerExit(Collider obj)
    {
        if (!IsServer) return;

        if (obj.CompareTag("Ship"))
        {
            anim.SetBool("mouthOpen", false);
        }
    }
}
