using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Vivox;
using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Netcode;

public class PlayerVoice : NetworkBehaviour
{
    [SerializeField] private GameObject head;

    void LateUpdate()
    {
        if (!IsOwner) return;
        if (!VoiceManager.Instance.IsVivoxReady) return;

        VivoxService.Instance.Set3DPosition(
            head,
            VoiceManager.Instance.channelName
        );
    }
}
