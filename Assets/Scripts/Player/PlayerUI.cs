using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject menu;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private Slider healthSlider;

    [Space(10)]

    [Header("Inventory UI")]
    [SerializeField] private GameObject inventoryMenu;

    [Space(5)]

    [Tooltip("In game inventory content box")]
    [SerializeField] private Transform invGameTransform;

    [Tooltip("Menu inventory content box")]
    [SerializeField] private GameObject invMenuTransform;

    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private GameObject itemMenuPrefab;

    [Space(10)]

    [Header("Trap UI")]
    [SerializeField] private Transform trapUITransform; 
    [SerializeField] private GameObject trapUIPrefab;

    private PlayerController player;
    private PlayerCam playerCam;
    private HealthManager healthManager;
    private TrapGun trapGun;

    private PlayerInventory playerInventory;
    private List<GameObject> itemsGame = new List<GameObject>();
    private List<GameObject> itemsMenu = new List<GameObject>();
    private List<GameObject> trapsUI = new List<GameObject>();

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

    public void BindHealth(HealthManager h)
    {
        healthManager = h;
        healthSlider.maxValue = healthManager.maxHealth;
    }

    public void BindTrapGun(TrapGun tg, TrapSlot[] traps)
    {
        trapGun = tg;

        foreach (GameObject trap in trapsUI)
        {
            Destroy(trap);
        }

        for (int i = 0; i < traps.Length; i++)
        {
            AddTrapUI(traps[i].trapSprite, i);
        }

        SelectTrapUI(0); // Highlights first trap
    }

    void Update()
    {
        if (player == null || healthManager == null) return;

        staminaSlider.value = Mathf.Clamp(player.smoothedSprintValue, 0f, staminaSlider.maxValue);
        healthSlider.value = Mathf.Clamp(healthManager.currentHealth.Value, 0f, healthSlider.maxValue);

        if (Input.GetKeyDown(KeyCode.Escape)) ToggleMenu();

        if (Input.GetKeyDown(KeyCode.Tab)) ToggleInventoryMenu();
    }

    public void TrapUICooldown(float cooldown)
    {
        foreach (var trap in trapsUI)
        {
            StartCoroutine(CooldownWindDown(cooldown, trap.GetComponent<TrapSlotUI>().cooldownOverlay));
        }
    }

    private IEnumerator CooldownWindDown(float cooldown, Image cooldownOverlay)
    {
        float elapsedTime = 0f;
        cooldownOverlay.fillAmount = 1;

        while (elapsedTime <= cooldown)
        {
            elapsedTime += Time.deltaTime;
            cooldownOverlay.fillAmount = 1 - (elapsedTime / cooldown);
            yield return null;
        }

        cooldownOverlay.fillAmount = 0;
    }

    public void AddTrapUI(Sprite trapSprite, int trapIndex)
    {
        // In game trap UI slot
        GameObject trapUI = Instantiate(trapUIPrefab, trapUITransform);
        trapUI.GetComponent<TrapSlotUI>().trapUISprite.sprite = trapSprite;
        trapUI.GetComponent<TrapSlotUI>().slotNumberText.text = $"{trapIndex + 1}"; // Keybind
        trapsUI.Add(trapUI);
    }

    public void SelectTrapUI(int trapIndex)
    {
        // Highlights selected trap
        for (int i = 0; i < trapsUI.Count; i++)
        {
            trapsUI[i].GetComponent<TrapSlotUI>().slotBorder.color = i == trapIndex ? Color.green : Color.black;
        }
    }

    public void AddItemUI(Sprite itemSprite)
    {
        // In game item UI
        GameObject itemUI = Instantiate(itemPrefab, invGameTransform);
        itemUI.transform.GetChild(0).GetComponent<Image>().sprite = itemSprite;
        itemsGame.Add(itemUI);

        // Menu item UI
        // Insantiates as a child of the menu content
        GameObject menuItemUI = Instantiate(itemMenuPrefab, invMenuTransform.transform);
        menuItemUI.transform.GetChild(0).GetComponent<Image>().sprite = itemSprite;
        itemsMenu.Add(menuItemUI);

        // Assigning item button index data
        menuItemUI.GetComponentInChildren<ItemButton>(true).AssignData(itemsMenu.IndexOf(menuItemUI), playerInventory);
    }

    public void RemoveItemUI(int index)
    {
        // In game item UI
        Destroy(itemsGame[index]);
        itemsGame.RemoveAt(index);

        // Menu item UI
        Destroy(itemsMenu[index]);
        itemsMenu.RemoveAt(index);

        RefreshButtonIndices();
    }

    /// Corrects button data indexes when an item is removed
    private void RefreshButtonIndices()
    {
        // Refresh in-game and menu item indices
        for (int i = 0; i < itemsGame.Count; i++)
        {
            itemsGame[i].GetComponent<ItemButton>().AssignData(i, playerInventory);
            itemsMenu[i].GetComponent<ItemButton>().AssignData(i, playerInventory);
        }
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