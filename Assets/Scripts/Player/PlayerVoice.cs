using UnityEngine;
using Unity.Netcode;
using Unity.Services.Vivox;

public class PlayerVoice : NetworkBehaviour
{
    [SerializeField] private GameObject head;

    void LateUpdate()
    {
        if (!IsOwner) return;

        VivoxService.Instance.Set3DPosition(
            head,
            VoiceManager.Instance.channelName
        );
    }
}
