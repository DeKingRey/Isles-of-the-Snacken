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

public class RelayManager : MonoBehaviour
{
    public RelayManager Instance;

    [SerializeField] private TextMeshProUGUI joinCodeText;
    [SerializeField] private TMP_InputField joinCodeInput;

    private bool isStartingHost;
    private bool isJoining;

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
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        await VoiceManager.Instance.InitializeVoiceAsync();
    }

    public async void StartRelay()
    {
        if (isStartingHost || NetworkManager.Singleton.IsListening) return;

        isStartingHost = true;

        string joinCode = await StartHostWithRelay();
        joinCodeText.text = joinCode;

        isStartingHost = false;
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        Debug.Log("Relay Host Config Applied");
        Debug.Log(transport.ConnectionData.Address);
    }

    public async void JoinRelay()
    {
        if (isJoining || NetworkManager.Singleton.IsListening) return;
        
        isJoining = true;

        await StartClientWithRelay(joinCodeInput.text);

        isJoining = false;
    }

    public void CopyToClipboard()
    {
        GUIUtility.systemCopyBuffer = joinCodeText.text;
    }

    private async Task<string> StartHostWithRelay(int maxConnections = 4)
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        return NetworkManager.Singleton.StartHost() ? joinCode : null;
    }

    private async Task<bool> StartClientWithRelay(string joinCode)
    {
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));

        return !string.IsNullOrEmpty(joinCode) && NetworkManager.Singleton.StartClient();
    }
}
