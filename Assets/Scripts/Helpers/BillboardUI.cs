using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BillboardUI : MonoBehaviour
{
    private Camera cam;

    void LateUpdate()
    {
        cam = NetworkManager.Singleton?.LocalClient?.PlayerObject?.GetComponentInChildren<Camera>();

        if (cam == null)
            return;
        
        transform.LookAt(transform.position + cam.transform.rotation * Vector3.forward, cam.transform.rotation * Vector3.up);
    }
}