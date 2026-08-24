using Unity.Netcode;
using UnityEngine;

public class Coin : NetworkBehaviour
{
    [Tooltip("Y position that the coin will float down to")]
    [SerializeField] private float targetY = 4f;
    [SerializeField] private float floatSpeed = 5f;

    void Update()
    {
        if (!IsServer) return;

        // Floats towards ground level
        Vector3 position = transform.position;

        position.y = Mathf.Lerp(position.y, targetY, floatSpeed * Time.deltaTime);

        transform.position = position;
    }

    void OnTriggerEnter(Collider obj)
    {
        if (!IsServer) return; // Server authorative

        // Collects coin
        if (obj.CompareTag("Ship"))
        {
            QuotaManager.Instance.coinAmount--;
            GameManager.Instance.AddCoin();
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
