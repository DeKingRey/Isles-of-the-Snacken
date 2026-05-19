using UnityEngine;

public enum EntityType
{
    Player,
    Nommian
}

public class Item : MonoBehaviour
{
    public ItemData itemData;
    public EntityType type;
}
