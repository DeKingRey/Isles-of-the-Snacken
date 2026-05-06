using UnityEngine;

public class TrapBullet : MonoBehaviour
{
    [SerializeField] private float detectionDistance = 0.25f;
    [SerializeField] private LayerMask groundLayer;

    [HideInInspector] public GameObject trapToDeploy;
    private TrapGun gun;

    private bool deployed = false;

    void Start()
    {
        gun = FindAnyObjectByType<TrapGun>();
    }

    void Update()
    {
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
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        GameObject trap = Instantiate(trapToDeploy, hit.point, rotation);
        gun.currentTrap = trap.GetComponent<Trap>();

        Destroy(gameObject);
    }
}
