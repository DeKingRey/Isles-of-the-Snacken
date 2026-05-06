using UnityEngine;

public class Trap : MonoBehaviour
{
    [SerializeField] private float expiryTime;

    public void Activate()
    {
        Debug.Log("Activate");
    }
}
