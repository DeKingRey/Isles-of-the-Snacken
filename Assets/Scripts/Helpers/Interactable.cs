using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using Unity.Netcode;

/// This is used for all sorts of collection/harvesting/delivering
/// Delivering nommians, collecting nommians, picking up items
/// This also works for tap to interact objects
public class Interactable : NetworkBehaviour
{
    [Header("Interact Settings")]
    [SerializeField] private float interactHoldTime = 1f;
    [SerializeField] private float rayRadius = 0.5f;
    [SerializeField] private float rayDistance = 5f;

    [Tooltip("Whether you hold down E to interact or its just a popup")]
    [SerializeField] private bool holdToInteract = true;

    [Tooltip("Layer of this object")]
    [SerializeField] private LayerMask interactLayer;

    [Space(10)]
    
    [Header("Range Check")]

    [Tooltip("How far the player can be to see the interact UI")]
    [SerializeField] private float interactRange = 10f;
    [SerializeField] private float rangeCheckInterval = 0.2f;

    [Space(10)]

    [Header("References")]
    [SerializeField] private GameObject interactUI;
    [SerializeField] private Image progressRing;

    public event Action OnInteractComplete;
    [HideInInspector] public bool canInteract = false;

    private Camera cam;

    private float elapsedHoldTime = 0f;
    private bool playerInRange = false;
    private float rangeTimer = 0f;
    private Transform player;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        try
        {
            player = NetworkManager.Singleton.LocalClient.PlayerObject.transform;
            cam = player.GetComponentInChildren<Camera>();
        }
        catch
        {
            Debug.LogWarning("Player not found");
        }
        
    }

    void Update()
    {
        if (!IsOwner || interactUI == null || progressRing == null || cam == null) return;

        if (player == null)
        {
            try
            {
                player = NetworkManager.Singleton.LocalClient.PlayerObject.transform;
                cam = player.GetComponentInChildren<Camera>();
            }
            catch
            {
                Debug.LogWarning("Player not found");
                return;
            }
        }
        
        // Checks if the player is in range every few frames (for perfomance)
        rangeTimer -= Time.deltaTime;
        if (rangeTimer <= 0f)
        {
            rangeTimer = rangeCheckInterval;

            // Checks if player is in range, the check is squared as it is more optimal
            playerInRange = (player.position - transform.position).sqrMagnitude <= interactRange * interactRange;
        }

        // Hides UI if player isn't in range
        if (!playerInRange)
        {
            interactUI.SetActive(false);
            return;
        }

        HandleInteraction();
    }

    public void AssignVariables(GameObject ui, Image ring, Camera c)
    {
        interactUI = ui;
        progressRing = ring;
        cam = c;
    }

    void HandleInteraction()
    {
        RaycastHit hit;

        // Only shows UI if the player can collect
        if (canInteract)
        {
            interactUI.SetActive(true);
            if (holdToInteract) progressRing.fillAmount = elapsedHoldTime / interactHoldTime;
        }
        else
        {
            interactUI.SetActive(false);
            return;
        }

        // Player has to be looking at the interactable to collect it
        if (!Physics.SphereCast(cam.transform.position, rayRadius, cam.transform.forward, out hit, rayDistance, interactLayer))
        {
            elapsedHoldTime = 0f;
            return;
        }

        // Hold down to collect (hold time is 0 for single click objects)
        if (Input.GetKey(KeyCode.E))
        {
            if (!holdToInteract)
            {
                canInteract = false;
                return;
            }

            elapsedHoldTime += Time.deltaTime;

            // Invokes interact action
            if (elapsedHoldTime >= interactHoldTime && canInteract)
            {
                Debug.Log("interact");
                OnInteractComplete?.Invoke();
            }
        } else
        {
            elapsedHoldTime -= Time.deltaTime;
            if (elapsedHoldTime < 0) elapsedHoldTime = 0f;
        }
    }
}
