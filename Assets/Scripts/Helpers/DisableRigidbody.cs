using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class DisableRigidbody : NetworkBehaviour
{
    [SerializeField] private float enabledDuration = 2f;
    private Rigidbody rb;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        rb = GetComponent<Rigidbody>();
    }

    private IEnumerator DisableCountdown()
    {
        yield return new WaitForSeconds(enabledDuration);

        rb.isKinematic = true;
    }
}