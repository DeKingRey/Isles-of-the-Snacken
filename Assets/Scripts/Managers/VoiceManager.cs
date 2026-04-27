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

    private async void Awake()
    {
        Instance = this;

        await VivoxService.Instance.InitializeAsync();

        LoginOptions options = new LoginOptions
        {
            DisplayName = "Player_" + AuthenticationService.Instance.PlayerId
        };
        await VivoxService.Instance.LoginAsync(options);

        Channel3DProperties channelProperties = new Channel3DProperties();

        Debug.Log(channelProperties);

        await VivoxService.Instance.JoinPositionalChannelAsync(channelName, ChatCapability.AudioOnly, channelProperties);
    }
}
