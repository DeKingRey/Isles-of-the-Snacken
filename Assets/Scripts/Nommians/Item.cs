using UnityEngine;

[System.Serializable]
public class Data
{
    public string name;
    public float weight;
    public float value;
}

public enum EntityType
{
    Player,
    Nommian
}

public class Item : MonoBehaviour
{
    public Data itemData;
    public EntityType type;
}
