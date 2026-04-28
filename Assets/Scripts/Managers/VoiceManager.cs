using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Vivox;
using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;

public class VoiceManager : MonoBehaviour
{
    public static VoiceManager Instance;
    public string channelName = "GameChannel";

    public bool IsVivoxReady = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    public async Task InitializeVoiceAsync()
    {
        await VivoxService.Instance.InitializeAsync();

        LoginOptions options = new LoginOptions
        {
            DisplayName = "Player_" + AuthenticationService.Instance.PlayerId
        };
        await VivoxService.Instance.LoginAsync(options);

        Channel3DProperties channelProperties = new Channel3DProperties(
            32,
            1,
            1f,
            AudioFadeModel.InverseByDistance
        );

        await VivoxService.Instance.JoinPositionalChannelAsync(channelName, ChatCapability.AudioOnly, channelProperties);

        IsVivoxReady = true;
    }
}
