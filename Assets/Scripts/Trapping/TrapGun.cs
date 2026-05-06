using UnityEngine;

public class TrapGun : MonoBehaviour
{
    [Header("Shooting")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float shootForce;
    [SerializeField] private float fireRate = 0.25f;

    [SerializeField] private GameObject selectedTrap; // Eventually make private
    [HideInInspector] public Trap currentTrap;

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
        if (!player.inputEnabled) return;

        shootTimer -= Time.deltaTime;

        // Shoot the trap gun
        if (Input.GetMouseButtonDown(0) && shootTimer <= 0f)
        {
            if (currentTrap != null)
                Destroy(currentTrap.gameObject);
            Shoot();
            shootTimer = fireRate;
        }

        if (Input.GetMouseButtonDown(1) && currentTrap != null)
        {
            currentTrap.Activate();
        }
    }

    void Shoot()
    {
        GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, Quaternion.identity);
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();

        // Sends the bullet forward
        Vector3 bulletForce = cam.transform.forward * shootForce;
        bulletRb.AddForce(bulletForce, ForceMode.Impulse);

        TrapBullet trapBullet = bullet.GetComponent<TrapBullet>();
        trapBullet.trapToDeploy = selectedTrap;
    }
}
