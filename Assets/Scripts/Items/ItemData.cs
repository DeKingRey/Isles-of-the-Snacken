using UnityEngine;

[CreateAssetMenu(menuName = "Items/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public float weight;
    public float value;

    [Space(5)]

    public GameObject itemModel;
    public GameObject itemSprite;
}