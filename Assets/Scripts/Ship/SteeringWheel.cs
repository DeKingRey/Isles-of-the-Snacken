using UnityEngine;

public class SteeringWheel : MonoBehaviour
{
    private ShipController ship;

    private void Awake()
    {
        ship = FindAnyObjectByType<ShipController>();
    }

    public void TrySteerShip(PlayerController player)
    {
        if (ship.HasDriver) return;

        ship.RequestSteerRpc(player.OwnerClientId);
    }
}
