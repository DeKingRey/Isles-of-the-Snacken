using TMPro;
using Unity.Netcode;
using UnityEngine;

public class QuotaManager : NetworkBehaviour
{
    [Header("Quota")]
    [SerializeField] private TextMeshProUGUI quotaText;

    [Tooltip("Base min amount of nommians - increments with levels")]
    [SerializeField] private int minNommians = 4;
    [SerializeField] private int maxNommians = 6;

    [Tooltip("How much the required nommians will increment per level")]
    [SerializeField] private int quotaMultiplier = 2;
    private NetworkVariable<int> deliveredNommians = new(0);
    private NetworkVariable<int> requiredNommians = new();

    public override void OnNetworkSpawn()
    {
        // Assign on quota changed so that text isn't constantly updating
        deliveredNommians.OnValueChanged += OnQuotaChanged;
        requiredNommians.OnValueChanged += OnQuotaChanged;

        if (!IsServer) return;

        // Required nommians (for the quota) increments each level
        requiredNommians.Value = Random.Range(minNommians, maxNommians + 1) 
                                + (GameManager.Instance.currentLevel * quotaMultiplier);
    }

    public override void OnNetworkDespawn()
    {
        deliveredNommians.OnValueChanged -= OnQuotaChanged;
        requiredNommians.OnValueChanged -= OnQuotaChanged;
    }

    private void OnQuotaChanged(int previous, int current)
    {
        quotaText.text = $"{requiredNommians.Value}";
    }
}
