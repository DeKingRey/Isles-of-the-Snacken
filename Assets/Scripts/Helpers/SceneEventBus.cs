using UnityEngine;
using System;
using System.Collections;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine.SceneManagement;

/// <summary>
///  Handles loading screens and syncronisation
/// </summary>
public class SceneEventBus : MonoBehaviour
{
    [SerializeField] private GameObject loadingScreen;

    public static SceneEventBus Instance;
    public static event Action SceneChanged;
    public static event Action<ulong> ClientFinishedLoading;
    private HashSet<ulong> loadedClients = new();

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

    private IEnumerator Start()
    {
        // Waits for network manager initialisation
        while (NetworkManager.Singleton == null || NetworkManager.Singleton.SceneManager == null)
        {
            yield return null;
        }

        NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;   
        }
    }

    void OnSceneEvent(SceneEvent sceneEvent)
    {
        if (sceneEvent.SceneEventType != SceneEventType.LoadComplete)
            return;

        if (sceneEvent.ClientId == NetworkManager.Singleton.LocalClientId)
        {
            SceneChanged?.Invoke();

            // Returns if there is an island generator in scene
            if (FindAnyObjectByType<IslandGenerator>()) return; // Allows island generator to control loading
            loadingScreen.SetActive(false);
        }

        ClientFinishedLoading?.Invoke(sceneEvent.ClientId);

        // Only the server counts up loaded clients
        if (NetworkManager.Singleton.IsServer)
            OnClientFinishedLoading(sceneEvent.ClientId);
    }

    private void OnClientFinishedLoading(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        loadedClients.Add(clientId);

        // Hides loading screen when all players load in
        if (loadedClients.Count == NetworkManager.Singleton.ConnectedClients.Count)
        {
            AllPlayersLoaded();
        }
    }

    private void AllPlayersLoaded()
    {
        SpawnPlayers(); // Puts the players in the correct position

        if (SceneManager.GetActiveScene().name == "Snacken")
            GameManager.Instance.ChangeState(GameManager.GameState.Snacken);

        // Returns if there is an island generator in scene
        if (FindAnyObjectByType<IslandGenerator>()) return; // Allows island generator to control loading
        
        ToggleLoadingScreenRpc(false);
    }

    [Rpc(SendTo.Everyone)]
    public void ToggleLoadingScreenRpc(bool isActive)
    {
        loadingScreen.SetActive(isActive);
    }
    
    // Spawns players at placed spawnpoints
    private void SpawnPlayers()
    {
        PlayerSpawnpoint[] spawnpoints = FindObjectsByType<PlayerSpawnpoint>();

        for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
        {
            // Will spawn randomly if there are no available spawnpoints (though there should be)
            if (spawnpoints[i] == null || NetworkManager.Singleton.ConnectedClientsList[i].PlayerObject == null || i >= spawnpoints.Length)
                break;

            var player =  NetworkManager.Singleton.ConnectedClientsList[i].PlayerObject;

            player.transform.position = spawnpoints[i].transform.position;

            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller != null && controller.isSteering)
            {
                FindAnyObjectByType<SteeringWheel>().TrySteerShip(controller);
            }
        }
    }
}