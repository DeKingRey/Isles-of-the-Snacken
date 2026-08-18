using UnityEngine;

public class PlayerHead : MonoBehaviour
{
    private HealthManager healthManager;

    void Start()
    {
        healthManager = GetComponentInParent<HealthManager>();
    }

    void OnTriggerEnter(Collider obj)
    {
        if (obj.CompareTag("Ocean"))
        {
            healthManager.IsUnderwater(true);
        }
    }

    void OnTriggerExit(Collider obj)
    {
        if (obj.CompareTag("Ocean"))
        {
            healthManager.IsUnderwater(false);
        }
    }
}
