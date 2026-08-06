using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            DontDestroyOnLoad(gameObject);
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
    }

    public override void OnNetworkDespawn()
    {
        State.OnValueChanged -= OnStateChanged;
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

        Debug.Log("State change");

        StartCoroutine(TransitionToState(newState, delay));
    }

    private IEnumerator TransitionToState(GameState newState, float delay)
    {
        yield return new WaitForSeconds(delay);

        Debug.Log("Delay over");
        
        State.Value = newState;
    }

    // Handles state changes
    private void OnStateChanged(GameState previous, GameState current)
    {
        switch (current)
        {
            case GameState.Playing:
                break;
            case GameState.GameOver:
                Debug.Log("Game over");
                GameOverRpc();
                break;
            case GameState.Snacken:
                Debug.Log("Inside snacken stomach");
                if (IsServer) SpawnDeliveredNommiansRpc();
                break;
        }
    }

    [Rpc(SendTo.Everyone)]
    void GameOverRpc()
    {
        if (IsServer)
        {
            // Disables input for all players
            for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
            {
                NetworkManager.Singleton.ConnectedClientsList[i].PlayerObject.GetComponent<PlayerController>().ToggleInput(false);
                NetworkManager.Singleton.ConnectedClientsList[i].PlayerObject.GetComponent<PlayerCam>().ToggleInput(false);
            }
        }

        Debug.Log("rpc");

        gameOverUI.SetActive(true);
        if (IsHost) playAgainButton.SetActive(true);
        if (IsClient) waitingForHostUI.SetActive(true);
    }

    public void PlayAgain()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        }
    }
}