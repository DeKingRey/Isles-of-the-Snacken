using Unity.Netcode;
using UnityEngine;

public class PlayerFeet : MonoBehaviour
{
    [SerializeField] private LayerMask oceanLayer;
    [SerializeField] private GameObject splashParticles;

    void OnTriggerEnter(Collider obj)
    {
        if (obj.CompareTag("Ocean"))
        {
            Vector3 rayPos = new Vector3(transform.position.x, transform.position.y + 10f, transform.position.z);
            RaycastHit hit;
            if (Physics.Raycast(rayPos, -transform.up, out hit, oceanLayer))
            {
                GameObject splash = Instantiate(splashParticles, hit.point, Quaternion.identity);
                splash.GetComponent<NetworkObject>().Spawn();
            }
        }
    }
}
