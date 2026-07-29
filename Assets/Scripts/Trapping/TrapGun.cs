using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

[System.Serializable]
public class TrapSlot
{
    public KeyCode keyCode;
    public GameObject trapObj;
    public Sprite trapSprite;
}

public class TrapGun : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] TrapSlot[] traps;

    [Space(10)]

    [Header("Settings")]
    [SerializeField] private float shootForce;
    [SerializeField] private float cooldown = 10f;
    private int currentTrapIndex = 0;
    [HideInInspector] public NetworkVariable<NetworkObjectReference> currentTrap = new NetworkVariable<NetworkObjectReference>();

    private PlayerController player;
    private Camera cam;

    private float shootTimer = 0f;
    private PlayerUI ui;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        SceneEventBus.SceneChanged += RebindScene;
        
        RebindScene();
    }

    private void RebindScene()
    {
        ui = FindAnyObjectByType<PlayerUI>();
        
        if (ui != null)
        {
            ui.BindTrapGun(this, traps);
        }

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
            {
                if (!trapObj.GetComponent<Trap>().canHarvest)
                    trapObj.Despawn(true);
            }
                
            ShootRpc(currentTrapIndex);
            shootTimer = cooldown;
            ui.TrapUICooldown(cooldown);
        }

        if (Input.GetMouseButtonDown(1) && currentTrap != null)
        {
            if (currentTrap.Value.TryGet(out NetworkObject trapObj))
            {
                Trap trap = trapObj.GetComponent<Trap>();
                if (trap.isManual) trap.Activate(); // Only activates if it is manually activated
            }
        }

        for (int i = 0; i < traps.Length; i++)
        {
            if (Input.GetKeyDown(traps[i].keyCode))
            {
                currentTrapIndex = i;
                ui.SelectTrapUI(i);
            }
        }
    }

    [Rpc(SendTo.Server)]
    void ShootRpc(int selectedTrapIndex)
    {
        GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, Quaternion.identity);
        bullet.GetComponent<NetworkObject>().Spawn();
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();

        // Sends the bullet forward
        Vector3 bulletForce = cam.transform.forward * shootForce;
        bulletRb.AddForce(bulletForce, ForceMode.Impulse);

        TrapBullet trapBullet = bullet.GetComponent<TrapBullet>();
        trapBullet.trapToDeploy = traps[selectedTrapIndex].trapObj;
        trapBullet.ownerClientId = OwnerClientId;
        trapBullet.bulletCam = cam;
    }
}
