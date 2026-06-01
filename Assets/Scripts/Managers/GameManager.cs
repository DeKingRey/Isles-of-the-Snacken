using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public ItemData[] itemDatabase;
    public NommianDatabase nommianDatabase;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    public int GetItemId(ItemData data)
    {
        for (int i = 0; i < itemDatabase.Length; i++)
        {
            if (itemDatabase[i] == data)
                return i;
        }

        return -1; // Not found
    }
}