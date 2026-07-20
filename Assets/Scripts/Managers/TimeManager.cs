using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;

public class TimeManager : NetworkBehaviour
{
    [Header("Game States")]
    [SerializeField] private GameManager.GameState dayCompleteState;
    [SerializeField] private GameManager.GameState playingState;

    [Header("UI Objects")]
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private RectTransform timeArrow;

    [SerializeField] private float startArrowRotation;
    [SerializeField] private float endArrowRotation;
    private Quaternion targetArrowRotation;

    [Header("Time Settings")]
    public float dayDurationSeconds = 600;
    [SerializeField] private int startHour = 6;
    [SerializeField] private int endHour = 18;

    [Space(10)]

    [Header("Snacken")]
    [SerializeField] private GameObject snackenObj;
    [SerializeField] private Vector3 snackenEndPos;

    [Space(10)]

    [Header("Sky")]
    [SerializeField] private float startThickness = 0.5f;
    [SerializeField] private float endThickness = 5f;

    private Vector3 snackenStartPos;
    private Material skyboxMaterial;

    private NetworkVariable<float> elapsedTime = new();
    private NetworkVariable<int> currentHour = new();

    private float secondsPerHour;
    private float hourTimer;
    private bool dayEnded = false;
    
    public override void OnNetworkSpawn()
    {
        secondsPerHour = dayDurationSeconds / (endHour - startHour);
        snackenStartPos = snackenObj.transform.position;

        skyboxMaterial = RenderSettings.skybox;
        skyboxMaterial.SetFloat("_AtmosphereThickness", startThickness);

        if (IsServer)
        {
            elapsedTime.Value = 0f;
            currentHour.Value = startHour;
            hourTimer = 0f;
        }
        UpdateUI(0);
    }

    void Update()
    {   
        float t = Mathf.Clamp01(elapsedTime.Value / dayDurationSeconds); // Percentage of time passed
        if (IsServer && GameManager.Instance.State.Value == playingState && !dayEnded)
        {
            elapsedTime.Value += Time.deltaTime;

            // Hour increment
            hourTimer += Time.deltaTime;
            if (hourTimer >= secondsPerHour)
            {
                hourTimer -= secondsPerHour; // Resets time
                currentHour.Value++;
            }

            // Makes the snacken rise over time
            snackenObj.transform.position = Vector3.Lerp(snackenStartPos, snackenEndPos, t);

            if (currentHour.Value >= endHour)
            {
                dayEnded = true;
                GameManager.Instance.ChangeState(dayCompleteState, 0);
            }
        }
        
        // Changes sky to red over time
        float thickness = Mathf.Lerp(startThickness, endThickness, t);  
        skyboxMaterial.SetFloat("_AtmosphereThickness", thickness);
        UpdateUI(t); // Client-side
    }

    void UpdateUI(float t)
    {
        if (timeText == null) return;

        int hour = currentHour.Value % 12;
        if (hour == 0) hour = 12;

        string meridiem = currentHour.Value < 12 ? "AM" : "PM";

        timeText.text = $"{hour:00}:00 {meridiem}";
        
        // Smoothly rotates arrow in accordance to day time
        timeArrow.localRotation = Quaternion.Lerp(
            Quaternion.Euler(0, 0, startArrowRotation),
            Quaternion.Euler(0, 0, endArrowRotation),
            t);
    }
}
