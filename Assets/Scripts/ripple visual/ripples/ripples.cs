using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ripples : MonoBehaviour
{
    [Header("Shape")]
    public float maxRadius = 12f;
    public float lineWidth = 0.02f;
    public float lifetime = 4f;
    public int segments = 128;

    [Header("Visual")]
    public float minAlpha = 0.5f;
    public float maxAlpha = 1.0f;

    [HideInInspector] public int stemID = -1;
    [HideInInspector] public float sourceVolume;
    [HideInInspector] public float sourceFrequency;

    private float age;
    private float radius;

    private LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();

        segments = Mathf.Max(segments, 32);

        lr.loop = true;
        lr.positionCount = segments;
        lr.useWorldSpace = true;

        lr.material = new Material(Shader.Find("Sprites/Default"));

        lr.enabled = false;
    }

    void Update()
    {
        age += Time.deltaTime;
        float t = age / lifetime;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        // expansion
        radius = Mathf.Lerp(0f, maxRadius, Mathf.Pow(t, 0.8f));

        if (radius <= 0.001f)
        {
            lr.enabled = false;
            return;
        }

        // now safe to render
        lr.enabled = true;

        BuildCircle(radius);

        // thickness
        lr.widthMultiplier = Mathf.Max(lineWidth, 0.005f);

        // alpha
        float audioAlpha = Mathf.Lerp(minAlpha, maxAlpha, sourceVolume);
        float fade = 1f - t;

        Color c = Color.white;
        c.a = audioAlpha * fade;

        lr.startColor = lr.endColor = c;
    }

    void BuildCircle(float r)
    {
        int count = lr.positionCount;

        if (count < 3)
        {
            lr.enabled = false;
            return;
        }

        for (int i = 0; i < count; i++)
        {
            float a = i * Mathf.PI * 2f / count;
            Vector3 pos = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
            lr.SetPosition(i, transform.position + pos);
        }
    }
}