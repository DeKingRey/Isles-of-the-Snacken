using UnityEngine;
using System.Collections;

public class BearTrap : Trap
{
    [SerializeField] private float trapDuration = 2f;

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
