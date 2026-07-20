using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public NetworkVariable<GameState> State = new(GameState.Playing);
    
    [Space(10)]

    [Header("Databases")]
    public ItemData[] itemDatabase;
    public NommianDatabase nommianDatabase;

    [HideInInspector] public int currentLevel = 0;

    public enum GameState
    {
        Playing,
        DayComplete
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

    public void ChangeState(GameState newState, float delay)
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
                break;
            case GameState.DayComplete:
                Debug.Log("Game Over");
                break;
        }
    }
}