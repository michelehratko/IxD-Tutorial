using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class rippleAnalyzer : MonoBehaviour
{
    public spawn rippleSpawner;

    public float threshold = 0.001f;
    public int minBand = 0;
    public int maxBand = 60;

    public float minTimeBetweenRipples = 0.2f;

    [Header("Signal")]
    public float edgeCooldown = 0.3f;
    public bool isActive = false;

    private AudioSource src;
    private float[] spectrum = new float[256];

    private float lastRippleTime = -999f;
    private float lastEdgeTime = -999f;

    private bool lastActiveState = false;

    void Start()
    {
        src = GetComponent<AudioSource>();
        if (!src.isPlaying) src.Play();

        // prevent false edge on startup
        lastActiveState = isActive;
        lastEdgeTime = Time.time;
    }

    void Update()
    {
        // EDGE TRIGGER (only on 0 → 1)
        if (isActive && !lastActiveState && Time.time - lastEdgeTime > edgeCooldown)
        {
            Debug.Log($"[EDGE] Trigger from {rippleSpawner.stemID}");

            var coord = FindFirstObjectByType<SignalConvergenceCoordinator>();
            if (coord != null && rippleSpawner != null)
            {
                coord.RegisterSignal(rippleSpawner);
                lastEdgeTime = Time.time;
            }
        }

        lastActiveState = isActive;

        // 🔥 CRITICAL FIX: no ripples unless active
        if (!src.isPlaying || !isActive) return;

        src.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        float avg = 0f;
        for (int i = minBand; i <= maxBand && i < spectrum.Length; i++)
            avg += spectrum[i];

        avg /= (maxBand - minBand + 1);

        if (avg > threshold && Time.time - lastRippleTime >= minTimeBetweenRipples)
        {
            rippleSpawner.SetVolumeAndFrequency(avg, avg);
            rippleSpawner.SpawnRipple(avg);
            lastRippleTime = Time.time;
        }
    }
}