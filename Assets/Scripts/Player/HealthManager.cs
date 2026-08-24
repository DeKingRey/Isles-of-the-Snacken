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
    [SerializeField] private float maxDamageVignetteIntensity = 0.4f;
    [SerializeField] private Volume damageVolume;

    [Space(10)]

    [Header("Drowning")]
    [Tooltip("Time until player starts drowning")]
    [SerializeField] private float drownTime = 1f;

    [Tooltip("Interval between taking damage when drowning")]
    [SerializeField] private float drownInterval = 1f;
    [SerializeField] private float drownDamage = 20f;
    [SerializeField] private Volume drowningVolume;
    [SerializeField] private float maxDrownVignetteIntensity = 1f;
    [SerializeField] private float drownFadeOutDuration = 1f;
    [HideInInspector] public NetworkVariable<float> currentHealth = new NetworkVariable<float>();

    private bool isInvulnerable;
    private Coroutine currentDrownCoroutine;
    private bool isDrowning = false;
    private float elapsedDrownTime = 0f;
    private Vignette damageVignette;
    private Vignette drowningVignette;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            currentHealth.Value = maxHealth;
        
        if (IsOwner && entityType == EntityType.Player)
        {
            damageVolume.profile.TryGet(out damageVignette);
            drowningVolume.profile.TryGet(out drowningVignette);
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
            float t = Mathf.SmoothStep(0f, 1f, 1 - (currentHealth.Value / maxHealth));

            drowningVignette.intensity.value = Mathf.Lerp(0f, maxDrownVignetteIntensity, t);

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

        while (elapsedTime <= vignetteFadeDuration / 2f)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / (vignetteFadeDuration / 2f));

            damageVignette.intensity.value = Mathf.Lerp(startingIntensity, maxDamageVignetteIntensity, t);

            yield return null;
        }

        elapsedTime = 0f;
        while (elapsedTime <= vignetteFadeDuration / 2f)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / (vignetteFadeDuration / 2f));

            damageVignette.intensity.value = Mathf.Lerp(maxDamageVignetteIntensity, 0f, t);

            yield return null;
        }

        damageVignette.intensity.value = 0f;
    }

    private void Die()
    {
        switch (entityType)
        {
            case EntityType.Player:
                // If all players are dead, end the game
                int deadPlayerCount = 0;
                for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
                {
                    if (NetworkManager.Singleton.ConnectedClientsList[i].PlayerObject.GetComponent<HealthManager>().currentHealth.Value <= 0)
                    {
                        deadPlayerCount += 1;
                    }
                }

                if (deadPlayerCount == NetworkManager.Singleton.ConnectedClientsList.Count)
                {
                    GameManager.Instance.ChangeState(GameManager.GameState.GameOver);
                    break;
                }
                
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

    public void ResetHealth()
    {
        // Stop drowning
        if (currentDrownCoroutine != null)
        {
            StopCoroutine(currentDrownCoroutine);
            currentDrownCoroutine = null;
        }

        isDrowning = false;
        elapsedDrownTime = 0f;

        // Stop damage/invulnerability state
        isInvulnerable = false;

        // Reset health on the server
        if (IsServer)
            currentHealth.Value = maxHealth;

        // Reset visual effects
        if (damageVignette != null)
            damageVignette.intensity.value = 0f;

        if (drowningVignette != null)
            drowningVignette.intensity.value = 0f;
    }

    public void IsUnderwater(bool isUnderwater)
    {
        if (isUnderwater)
        {
            if (currentDrownCoroutine != null) 
            {
                StopCoroutine(currentDrownCoroutine);
                currentDrownCoroutine = null;
            }
            currentDrownCoroutine = StartCoroutine(DrownCountdown());
        } else
        {
            if (currentDrownCoroutine != null) 
            {
                StopCoroutine(currentDrownCoroutine);
                currentDrownCoroutine = null;
            }
            isDrowning = false;
            StartCoroutine(FadeOutDrownVignette());
        }
    }

    private IEnumerator DrownCountdown()
    {
        yield return new WaitForSeconds(drownTime);

        isDrowning = true;
    }

    private IEnumerator FadeOutDrownVignette()
    {
        float elapsedTime = 0f;
        float startingIntensity = drowningVignette.intensity.value;

        while (elapsedTime <= drownFadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / drownFadeOutDuration);

            drowningVignette.intensity.value = Mathf.Lerp(startingIntensity, 0f, t);

            yield return null;
        }
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