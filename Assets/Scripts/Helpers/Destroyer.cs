using UnityEngine;

public class Destroyer : MonoBehaviour
{
    [SerializeField] private float lifetime = 5f;

    void Start()
    {
        Destroy(this, lifetime);
    }
}
