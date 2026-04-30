using UnityEngine;
using Unity.Netcode;

public class Health : NetworkBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            currentHealth = maxHealth;
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
        Debug.Log("You dead");
    }
}