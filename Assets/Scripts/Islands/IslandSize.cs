using UnityEngine;

public class IslandSize : MonoBehaviour
{
    public float radius = 30f;

    public bool applyLossyScale = true;

    public float GetScaledRadius()
    {
        if (!applyLossyScale)
            return radius;

        float scaleXZ = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        return radius * scaleXZ;
    }


    public void AutoFitRadius()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            return;
        }

        
        Bounds combined = renderers[0].bounds;
        foreach (Renderer r in renderers)
            combined.Encapsulate(r.bounds);

        
        float rawRadius = Mathf.Max(combined.extents.x, combined.extents.z);
        if (applyLossyScale)
        {
            float scaleXZ = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
            if (scaleXZ > 0f)
                rawRadius /= scaleXZ;
        }

        radius = rawRadius;
    }

       void OnDrawGizmosSelected()
    {
        float display = applyLossyScale ? GetScaledRadius() : radius;

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, display);
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, display + 10f);
    }
}