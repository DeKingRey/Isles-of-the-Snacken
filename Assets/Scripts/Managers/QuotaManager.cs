using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class QuotaManager : NetworkBehaviour
{
    public static QuotaManager Instance;
    [Tooltip("The wait time between feeding the Snacken and finding out the result")]
    [SerializeField] private float checkDuration = 10f;

    [Header("Quota")]
    [SerializeField] private TextMeshProUGUI quotaText;

    [Tooltip("Base min amount of nommians - increments with levels")]
    [SerializeField] private int minNommians = 4;
    [SerializeField] private int maxNommians = 6;

    [Tooltip("How much the required nommians will increment per level")]
    [SerializeField] private int quotaMultiplier = 2;
    private NetworkVariable<int> deliveredNommians = new(0);
    private NetworkVariable<int> requiredNommians = new();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

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

    // This is called when the players feed the Snacken
    public IEnumerator CheckQuotaReached(int amount)
    {
        yield return new WaitForSeconds(checkDuration);

        if (amount < requiredNommians.Value)
        {
            // Lose :(
            GameManager.Instance.ChangeState(GameManager.GameState.GameOver);
        } else
        {
            // Win!
            // Spawn coins
            SceneEventBus.Instance.ToggleLoadingScreenRpc(true);
            NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
        }
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
