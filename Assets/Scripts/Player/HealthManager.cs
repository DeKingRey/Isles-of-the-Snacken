using UnityEngine;
using Unity.Netcode;
using Unity.AI;

public class HealthManager : NetworkBehaviour, IDamageable
{
    public enum EntityType
    {
        Player,
        Nommian
    }

    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private EntityType entityType;
    private float currentHealth;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            currentHealth = maxHealth;
    }

    void Update()
    {
        if (currentHealth <= 0 && entityType == EntityType.Player)
        {
            GetComponent<PlayerController>().ToggleInput(false);
        }
    }

    public void TakeDamage(float damage)
    {
        if (!IsServer) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        switch (entityType)
        {
            case EntityType.Player:
                PlayerDie();
                break;
            case EntityType.Nommian:
                NommianDie();
                break;
        }
    }

    private void PlayerDie()
    {
        GetComponent<PlayerController>().ToggleInput(false);
    }

    private void NommianDie()
    {
        GetComponent<NommianController>().isCaptured = true;
    }

    void OnTriggerStay(Collider obj)
    {
        if (obj.CompareTag("Trap"))
        {
            Trap trap = obj.GetComponentInParent<Trap>();
            if (trap.canCapture)
            {
                TakeDamage(maxHealth);
                trap.AddContent(gameObject);
            }
        }
    }
}