using UnityEngine;
using Unity.Netcode;

public abstract class Trap : MonoBehaviour
{
    [HideInInspector] public bool canCapture;
    private Animator anim;
    [HideInInspector] public TrapGun gun;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    public virtual void Activate()
    {
        anim.SetTrigger("Activate");
    }
}
