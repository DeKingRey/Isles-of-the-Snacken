using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SnackenSceneTrigger : NetworkBehaviour
{
    private bool isLoading = false;

    void OnTriggerEnter(Collider obj)
    {
        if (!IsServer) return;

        if (obj.CompareTag("Ship") && !isLoading)
        {
            isLoading = true; // Safety check so doesn't load twice
            SceneEventBus.Instance.ToggleLoadingScreenRpc(true);
            NetworkManager.Singleton.SceneManager.LoadScene("Snacken", LoadSceneMode.Single);
        }
    }
}
