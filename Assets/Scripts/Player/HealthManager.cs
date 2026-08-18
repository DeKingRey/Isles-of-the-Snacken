using UnityEngine;
using Unity.Netcode;
using System.Collections;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class HealthManager : NetworkBehaviour, IDamageable
{
    [Header("Settings")]
    public float maxHealth = 100f;
    [SerializeField] private float invulnerabilityDuration = 1f;
    [SerializeField] private EntityType entityType;

    [Space(10)]

    [Header("Damage Effects")]
    [SerializeField] private float vignetteFadeDuration = 0.5f;
    [SerializeField] private float maxVignetteIntensity = 0.4f;

    [Space(10)]

    [Header("Drowning")]
    [Tooltip("Time until player starts drowning")]
    [SerializeField] private float drownTime = 5f;

    [Tooltip("Interval between taking damage when drowning")]
    [SerializeField] private float drownInterval = 1f;
    [SerializeField] private float drownDamage = 20f;
    [HideInInspector] public NetworkVariable<float> currentHealth = new NetworkVariable<float>();

    private bool isInvulnerable;
    private Coroutine currentDrownCoroutine;
    private bool isDrowning = false;
    private float elapsedDrownTime = 0f;
    private Vignette damageVignette;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            currentHealth.Value = maxHealth;
        
        if (IsOwner && entityType == EntityType.Player)
        {
            GetComponentInChildren<Volume>().profile.TryGet(out damageVignette);
            currentHealth.OnValueChanged += OnHealthChanged;

            SceneEventBus.SceneChanged += RebindScene;
            RebindScene();   
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && entityType == EntityType.Player)
        {
            currentHealth.OnValueChanged -= OnHealthChanged;
            SceneEventBus.SceneChanged -= RebindScene;
        }
    }

    private void RebindScene()
    {
        PlayerUI ui = FindAnyObjectByType<PlayerUI>();
        
        if (ui != null)
        {
            ui.BindHealth(this);
        }

        // Revives player if they died
        currentHealth.Value = maxHealth;
        TogglePlayer(true);
    }

    void Update()
    {
        if (!IsOwner) return;

        if (currentHealth.Value <= 0 && entityType == EntityType.Player)
        {
            GetComponent<PlayerController>().ToggleInput(false);
        }

        if (isDrowning)
        {
            elapsedDrownTime += Time.deltaTime;

            if (elapsedDrownTime >= drownInterval)
            {
                TakeDamage(drownDamage);
                elapsedDrownTime = 0f;
            }
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

    // Begins damage effects for client
    private void OnHealthChanged(float prev, float current)
    {
        if (!IsOwner || entityType != EntityType.Player)
            return;
        
        if (current < prev && current > 0)
            StartCoroutine(FadeDamageVignette());
    }

    private IEnumerator FadeDamageVignette()
    {
        float elapsedTime = 0f;
        float startingIntensity = damageVignette.intensity.value;

        Debug.Log("fading");

        while (elapsedTime <= vignetteFadeDuration / 2f)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / (vignetteFadeDuration / 2f));

            damageVignette.intensity.value = Mathf.Lerp(startingIntensity, maxVignetteIntensity, t);

            yield return null;
        }

        elapsedTime = 0f;
        while (elapsedTime <= vignetteFadeDuration / 2f)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / (vignetteFadeDuration / 2f));

            damageVignette.intensity.value = Mathf.Lerp(maxVignetteIntensity, 0f, t);

            yield return null;
        }

        damageVignette.intensity.value = 0f;
    }

    private void Die()
    {
        switch (entityType)
        {
            case EntityType.Player:
                TogglePlayer(false);
                FindAnyObjectByType<GameUI>().deadUI.SetActive(true);
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
        GetComponent<Collider>().isTrigger = !isActive;
    }

    public void IsUnderwater(bool isUnderwater)
    {
        if (isUnderwater)
        {
            currentDrownCoroutine = StartCoroutine(DrownCountdown());
        } else
        {
            if (currentDrownCoroutine != null) StopCoroutine(DrownCountdown());
            isDrowning = false;
        }
    }

    private IEnumerator DrownCountdown()
    {
        yield return new WaitForSeconds(drownTime);

        isDrowning = true;
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

                float weight = 1f;

                // Disables fleeing enemies interact UI
                if (TryGetComponent<Item>(out Item item))
                {
                    item.canCollect = false;
                    weight = item.itemData.weight;
                }
                
                if (!trap.isManual) trap.Activate(weight); // For auto traps
                Struggle(true);
            }
        }
    }
}