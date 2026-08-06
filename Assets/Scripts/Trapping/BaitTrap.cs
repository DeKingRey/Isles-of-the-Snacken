using UnityEngine;
using System.Collections;

/// <summary>
/// The bait trap is a manually activated trap
/// It insta kills content within (this may be changed if trap is moved or otherwise)
/// Players can easily harvest content inside
/// </summary>
public class BaitTrap : Trap
{
    [SerializeField] private float trapDuration;

    public override void Activate(float contentWeight)
    {
        base.Activate();
        StartCoroutine(TrapContents());
    }

    // Brief time in which trap will check for contents 
    private IEnumerator TrapContents()
    {
        canCapture = true;

        yield return new WaitForSeconds(trapDuration);

        canCapture = false;
    }
}
