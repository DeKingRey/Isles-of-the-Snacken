using UnityEngine;

public class ItemButton : MonoBehaviour
{
    private int itemIndex;
    private PlayerInventory inventory;

    public void AssignData(int index, PlayerInventory inv)
    {
        itemIndex = index;
        inventory = inv;
    }

    // Will remove item from inventory when button is pressed
    public void DeleteButtonPressed()
    {
        inventory.DropItem(itemIndex);
    }
}
