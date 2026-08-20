using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SnackenSceneTrigger : NetworkBehaviour
{
    [Header("Cutscene")]
    [SerializeField] private Camera cutsceneCam;
    [SerializeField] private Transform targetShipPos;
    [SerializeField] private float stoppingDistance = 0.5f;
    [SerializeField] private float closeMouthDistance = 30f;
    [SerializeField] private float inhaleSpeed = 5f;
    private bool isLoading = false;
    private Animator anim;
    private Transform shipTransform;
    private bool mouthClosed = false;

    private void Start()
    {
        if (!IsServer) return;

        anim = GetComponentInParent<Animator>();
    }

    void OnTriggerEnter(Collider obj)
    {
        if (!IsServer) return;

        if (obj.CompareTag("Ship") && !isLoading)
        {
            isLoading = true; // Safety check so doesn't load twice

            shipTransform = obj.GetComponentInParent<ShipController>().gameObject.transform;
            GameManager.Instance.ChangeState(GameManager.GameState.SnackenEating);

            StartCoroutine(EatShip());
        }
    }

    private IEnumerator EatShip()
    {
        float distance = Vector3.Distance(shipTransform.position, targetShipPos.position);

        // Disables player movement
        StartCutsceneRpc();

        // Moves the ship towards the snacken (giving the effect that the snacken is inhaling them)
        while (distance > stoppingDistance)
        {
            distance = Vector3.Distance(shipTransform.position, targetShipPos.position);
            shipTransform.position = Vector3.MoveTowards(shipTransform.position, targetShipPos.position, inhaleSpeed * Time.deltaTime);

            if (!mouthClosed && distance < closeMouthDistance)
            {
                anim.SetBool("mouthOpen", false);
            }

            yield return null;
        }

        shipTransform.position = targetShipPos.position;

        IslandGenerator.Instance.ClearIslands(); // Despawns islands

        // Despawns nommians
        foreach (var spawner in FindObjectsByType<NommianSpawner>())
        {
            spawner.DespawnNommians();
        }

        SceneEventBus.Instance.ToggleLoadingScreenRpc(true);
        NetworkManager.Singleton.SceneManager.LoadScene("Snacken", LoadSceneMode.Single);
    }

    [Rpc(SendTo.Everyone)]
    private void StartCutsceneRpc()
    {
        var playerObject = NetworkManager.Singleton.LocalClient.PlayerObject;

        if (playerObject == null)
            return;

        playerObject.GetComponent<PlayerController>().ToggleInput(false);
        playerObject.GetComponent<PlayerCam>().ToggleInput(false);

        cutsceneCam.gameObject.SetActive(true);
    }
}
