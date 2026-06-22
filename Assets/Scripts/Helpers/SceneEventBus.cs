using UnityEngine;
using System;
using System.Collections;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.VectorGraphics;

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
        if (sceneEvent.SceneEventType == SceneEventType.LoadComplete)
        {
            ClientFinishedLoading?.Invoke(sceneEvent.ClientId);

            if (sceneEvent.ClientId == NetworkManager.Singleton.LocalClientId)
            {
                SceneChanged?.Invoke();
                OnClientFinishedLoading(sceneEvent.ClientId);
            }
        }
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
        // Returns if there is an island generator in scene
        if (FindAnyObjectByType<IslandGenerator>()) return; // Allows island generator to control loading
        
        ToggleLoadingScreenRpc(false);
    }

    [Rpc(SendTo.Everyone)]
    public void ToggleLoadingScreenRpc(bool isActive)
    {
        loadingScreen.SetActive(isActive);
    }
}