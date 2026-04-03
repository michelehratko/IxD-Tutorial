using UnityEngine;

public class datadriver : MonoBehaviour
{
    [Header("Mock Noise Offsets (unique per instance)")]
    public float volumeOffset = 0f;
    public float frequencyOffset = 5f;

    [Header("Target Ripple Spawner")]
    public spawn rippleSpawner;

    void Update()
    {
        float volume = Mathf.PerlinNoise(Time.time * 0.5f, volumeOffset);
        float frequency = Mathf.PerlinNoise(Time.time * 0.7f, frequencyOffset);

        rippleSpawner.SetVolumeAndFrequency(volume, frequency);
    }
}
