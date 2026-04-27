using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private Slider staminaSlider;

    private PlayerController player;

    public void Bind(PlayerController p)
    {
        player = p;
    }

    void Update()
    {
        if (player == null) return;

        staminaSlider.value = Mathf.Clamp(player.smoothedSprintValue, 0f, staminaSlider.maxValue);
    }
}