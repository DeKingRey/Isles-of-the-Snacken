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
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;   
        }
    }

    void OnSceneEvent(SceneEvent sceneEvent)
    {
        switch (sceneEvent.SceneEventType)
        {
            // Local to individual clients
            case SceneEventType.LoadComplete:
                if (sceneEvent.ClientId == NetworkManager.Singleton.LocalClientId)
                {
                    SceneChanged?.Invoke();

                    // Allows island generator to control loading screen
                    if (!FindAnyObjectByType<IslandGenerator>())
                        loadingScreen.SetActive(false);
                }

                ClientFinishedLoading?.Invoke(sceneEvent.ClientId);
                break;
            
            // Runs when all clients have loaded
            case SceneEventType.LoadEventCompleted:
                if (NetworkManager.Singleton.IsServer)
                    AllPlayersLoaded();
                break;
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
            if (i >= spawnpoints.Length || NetworkManager.Singleton.ConnectedClientsList[i].PlayerObject == null || spawnpoints[i] == null)
                break;
            
            var player =  NetworkManager.Singleton.ConnectedClientsList[i].PlayerObject;
            PlayerController controller = player.GetComponent<PlayerController>();

            controller.enabled = false;
            player.transform.position = spawnpoints[i].transform.position;
            controller.enabled = true;

            if (controller != null && controller.isSteering)
            {
                FindAnyObjectByType<SteeringWheel>().TrySteerShip(controller);
            }
        }
    }
}