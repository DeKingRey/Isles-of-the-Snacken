using UnityEngine;
using Unity.Netcode;

public class TrapGun : NetworkBehaviour
{
    [Header("Shooting")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float shootForce;
    [SerializeField] private float fireRate = 0.25f;

    [SerializeField] private GameObject selectedTrap; // Eventually make private
    [HideInInspector] public NetworkVariable<NetworkObjectReference> currentTrap = new NetworkVariable<NetworkObjectReference>();

    private PlayerController player;
    private Camera cam;

    private float shootTimer = 0f;

    void Start()
    {
        player = GetComponent<PlayerController>();
        cam = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        if (!IsOwner) return;

        if (!player.inputEnabled) return;

        shootTimer -= Time.deltaTime;

        // Shoot the trap gun
        if (Input.GetMouseButtonDown(0) && shootTimer <= 0f)
        {
            if (currentTrap.Value.TryGet(out NetworkObject trapObj))
                trapObj.Despawn(true);
            ShootRpc();
            shootTimer = fireRate;
        }

        if (Input.GetMouseButtonDown(1) && currentTrap != null)
        {
            if (currentTrap.Value.TryGet(out NetworkObject trapObj))
            {
                trapObj.GetComponent<Trap>().Activate();
            }
        }
    }

    [Rpc(SendTo.Server)]
    void ShootRpc()
    {
        GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, Quaternion.identity);
        bullet.GetComponent<NetworkObject>().Spawn();
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();

        // Sends the bullet forward
        Vector3 bulletForce = cam.transform.forward * shootForce;
        bulletRb.AddForce(bulletForce, ForceMode.Impulse);

        TrapBullet trapBullet = bullet.GetComponent<TrapBullet>();
        trapBullet.trapToDeploy = selectedTrap;
        trapBullet.ownerClientId = OwnerClientId;
    }
}
