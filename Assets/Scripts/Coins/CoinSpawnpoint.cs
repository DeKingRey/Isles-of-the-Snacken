using UnityEngine;

public class CoinSpawnpoint : MonoBehaviour
{
    [HideInInspector] public Transform spawnTransform;

    void Start()
    {
        spawnTransform = transform;
    }
}
