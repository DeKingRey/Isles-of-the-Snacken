using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private GameObject menu;
    [SerializeField] private Slider staminaSlider;

    [Space(10)]

    [Header("Inventory UI")]
    [Tooltip("In game inventory transform")]
    [SerializeField] private Transform invGameTransform;
    [SerializeField] private GameObject inventoryMenu;
    [SerializeField] private GameObject itemPrefab;

    private PlayerController player;
    private PlayerCam playerCam;

    private PlayerInventory playerInventory;
    private List<GameObject> itemsGame = new List<GameObject>();
    private List<GameObject> itemsMenu = new List<GameObject>();

    private bool menuOpen;
    private bool inventoryOpen;

    public void BindPlayer(PlayerController p)
    {
        player = p;
        staminaSlider.maxValue = player.maxStamina;
    }

    public void BindCamera(PlayerCam c)
    {
        playerCam = c;
    }

    public void BindInventory(PlayerInventory i)
    {
        playerInventory = i;
    }

    void Update()
    {
        if (player == null) return;

        staminaSlider.value = Mathf.Clamp(player.smoothedSprintValue, 0f, staminaSlider.maxValue);

        if (Input.GetKeyDown(KeyCode.Escape)) ToggleMenu();

        if (Input.GetKeyDown(KeyCode.Tab)) ToggleInventoryMenu();
    }

    public void AddItemUI(Sprite itemSprite)
    {
        // In game item UI
        GameObject itemUI = Instantiate(itemPrefab, invGameTransform);
        itemUI.transform.GetChild(0).GetComponent<Image>().sprite = itemSprite;
        itemsGame.Add(itemUI);

        // Menu item UI
        GameObject menuItemUI = Instantiate(itemPrefab, inventoryMenu.transform);
        menuItemUI.transform.GetChild(0).GetComponent<Image>().sprite = itemSprite;
        itemsMenu.Add(menuItemUI);
    }

    public void RemoveItemUI(int index)
    {
        // In game item UI
        Destroy(itemsGame[index]);
        itemsGame.RemoveAt(index);

        // Menu item UI
        Destroy(itemsMenu[index]);
        itemsMenu.RemoveAt(index);
    }

    void ToggleMenu()
    {
        menuOpen = !menuOpen;
        menu.SetActive(menuOpen);

        // Toggles input
        player.ToggleInput(!menuOpen);
        playerCam.ToggleInput(!menuOpen);

        // Toggles cursor usability
        Cursor.visible = menuOpen;
        Cursor.lockState = menuOpen ? CursorLockMode.None : CursorLockMode.Locked;
    }

    void ToggleInventoryMenu()
    {
        inventoryOpen = !inventoryOpen;
        inventoryMenu.SetActive(inventoryOpen);

        // Toggles input
        player.ToggleInput(!inventoryOpen);
        playerCam.ToggleInput(!inventoryOpen);

        // Toggles cursor usability
        Cursor.visible = inventoryOpen;
        Cursor.lockState = inventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
    }
}