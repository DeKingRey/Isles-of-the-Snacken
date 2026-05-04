using UnityEngine;

public class TrapGun : MonoBehaviour
{
    private PlayerController player;

    void Start()
    {
        player = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (!player.inputEnabled) return;

        if (Input.GetMouseButtonDown())
    }
}
