using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartGameButton : NetworkBehaviour
{
    [SerializeField] private GameObject collectUI;
    [SerializeField] private Image progressRing;
    
    private Camera cam;
    private Interactable interaction;

    public override void OnNetworkSpawn()
    {
        if (!IsHost) return;

        cam = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponentInChildren<Camera>();

        // Assigns interaction
        interaction = GetComponent<Interactable>();
        interaction.OnInteractComplete += StartGame;
        interaction.AssignVariables(collectUI, progressRing, cam);
        interaction.canInteract = true;
    }

    private void StartGame()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            SceneEventBus.Instance.ToggleLoadingScreenRpc(true);
            NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
        }
    }
}