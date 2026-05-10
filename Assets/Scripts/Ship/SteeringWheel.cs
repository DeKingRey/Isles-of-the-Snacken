using UnityEngine;

public class SteeringWheel : MonoBehaviour
{
    private ShipController ship;

    private void Awake()
    {
        ship = GetComponentInParent<ShipController>();
    }

    public void TrySteerShip(PlayerController player)
    {
        if (ship.HasDriver) return;

        Debug.Log($"Request steer: {player.OwnerClientId}");

        ship.RequestSteerRpc(player.OwnerClientId);
    }
}
