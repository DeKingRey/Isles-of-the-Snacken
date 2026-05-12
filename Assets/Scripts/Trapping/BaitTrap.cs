using UnityEngine;
using System.Collections;

public class BaitTrap : Trap
{
    [SerializeField] private float trapDuration;

    public override void Activate()
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
