using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class dotTrail : MonoBehaviour
{
    [Header("Dots")]
    public GameObject dotPrefab;
    [Range(2, 64)] public int dotCount = 8;
    public float spacing = 0.5f;
    [Tooltip("Minimum distance between recorded samples (relative to spacing).")]
    public float minSampleStepFactor = 0.1f;

    [Header("Dot Size (head→tail)")]
    [Range(0.005f, 0.5f)] public float dotScaleHead = 0.020f;
    [Range(0.005f, 0.5f)] public float dotScaleTail = 0.015f;

    [Header("Opacity")]
    [Range(0f, 1f)] public float opacityMultiplier = 0.60f;

    [Header("Smoothing")]
    [Range(0, 6)]  public int   maxExtraSamplesPerFrame = 2;
    [Range(0f,45f)]public float angleSampleThresholdDeg = 10f;
    [Range(1f, 20f)]public float alphaFeather = 16f;

    [Header("Approach Fade (before pause)")]
    public float crashFadeWindow = -1f;
    public AnimationCurve crashFadeEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Reveal (after pause)")]
    public float revealDuration = 0.5f;
    public AnimationCurve revealEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Look")]
    public Color headColor = Color.white;
    public Color tailColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [Range(0.5f, 1.5f)] public float headAlpha = 0.90f;
    [Range(0.05f, 1f)]  public float tailAlpha = 0.12f;
    [Range(0f, 0.4f)]   public float hiddenAlpha = 0f;

    [Header("Stationary Marker")]
    public bool showBrightStackMarker = true;

    [Header("Head Brightness")]
    public bool forceHeadAlwaysBright = true;

    [Header("Bounce Feather")]
    public float bounceFeatherTime = 0.18f;

    [Header("Debug")]
    public bool debugLogs = false;

    private enum TrailState { Live, PausedHidden, Revealing }
    private TrailState state = TrailState.Live;

    private class Dot
    {
        public Transform tr;
        public Renderer rd;
        public MaterialPropertyBlock mpb;
        public int colorId;
        public Color rgbCache;
    }

    private readonly List<Dot> dots = new();
    private readonly List<Vector3> history = new();


    void SetDotsEnabled(bool enabled)
    {
        for (int i = 0; i < dots.Count; i++)
        {
            if (dots[i]?.rd != null) dots[i].rd.enabled = enabled;
        }
    }

    public Transform target;
    public void SetTarget(Transform t) => target = t;

    private Vector3 lastSample;
    private Vector3 prevHeadDir;
    private float   minSampleStep;

    private float[] targetAlpha;
    private float[] liveAlpha;

    private enum RevealState { Hidden, Revealing, Shown }
    private RevealState[] rState;
    private float[] rT;

    private Vector3 stackPoint;
    private float bounceTimer;

    const float ALPHA_EPS = 0.0025f;

    // ArcMover may call ResetTrailImmediate() in its Start().
    // Unity does not guarantee script Start() order, so we must be initialized earlier.
    private bool _initialized;

    void Awake()
    {
        EnsureInitialized();
    }

    void Start()
    {
        EnsureInitialized();
        if (_initialized) ResetTrailImmediate();
    }

    void Update()
    {
        if (!target) return;

        if (bounceTimer > 0f) bounceTimer -= Time.deltaTime;

        if (state != TrailState.PausedHidden)
            RecordHeadSamples();

        if (state == TrailState.Revealing)
            DriveReveal();
    }

    void LateUpdate()
    {
        switch (state)
        {
            case TrailState.Live:         LayoutLiveStable();  break;
            case TrailState.PausedHidden: LayoutPausedHidden();break;
            case TrailState.Revealing:    LayoutRevealing();   break;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        for (int i = 0; i < dots.Count; i++)
        {
            float t = (dotCount <= 1) ? 0f : (float)i / (dotCount - 1);
            float s = Mathf.Lerp(dotScaleHead, dotScaleTail, t);
            if (dots[i]?.tr) dots[i].tr.localScale = Vector3.one * s;
        }
    }
#endif

    public void OnBounce(Vector3 hitPoint)
    {
        AddSample(hitPoint);
        AddSample(hitPoint);
        bounceTimer = bounceFeatherTime;
        if (debugLogs) Debug.Log("[dotTrail] bounce feather");
    }

    public void HideAtPausePoint(Vector3 point)
    {
        EnsureInitialized();
        if (!_initialized) return;

        stackPoint = point;

        history.Clear();
        lastSample = stackPoint;
        for (int i = 0; i < 8; i++) history.Add(stackPoint);

        state = TrailState.PausedHidden;


        SetDotsEnabled(false);
        for (int i = 0; i < dotCount; i++) { SetAlphaImmediate(i, 0f); liveAlpha[i] = 0f; }
        for (int i = 0; i < dotCount; i++) { rState[i] = RevealState.Hidden; rT[i] = 0f; }
    }

    public void OnStationaryPauseEndFresh()
    {
        // Intentionally keep the trail hidden here.
        // ArcMover will call ResetTrailImmediate() when the next arc begins.
        state = TrailState.PausedHidden;
    }

    public void ResetTrailImmediate()
    {
        StopAllCoroutines();
        EnsureInitialized();
        if (!_initialized) return;

        SetDotsEnabled(true);

        state = TrailState.Live;

        // Re-seed history at the current head position so we don't "drag" old dots into the next arc.
        Vector3 head = target ? target.position : transform.position;
        history.Clear();
        lastSample = head;
        for (int i = 0; i < Mathf.Max(8, dotCount); i++) history.Add(head);

        // Snap dots to the head and hide them; Live layout will fade them in naturally as samples accumulate.
        for (int i = 0; i < dotCount; i++)
        {
            if (dots[i]?.tr) dots[i].tr.position = head;

            rState[i] = RevealState.Hidden;
            rT[i] = 0f;

            liveAlpha[i] = 0f;
            SetAlphaImmediate(i, 0f);
        }
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;

        if (!target)
        {
            Debug.LogWarning("[dotTrail] No target assigned — disabling.");
            enabled = false;
            return;
        }

        if (!dotPrefab)
        {
            Debug.LogError("[dotTrail] dotPrefab is not assigned.", this);
            return;
        }

        InitializeDots();
        _initialized = true;
    }

    IEnumerator SoftBootReveal()
    {
        float dur = Mathf.Max(0.05f, revealDuration * 0.6f);
        float stagger = dur / Mathf.Max(1, dotCount - 1);

        for (int i = 0; i < dotCount; i++)
        {
            float t0 = Time.time;
            rState[i] = RevealState.Revealing;
            yield return new WaitForSeconds(stagger * 0.15f);
            while (Time.time - t0 < dur)
            {
                float k = (Time.time - t0) / dur;
                k = revealEase.Evaluate(k);
                float goal = (i == 0 && forceHeadAlwaysBright) ? headAlpha : targetAlpha[i];
                SmoothAlphaTo(i, Mathf.Lerp(hiddenAlpha, goal, k));
                yield return null;
            }
            SmoothAlphaTo(i, (i == 0 && forceHeadAlwaysBright) ? headAlpha : targetAlpha[i]);
            rState[i] = RevealState.Shown;
        }
    }

    public void ApproachCrashFadeByDistance(Vector3 stopPoint, float remainingArcLenMeters)
    {
        stackPoint = stopPoint;

        float win = (crashFadeWindow > 0f) ? crashFadeWindow : spacing * 0.8f;
        win = Mathf.Clamp(win, 0.01f, spacing);

        for (int i = 0; i < dotCount; i++)
        {
            float dBehindHead = i * spacing;
            float edgeStart   = Mathf.Max(0f, remainingArcLenMeters - win);
            float edgeEnd     = remainingArcLenMeters;

            if (dBehindHead < edgeStart - 1e-4f)
            {
                float goal = (i == 0 && forceHeadAlwaysBright) ? headAlpha : targetAlpha[i];
                SmoothAlphaTo(i, goal);
            }
            else if (dBehindHead > edgeEnd + 1e-4f)
            {
                SmoothAlphaTo(i, 0f);
            }
            else
            {
                float t = Mathf.InverseLerp(edgeStart, edgeEnd, dBehindHead);
                float fromA = (i == 0 && forceHeadAlwaysBright) ? headAlpha : targetAlpha[i];
                SmoothAlphaTo(i, Mathf.Lerp(fromA, hiddenAlpha, crashFadeEase.Evaluate(t)));
            }
        }
    }

    void RecordHeadSamples()
    {
        Vector3 head = target.position;
        float step = Mathf.Max(0.001f, spacing * minSampleStepFactor);

        float dist = Vector3.Distance(head, lastSample);
        int extra = Mathf.Min(maxExtraSamplesPerFrame, Mathf.CeilToInt(dist / step) - 1);
        for (int i = 1; i <= extra; i++)
        {
            float t = i / (float)(extra + 1);
            AddSample(Vector3.Lerp(lastSample, head, t));
        }

        Vector3 curDir = (head - lastSample).sqrMagnitude > 1e-6f ? (head - lastSample).normalized : prevHeadDir;
        float ang = Vector3.Angle(prevHeadDir, curDir);
        if (ang >= angleSampleThresholdDeg) AddSample(head);
        else if (dist >= step * (bounceTimer > 0f ? 0.5f : 1f)) AddSample(head);
        prevHeadDir = curDir;

        float keepLen = (dotCount + 2) * spacing * 2f;
        CullHistoryToLength(keepLen);
    }

    void AddSample(Vector3 p) { history.Add(p); lastSample = p; }

    void CullHistoryToLength(float maxLen)
    {
        if (history.Count < 2) return;
        float len = 0f; Vector3 prev = history[^1];
        int cut = -1;
        for (int i = history.Count - 2; i >= 0; i--)
        {
            Vector3 q = history[i];
            len += Vector3.Distance(prev, q);
            if (len > maxLen) { cut = i; break; }
            prev = q;
        }
        if (cut > 0) history.RemoveRange(0, cut);
    }

    void LayoutLiveStable()
    {
        Vector3 head = target.position;
        for (int i = 0; i < dotCount; i++)
        {
            float want = i * spacing;
            if (TrySmoothPositionAtDistanceFromHead(want, head, out Vector3 pos))
            {
                dots[i].tr.position = pos;
                float goal = (i == 0 && forceHeadAlwaysBright) ? headAlpha : targetAlpha[i];
                if (i == 0 && forceHeadAlwaysBright) DrawHeadExact(0, 0f);
                else SmoothAlphaTo(i, goal);
            }
            else
            {
                dots[i].tr.position = head;
                SmoothAlphaTo(i, 0f);
            }
        }
    }

    void LayoutPausedHidden()
    {
        // During pause, trail should be completely invisible.
        for (int i = 0; i < dotCount; i++)
        {
            dots[i].tr.position = stackPoint;
            SetAlphaImmediate(i, 0f);
        }
    }

    void LayoutRevealing()
    {
        dots[0].tr.position = target.position;
        DrawHeadExact(0, headAlpha);
        rState[0] = RevealState.Shown;

        float L = CurrentTrailLength();
        for (int i = 1; i < dotCount; i++)
        {
            float need = i * spacing;

            if (rState[i] == RevealState.Hidden && L + 1e-4f >= need)
                rState[i] = RevealState.Revealing;

            if (rState[i] == RevealState.Hidden)
            {
                dots[i].tr.position = stackPoint;
                SmoothAlphaTo(i, hiddenAlpha);
                continue;
            }

            if (TrySmoothPositionAtDistanceFromHead(need, target.position, out Vector3 pos))
            {
                dots[i].tr.position = pos;
                if (rState[i] == RevealState.Revealing)
                {
                    rT[i] += Time.deltaTime / Mathf.Max(0.0001f, revealDuration);
                    float k = revealEase.Evaluate(Mathf.Clamp01(rT[i]));
                    SmoothAlphaTo(i, Mathf.Lerp(hiddenAlpha, targetAlpha[i], k));
                    if (rT[i] >= 1f) rState[i] = RevealState.Shown;
                }
                else
                {
                    SmoothAlphaTo(i, targetAlpha[i]);
                }
            }
            else
            {
                dots[i].tr.position = stackPoint;
                SmoothAlphaTo(i, hiddenAlpha);
            }
        }

        bool allShown = true;
        for (int i = 0; i < dotCount; i++) if (rState[i] != RevealState.Shown) { allShown = false; break; }
        if (allShown) state = TrailState.Live;
    }

    float CurrentTrailLength()
    {
        if (history.Count < 2) return 0f;
        float len = 0f; Vector3 prev = history[^1];
        for (int i = history.Count - 2; i >= 0; i--)
        {
            Vector3 next = history[i];
            len += Vector3.Distance(prev, next);
            prev = next;
        }
        return len;
    }

    bool TrySmoothPositionAtDistanceFromHead(float distance, Vector3 head, out Vector3 result)
    {
        float accum = 0f; Vector3 prev = head;
        for (int i = history.Count - 1; i >= 0; i--)
        {
            Vector3 next = history[i];
            float seg = Vector3.Distance(prev, next);
            if (accum + seg >= distance)
            {
                float t = (distance - accum) / Mathf.Max(1e-6f, seg);
                Vector3 p1 = prev, p2 = next;
                Vector3 p0 = (i + 1 <= history.Count - 1) ? history[i + 1] : p1 + (p1 - p2);
                Vector3 p3 = (i - 1 >= 0)               ? history[i - 1] : p2 + (p2 - p1);
                result = CatmullRom(p0, p1, p2, p3, t);
                return true;
            }
            accum += seg; prev = next;
        }
        result = head; return false;
    }

    static Vector3 CatmullRom(in Vector3 p0, in Vector3 p1, in Vector3 p2, in Vector3 p3, float t)
    {
        float t2 = t * t, t3 = t2 * t;
        return 0.5f * ((2f * p1)
            + (-p0 + p2) * t
            + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
            + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    void InitializeDots()
    {
        foreach (Transform child in transform) Destroy(child.gameObject);
        dots.Clear();

        targetAlpha = new float[dotCount];
        liveAlpha   = new float[dotCount];
        rState = new RevealState[dotCount];
        rT = new float[dotCount];

        Vector3 origin = target ? target.position : transform.position;

        for (int i = 0; i < dotCount; i++)
        {
            var go = Instantiate(dotPrefab, origin, Quaternion.identity, transform);
            go.name = $"Dot_{i}";

            var rd = go.GetComponent<Renderer>();
            var mpb = new MaterialPropertyBlock();
            int colorId = Shader.PropertyToID(
                rd != null && rd.sharedMaterial != null && rd.sharedMaterial.HasProperty("_BaseColor")
                ? "_BaseColor" : "_Color"
            );

            var d = new Dot { tr = go.transform, rd = rd, mpb = mpb, colorId = colorId };
            dots.Add(d);
        }

        for (int i = 0; i < dotCount; i++)
        {
            float t = (dotCount <= 1) ? 0f : (float)i / (dotCount - 1);

            Color gradRGB = Color.Lerp(headColor, tailColor, t);
            dots[i].rgbCache = new Color(gradRGB.r, gradRGB.g, gradRGB.b, 1f);

            float a = Mathf.Lerp(headAlpha, tailAlpha, t) * opacityMultiplier;
            targetAlpha[i] = (i == 0 && forceHeadAlwaysBright) ? headAlpha * opacityMultiplier : a;

            float s = Mathf.Lerp(dotScaleHead, dotScaleTail, t);
            dots[i].tr.localScale = Vector3.one * s;

            liveAlpha[i] = 0f;
            SetAlphaImmediate(i, 0f);
        }

        minSampleStep = Mathf.Max(0.001f, spacing * minSampleStepFactor);

        history.Clear();
        lastSample = origin;
        prevHeadDir = Vector3.right;
        for (int i = 0; i < 8; i++) history.Add(origin);
    }

    void SmoothAlphaTo(int index, float goal)
    {
        float k = 1f - Mathf.Exp(-alphaFeather * (bounceTimer > 0f ? 1.6f : 1f) * Time.deltaTime);
        float next = Mathf.Lerp(liveAlpha[index], Mathf.Clamp01(goal), k);
        if (Mathf.Abs(next - liveAlpha[index]) < ALPHA_EPS) return;
        WriteColor(index, next);
    }
    void SetAlphaImmediate(int index, float a)
    {
        if (Mathf.Abs(a - liveAlpha[index]) < ALPHA_EPS) return;
        WriteColor(index, a);
    }
    void WriteColor(int index, float a)
    {
        var d = dots[index];
        Color c = d.rgbCache; c.a = Mathf.Clamp01(a);
        if (d.rd != null) { d.mpb.SetColor(d.colorId, c); d.rd.SetPropertyBlock(d.mpb); }
        liveAlpha[index] = c.a;
    }
    void DrawHeadExact(int index, float a)
    {
        var d = dots[index];
        Color c = headColor; c.a = Mathf.Clamp01(a);
        if (d.rd != null) { d.mpb.SetColor(d.colorId, c); d.rd.SetPropertyBlock(d.mpb); }
        liveAlpha[index] = c.a;
    }

    void DriveReveal()
    {
        for (int i = 0; i < dotCount; i++)
        {
            if (rState[i] == RevealState.Revealing)
            {
                rT[i] += Time.deltaTime / Mathf.Max(0.0001f, revealDuration);
                if (rT[i] >= 1f) { rT[i] = 1f; rState[i] = RevealState.Shown; }
            }
        }
    }
}
