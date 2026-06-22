using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using Unity.Netcode;

/// This is used for all sorts of collection/harvesting/delivering
/// Delivering nommians, collecting nommians, picking up items
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

    [Header("References")]
    [SerializeField] private GameObject interactUI;
    [SerializeField] private Image progressRing;

    public event Action OnInteractComplete;
    [HideInInspector] public bool canInteract = false;

    private Camera cam;

    private float elapsedHoldTime = 0f;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        cam = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponentInChildren<Camera>();
    }

    void Update()
    {
        if (!IsOwner || interactUI == null || progressRing == null || cam == null) return;

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

        if (!Physics.SphereCast(cam.transform.position, rayRadius, cam.transform.forward, out hit, rayDistance, interactLayer))
        {
            elapsedHoldTime = 0f;
            return;
        }

        // Hold down to collect
        if (Input.GetKey(KeyCode.E))
        {
            if (!holdToInteract)
            {
                canInteract = false;
                return;
            }

            elapsedHoldTime += Time.deltaTime;

            // Collects
            if (elapsedHoldTime >= interactHoldTime && canInteract)
            {
                OnInteractComplete?.Invoke();
            }
        } else
        {
            elapsedHoldTime -= Time.deltaTime;
            if (elapsedHoldTime < 0) elapsedHoldTime = 0f;
        }
    }
}
