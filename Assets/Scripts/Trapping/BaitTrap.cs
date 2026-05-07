using UnityEngine;
using System.Collections;

public class BaitTrap : Trap
{
    [SerializeField] private float trapDuration;

    public override void Activate()
    {
        StartCoroutine(CheckForContents());
    }

    // Brief time in which trap will check for contents 
    private IEnumerator CheckForContents()
    {
        canCapture = true;

        yield return new WaitForSeconds(trapDuration);

        canCapture = false;
    }
}
