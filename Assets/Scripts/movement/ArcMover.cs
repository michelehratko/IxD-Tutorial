using UnityEngine;

public class ArcMover : MonoBehaviour
{
    [Header("Arc Motion")]
    public float minRadius = 4f;
    public float maxRadius = 8f;

    public Vector2 arcDegreesRange = new Vector2(60f, 140f);
    public Vector2 durationRange = new Vector2(1.5f, 3.5f);

    [Header("Bounds")]
    public CircleBoundsController bounds;

    [Header("Trail")]
    public dotTrail trail;

    private Vector3 center;
    private float radius;
    private float startAngle;
    private float endAngle;
    private float duration;
    private float t;

    void Start()
    {
        // auto-find bounds if missing
        if (!bounds)
            bounds = FindFirstObjectByType<CircleBoundsController>();

        // auto-find trail if missing
        if (!trail)
            trail = GetComponentInChildren<dotTrail>();

        if (trail)
        {
            trail.SetTarget(transform);
            trail.ResetTrailImmediate();
        }

        StartNewArc(transform.position, RandomDirection());
    }

    void Update()
    {
        if (duration <= 0f) return;

        t += Time.deltaTime / duration;
        float k = Mathf.Clamp01(t);

        float angle = Mathf.Lerp(startAngle, endAngle, k) * Mathf.Deg2Rad;

        Vector3 pos = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

        // stay inside circle
        if (bounds && !bounds.IsInside(pos))
        {
            Vector3 dir = (bounds.Center - transform.position).normalized;
            StartNewArc(transform.position, DirectionToCenter());
            return;
        }

        transform.position = pos;

        // arc finished → start new one
        if (k >= 1f)
        {
            StartNewArc(transform.position, RandomDirection());
        }
    }

    void StartNewArc(Vector3 startPos, Vector3 direction)
    {
        radius = Random.Range(minRadius, maxRadius);
        float arcDeg = Random.Range(arcDegreesRange.x, arcDegreesRange.y);
        duration = Random.Range(durationRange.x, durationRange.y);

        if (duration <= 0.01f) duration = 0.5f;

        // perpendicular direction
        Vector3 right = Vector3.Cross(Vector3.up, direction.normalized);

        // randomly choose left/right arc
        float side = (Random.value < 0.5f) ? 1f : -1f;

        center = startPos + right * radius * side;

        Vector3 toStart = (startPos - center).normalized;
        startAngle = Mathf.Atan2(toStart.z, toStart.x) * Mathf.Rad2Deg;

        float dir = (Random.value < 0.5f) ? 1f : -1f;
        endAngle = startAngle + arcDeg * dir;

        t = 0f;

        if (trail)
        {
            trail.SetTarget(transform);
            trail.ResetTrailImmediate();
        }
    }

    Vector3 RandomDirection()
    {
        Vector3 v = Random.insideUnitSphere;
        v.y = 0f;
        return v.normalized == Vector3.zero ? Vector3.forward : v.normalized;
    }

    Vector3 DirectionToCenter()
    {
        if (!bounds) return Vector3.forward;

        Vector3 dir = (bounds.Center - transform.position);
        dir.y = 0f;

        return dir.normalized == Vector3.zero ? Vector3.forward : dir.normalized;
    }
}