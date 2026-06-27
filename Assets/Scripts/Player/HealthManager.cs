using UnityEngine;
using Unity.Netcode;
using Unity.AI;
using System.Collections;

public class HealthManager : NetworkBehaviour, IDamageable
{
    [Header("Settings")]
    public float maxHealth = 100f;
    [SerializeField] private float invulnerabilityDuration = 1f;
    [SerializeField] private EntityType entityType;
    [HideInInspector] public NetworkVariable<float> currentHealth = new NetworkVariable<float>();

    private bool isInvulnerable;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            currentHealth.Value = maxHealth;
        
        if (IsOwner && entityType == EntityType.Player)
        {
            SceneEventBus.SceneChanged += RebindScene;
            RebindScene();   
        }
    }

    private void RebindScene()
    {
        PlayerUI ui = FindAnyObjectByType<PlayerUI>();
        
        if (ui != null)
        {
            ui.BindHealth(this);
        }
    }

    void Update()
    {
        if (currentHealth.Value <= 0 && entityType == EntityType.Player)
        {
            GetComponent<PlayerController>().ToggleInput(false);
        }
    }

    public void TakeDamage(float damage)
    {
        if (!IsServer || isInvulnerable) return;

        currentHealth.Value -= damage;

        if (currentHealth.Value <= 0)
            Die();
        else if (entityType == EntityType.Player)
        {
            StartCoroutine(InvulnerabilityTimer());
        }
    }

    private IEnumerator InvulnerabilityTimer()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(invulnerabilityDuration);
        isInvulnerable = false;
    }

    private void Die()
    {
        switch (entityType)
        {
            case EntityType.Player:
                TogglePlayer(false);  // Disables player
                break;
            case EntityType.Nommian:
                ToggleNommian(false);  // Disables nommian
                break;
        }
    }

    public void Struggle(bool isStruggling)
    {
        if (currentHealth.Value <= 0) return; // No need for corpses to struggle

        switch (entityType)
        {
            case EntityType.Player:
                TogglePlayer(!isStruggling);
                break;
            case EntityType.Nommian:
                ToggleNommian(!isStruggling);
                break;
        }
    }

    private void TogglePlayer(bool isActive)
    {
        GetComponent<PlayerController>().ToggleInput(isActive);
        if (currentHealth.Value <= 0) GetComponent<PlayerCam>().ToggleInput(isActive);
    }

    private void ToggleNommian(bool isActive)
    {
        GetComponent<NommianController>().isCaptured = !isActive;
        GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = isActive;
        GetComponent<Animator>().enabled = isActive;
        GetComponent<Rigidbody>().isKinematic = !isActive;
        GetComponent<BoxCollider>().isTrigger = !isActive;
    }

    void OnTriggerEnter(Collider obj)
    {
        // Player takes damage if they touch nommian weapon while the nommian is attacking
        if (entityType == EntityType.Player && obj.CompareTag("NommianWeapon"))
        {
            NommianController nommian = obj.GetComponentInParent<NommianController>();

            if (nommian.canDamage)
            {
                TakeDamage(nommian.GetNommianDamage());
            }
        }
    }

    void OnTriggerStay(Collider obj)
    {
        // Trap collision is in on trigger stay as the entity will be touching the trap before it can damage
        if (obj.CompareTag("Trap"))
        {
            Trap trap = obj.GetComponentInParent<Trap>();
            if (trap.canCapture)
            {
                TakeDamage(trap.GetTrapDamage());
                trap.AddContent(gameObject);
                if (!trap.isManual) trap.Activate(); // For auto traps
                Struggle(true);
            }
        }
    }
}