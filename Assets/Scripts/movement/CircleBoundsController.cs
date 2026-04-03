using UnityEngine;

[ExecuteAlways]
public class CircleBoundsController : MonoBehaviour
{
    [Min(0.1f)] public float radius = 10f;
    public Color gizmoColor = Color.green;

    public Vector3 Center => transform.position;

    public bool IsInside(Vector3 worldPos)
    {
        Vector2 p = new Vector2(worldPos.x, worldPos.z);
        Vector2 c = new Vector2(Center.x, Center.z);
        return (p - c).sqrMagnitude <= radius * radius;
    }

    public Vector3 GetBounceNormal(Vector3 worldPos)
    {
        Vector3 fromCenter = (new Vector3(worldPos.x, 0f, worldPos.z) - new Vector3(Center.x, 0f, Center.z));
        return fromCenter.sqrMagnitude < 0.0001f ? Vector3.forward : fromCenter.normalized;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        const int segs = 96;
        Vector3 last = Center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segs; i++)
        {
            float a = i * Mathf.PI * 2f / segs;
            Vector3 next = Center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
            Gizmos.DrawLine(last, next);
            last = next;
        }
    }
}
