using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public NetworkVariable<GameState> State = new(GameState.Playing);
    
    [Space(10)]

    [Header("Databases")]
    public ItemData[] itemDatabase;
    public NommianDatabase nommianDatabase;

    [Space(10)]

    [Header("GameOver")]
    [SerializeField] private GameObject playAgainButton;
    [SerializeField] private GameObject waitingForHostUI;
    [SerializeField] private GameObject gameOverUI;

    [HideInInspector] public int currentLevel = 0;
    private List<ItemData> deliveredNommians = new List<ItemData>();
    private NetworkVariable<int> coinAmount = new NetworkVariable<int>(0);
    private TextMeshProUGUI coinText;

    public enum GameState
    {
        Playing,
        GameOver,
        Snacken
    }

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
        State.OnValueChanged += OnStateChanged; // Triggers when state changes on all clients
        SceneEventBus.SceneChanged += RebindScene;
        coinAmount.OnValueChanged += OnCoinAmountChanged;

        RebindScene();
    }

    public override void OnNetworkDespawn()
    {
        State.OnValueChanged -= OnStateChanged;
        SceneEventBus.SceneChanged -= RebindScene;
    }

    void RebindScene()
    {
        GameUI ui = FindAnyObjectByType<GameUI>();

        if (ui == null) return;

        playAgainButton = ui.playAgainButton;
        waitingForHostUI = ui.waitingForHostUI;
        gameOverUI = ui.gameOverUI;

        coinText = ui.coinText;
        coinText.text = coinAmount.Value.ToString();

        playAgainButton.GetComponent<Button>().onClick.AddListener(PlayAgain);
    }

    public int GetItemId(ItemData data)
    {
        for (int i = 0; i < itemDatabase.Length; i++)
        {
            if (itemDatabase[i] == data)
                return i;
        }

        return -1; // Not found
    }

    public void AddCoin()
    {
        coinAmount.Value++;
    }

    private void OnCoinAmountChanged(int prev, int current)
    {
        coinText.text = current.ToString();
    }

    // Keeps track of delivered nommians to spawn them upon scene change 
    [Rpc(SendTo.Server)]
    public void AddDeliveredNommianRpc(int itemId)
    {
        deliveredNommians.Add(itemDatabase[itemId]);
    }

    // Spawns delivered nommians upon entering the Snacken
    [Rpc(SendTo.Server)]
    private void SpawnDeliveredNommiansRpc()
    {
        DeliveryManager dm = FindAnyObjectByType<DeliveryManager>();
        foreach (ItemData nommian in deliveredNommians)
        {
            dm.DeliverItemRpc(GetItemId(nommian));
        }
    }

    public void ChangeState(GameState newState, float delay = 0)
    {
        if (!IsServer || State.Value == newState) return;

        StartCoroutine(TransitionToState(newState, delay));
    }

    private IEnumerator TransitionToState(GameState newState, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        State.Value = newState;
    }

    // Handles state changes
    private void OnStateChanged(GameState previous, GameState current)
    {
        switch (current)
        {
            case GameState.Playing:
                if (gameOverUI) gameOverUI.SetActive(false);
                break;
            case GameState.GameOver:
                if (IsServer)
                {
                    // Disables input for all players
                    for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
                    {
                        NetworkManager.Singleton.ConnectedClientsList[i].PlayerObject.GetComponent<PlayerController>().ToggleInput(false);
                        NetworkManager.Singleton.ConnectedClientsList[i].PlayerObject.GetComponent<PlayerCam>().ToggleInput(false);
                    }
                }

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                gameOverUI.SetActive(true);
                if (IsHost) playAgainButton.SetActive(true);
                else waitingForHostUI.SetActive(true);

                break;
            case GameState.Snacken:
                if (IsServer) SpawnDeliveredNommiansRpc();
                break;
        }
    }

    public void PlayAgain()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        }
    }
}