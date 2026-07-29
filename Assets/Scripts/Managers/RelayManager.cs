using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System;
using UnityEngine.SceneManagement;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine.UI;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance;

    [Header("References")]
    [SerializeField] private GameObject joinButton;
    [SerializeField] private TextMeshProUGUI joinCodeText;
    [SerializeField] private TMP_InputField joinCodeInput;

    [SerializeField] private GameObject connectingPanel;
    [SerializeField] private GameObject menuPanel;

    [HideInInspector] public string joinCode {get; private set; }
    private string lobbyId; 

    private bool isStartingHost;
    private bool isJoining;
    private bool servicesReady;

    private void Awake()
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

    private async void Start()
    {
        try 
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            servicesReady = true;

            joinCodeInput.onValueChanged.AddListener(OnJoinInputChanged);

            if (connectingPanel != null)
                connectingPanel.SetActive(false);
            if (menuPanel != null)
                menuPanel.SetActive(true);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return;
        }

        try
        {
            await VoiceManager.Instance.InitializeVoiceAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Vivox failed to initialise: {e.Message}");
        }
    }

    // Awaits input from the join code
    // If the code is valid (6 chars) then the join button will enable
    private void OnJoinInputChanged(string text)
    {
        bool isValid = text.Length == 6;
        joinButton.GetComponent<Button>().interactable = isValid;
        joinButton.GetComponent<CanvasGroup>().alpha = isValid ? 1f : 0.5f;
    }

    public async void StartHost()
    {
        if (isStartingHost || NetworkManager.Singleton.IsListening || !servicesReady) return;

        SceneEventBus.Instance.ToggleLoadingScreenRpc(true);
        isStartingHost = true;

        joinCode = await StartHostWithRelay();

        isStartingHost = false;
        
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        }
    }

    public async void StartClientWithCode()
    {
        await StartClient();
    }

    public async Task StartClient(string code="")
    {
        if (isJoining || NetworkManager.Singleton.IsListening) return;
        
        SceneEventBus.Instance.ToggleLoadingScreenRpc(true);
        isJoining = true;
        
        // Sets code to the input if player manually inputted code
        if (code == "")
        {
            code = joinCodeInput.text;
        }

        await StartClientWithRelay(code);

        isJoining = false;
    }

    private async Task<string> StartHostWithRelay(int maxConnections = 4)
    {
        // Creates relay allocation
        Allocation allocation;
        try 
        {
            allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        }
        catch (Exception e)
        {
            Debug.LogError($"Relay create allocation request failed: {e.Message}");
            SceneEventBus.Instance.ToggleLoadingScreenRpc(false);
            throw;
        }

        // Gets join code
        string code;
        try
        {
            code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        }
        catch
        {
            Debug.LogError("Relay get join code request failed");
            SceneEventBus.Instance.ToggleLoadingScreenRpc(false);
            throw;
        }

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

        try
        {
            var createLobbyOptions = new CreateLobbyOptions();
            createLobbyOptions.IsPrivate = false; // Make optional later
            createLobbyOptions.Data = new Dictionary<string, DataObject>()
            {
                {
                    "JoinCode", new DataObject(
                        visibility: DataObject.VisibilityOptions.Member,
                        value: code
                    )
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync("My Lobby", maxConnections, createLobbyOptions);
            lobbyId = lobby.Id;
            StartCoroutine(HeartbeatLobbyCoroutine(15f));
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            SceneEventBus.Instance.ToggleLoadingScreenRpc(false);
            throw;
        }

        return NetworkManager.Singleton.StartHost() ? code : null;
    }

    private async Task<bool> StartClientWithRelay(string code)
    {
        JoinAllocation joinAllocation;

        try
        {
            joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);
        }
        catch
        {
            Debug.LogError("Relay get join code request failed");
            SceneEventBus.Instance.ToggleLoadingScreenRpc(false); 
            throw;
        }
        
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));
        
        return !string.IsNullOrEmpty(code) && NetworkManager.Singleton.StartClient();
    }

    private IEnumerator HeartbeatLobbyCoroutine(float waitTimeSeconds)
    {
        var delay = new WaitForSeconds(waitTimeSeconds);

        while (true)
        {
            _ = SendHeartbeat();
            yield return delay;
        }
    }

    private async Task SendHeartbeat()
    {
        try
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Lobby heartbeat failed: {e}");
        }
    }
}
