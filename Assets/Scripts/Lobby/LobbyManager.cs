using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI joinCodeText;

    void Start()
    {
        joinCodeText.text = RelayManager.Instance.joinCode;
    }
}
