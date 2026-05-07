using UnityEngine;
using Unity.Netcode;

public class HealthManager : NetworkBehaviour, IDamageable
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

    void OnTriggerEnter(Collider obj)
    {
        if (obj.CompareTag("Trap"))
        {
            if (obj.GetComponent<Trap>().canCapture)
            {
                Die(); // Enemy/player is captured
            }
        }
    }
}