using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerBag : MonoBehaviour
{
    [SerializeField] private int maxCapacity = 1;

    private List<Item> items = new List<Item>();
    private int capacity = 0;
    private float weight = 0;

    private void AddItem(Item item)
    {
        items.Add(item);
        weight += item.itemData.weight;
        capacity += 1;
    }

    public bool TryAddItem(Item item)
    {
        // Bag is full
        if (capacity + 1 > maxCapacity)
            return false;
        
        AddItem(item);
        return true;
    }
}
