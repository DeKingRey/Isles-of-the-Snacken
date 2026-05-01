using UnityEngine;

public class ShipController : MonoBehaviour
{
    private bool playerSteering = false;

    public void TrySteerShip(PlayerController player)
    {
        if (playerSteering) return;

        playerSteering = true;
        player.ToggleInput();
    }

    private void OnTriggerEnter(Collider obj)
    {
        if (obj.CompareTag("Steering Wheel") && !ship.playerControlling)
        {
            player = obj.GetComponent<PlayerController>();
            player.canSteer = true;
        }
    }

    private void OnTriggerExit(Collider obj)
    {
        if (obj.CompareTag("Steering Wheel") && !ship.playerControlling)
        {
            player = obj.GetComponent<PlayerController>();
            player.canSteer = false;
        }
    }
}
