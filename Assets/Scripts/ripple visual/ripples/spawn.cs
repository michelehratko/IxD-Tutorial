using UnityEngine;

public class spawn : MonoBehaviour
{
    [Header("Prefab")]
    public ripples ripplePrefab;

    [Header("Size")]
    public float radiusMin = 4f;
    public float radiusMax = 12f;

    [Header("Thickness")]
    public float widthMin = 0.02f;
    public float widthMax = 0.08f;

    [Header("Lifetime")]
    public float lifetimeMin = 2.5f;
    public float lifetimeMax = 6.5f;

    [HideInInspector] public int stemID = -1;

    private float inputVolume    = 0f;
    private float inputFrequency = 0.5f;

    public void SetVolumeAndFrequency(float volume, float frequency)
    {
        inputVolume    = Mathf.Clamp01(volume);
        inputFrequency = Mathf.Clamp01(frequency);
    }

    public void SpawnRipple(float volume)
    {
        SpawnRippleHere(volume, transform.position);
    }

    public void SpawnRippleHere(float volume, Vector3 worldPos)
    {
        if (!ripplePrefab) return;

        volume = Mathf.Clamp01(volume);
        float frequency = Mathf.Clamp01(inputFrequency);

        float radius    = Mathf.Lerp(radiusMin, radiusMax, volume);
        float thickness = Mathf.Lerp(widthMin,  widthMax,  volume);
        float lifetime  = Mathf.Lerp(lifetimeMax, lifetimeMin, frequency);

        var ripple = Instantiate(ripplePrefab, worldPos, Quaternion.identity);
        ripple.maxRadius       = radius;
        ripple.lineWidth       = thickness;
        ripple.lifetime        = lifetime;
        ripple.stemID          = stemID;
        ripple.sourceVolume    = volume;
        ripple.sourceFrequency = frequency;
    }
}