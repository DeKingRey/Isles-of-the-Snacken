using UnityEngine;

public abstract class Trap : MonoBehaviour
{
    [HideInInspector] public bool canCapture;
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    public virtual void Activate()
    {
        anim.SetTrigger("Activate");
    }
}
