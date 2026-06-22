using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class SteeringWheel : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject collectUI;
    [SerializeField] private Image progressRing;
    
    private Camera cam;
    private Interactable interaction;
    private ShipController ship;

    public override void OnNetworkSpawn()
    {
        if (!IsClient) return;

        ship = GetComponentInParent<ShipController>();

        cam = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponentInChildren<Camera>();

        // Assigns interaction
        interaction = GetComponent<Interactable>(); 
        interaction.AssignVariables(collectUI, progressRing, cam);
    }

    void Update()
    {
        if (!IsClient) return;

        if (ship.HasDriver) interaction.canInteract = false;
        else interaction.canInteract = true;
    }

    public void TrySteerShip(PlayerController player)
    {
        if (ship.HasDriver) return;

        ship.RequestSteerRpc(player.OwnerClientId);
    }
}
