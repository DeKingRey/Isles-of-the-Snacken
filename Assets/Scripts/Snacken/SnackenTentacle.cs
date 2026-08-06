using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class SnackenTentacle : NetworkBehaviour
{
    [Header("Randomisation Settings")]
    [SerializeField] private Vector3 minSizes;
    [SerializeField] private Vector3 maxSizes;

    [Space(5)]

    [SerializeField] private float minAnimSpeed;
    [SerializeField] private float maxAnimSpeed;

    [Space(5)]

    [SerializeField] private float minSlamDelay = 0f;
    [SerializeField] private float maxSlamDelay = 7f;
    
    private Animator anim;

    private NetworkVariable<Vector3> randomScale = new();
    private NetworkVariable<float> randomAnimSpeed = new();

    public override void OnNetworkSpawn()
    {
        anim = GetComponent<Animator>();

        // Server decides random values then sends them to all players
        if (IsServer)
        {
            randomScale.Value = new Vector3(
                Random.Range(minSizes.x, maxSizes.x),
                Random.Range(minSizes.y, maxSizes.y),
                Random.Range(minSizes.z, maxSizes.z)
            );

            randomAnimSpeed.Value = Random.Range(minAnimSpeed, maxAnimSpeed);
        }

        transform.localScale = randomScale.Value;
        anim.speed = randomAnimSpeed.Value;
    }

    public IEnumerator SlamDown()
    {
        // Slams will occcur at slightly random intervals for randomness
        float randomDelay = Random.Range(minSlamDelay, maxSlamDelay);
        
        yield return new WaitForSeconds(randomDelay);

        anim.SetTrigger("Slam");
    }
}
