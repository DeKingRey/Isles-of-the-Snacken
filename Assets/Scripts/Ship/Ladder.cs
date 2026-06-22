using Unity.Netcode;
using UnityEngine;

public class Ladder : NetworkBehaviour
{
    public Transform ladderTop;
    public Transform ladderBottom;

    private Interactable interaction;
    [HideInInspector] public bool hasPlayer;

    public override void OnNetworkSpawn()
    {
        if (!IsClient) return;

        // Assigns interaction
        interaction = GetComponent<Interactable>();
    }

    void Update()
    {
        if (!IsClient) return;

        if (!hasPlayer) interaction.canInteract = true;
        else interaction.canInteract = false;
    }
}
