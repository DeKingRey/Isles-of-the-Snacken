using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private GameObject menu;
    [SerializeField] private Slider staminaSlider;

    private PlayerController player;
    private PlayerCam playerCam;
    private bool menuOpen;

    public void BindPlayer(PlayerController p)
    {
        player = p;
    }

    public void BindCamera(PlayerCam c)
    {
        playerCam = c;
    }

    void Update()
    {
        if (player == null) return;

        staminaSlider.value = Mathf.Clamp(player.smoothedSprintValue, 0f, staminaSlider.maxValue);

        if (Input.GetKeyDown(KeyCode.Escape)) ToggleMenu();
    }

    void ToggleMenu()
    {
        menuOpen = !menuOpen;
        menu.SetActive(menuOpen);

        // Toggles input
        player.ToggleInput(!menuOpen);
        playerCam.ToggleInput();

        // Toggles cursor usability
        Cursor.visible = menuOpen;
        Cursor.lockState = menuOpen ? CursorLockMode.None : CursorLockMode.Locked;
    }
}