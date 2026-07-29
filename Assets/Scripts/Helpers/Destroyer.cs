using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Destroyer : MonoBehaviour
{
    [SerializeField] private float lifetime = 5f;

    void Start()
    {
        StartCoroutine(DestroySelf());
    }

    private IEnumerator DestroySelf()
    {
        yield return new WaitForSeconds(lifetime);

        if (GetComponent<NetworkObject>())
        {
            GetComponent<NetworkObject>().Despawn();
        } else
        {
            Destroy(this);
        }
    }
}
