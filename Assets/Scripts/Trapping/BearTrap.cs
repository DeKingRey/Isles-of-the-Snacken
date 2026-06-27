using UnityEngine;
using System.Collections;


/// <summary>
/// Bear trap is an automatic trap
/// When an entity steps on it, it will activate
/// While activated, the entity can struggle for a bit before escaping
/// The bear trap does a fair bit of damage
/// </summary>
public class BearTrap : Trap
{
    [Tooltip("How long the content will struggle before escaping")]
    [SerializeField] private float struggleDuration = 10f;

    public override void Start()
    {
        base.Start();
        canCapture = true;  // Bear trap doesn't need manual activationd
    }

    public override void Activate()
    {
        base.Activate(); // Anim
        StartCoroutine(ContainContent());
    }

    // Brief time in which trap will hold on to entities
    private IEnumerator ContainContent()
    {
        canCapture = false; // Bear trap can no longer capture after activating

        yield return new WaitForSeconds(struggleDuration);

        RemoveContent(); // Removes content so it can escape (if possible), if content has been harvested, this is arbitrary
    }
}
