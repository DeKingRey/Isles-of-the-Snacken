using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;

public class PlayerCam: NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private Transform playerModel; // Rotates Y, left/right
    [SerializeField] private Transform cameraHolder; // Rotates X, up/down

    [Space(10)]

    [Header("Sensitivity")]
    [SerializeField] private float sensX = 200f;
    [SerializeField] private float sensY = 200f;
    [SerializeField] private float sensitivityMultiplier = 1f;

    [Space(10)]

    [Header("Clamp")]
    [SerializeField] private float minX = -85f;
    [SerializeField] private float maxX = 85f;

    private float xRotation;

    void Start()
    {
        if (!IsOwner)
        {
            DisableCamera();
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!IsOwner) return;

        Look();
    }

    void Look()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * sensX * sensitivityMultiplier * Time.deltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * sensY * sensitivityMultiplier * Time.deltaTime;

        // Vertical Rotation
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minX, maxX);

        cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Horizontal Rotation
        playerModel.Rotate(Vector3.up * mouseX);
    }

    void DisableCamera()
    {
        // Disable real camera
        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null) cam.enabled = false;

        // Disable audio listener
        AudioListener listener = GetComponentInChildren<AudioListener>();
        if (listener != null) listener.enabled = false;

        // Disable Cinemachine
        CinemachineBrain brain = GetComponentInChildren<CinemachineBrain>();
        if (brain != null) brain.enabled = false;

        if (virtualCamera != null) virtualCamera.gameObject.SetActive(false);
    }
}