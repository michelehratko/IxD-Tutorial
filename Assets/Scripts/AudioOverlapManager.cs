using UnityEngine;

public class AudioOverlapManager : MonoBehaviour
{
    public DistanceAudioZone[] zones;

    void Update()
    {
        int playingCount = 0;

        foreach (DistanceAudioZone zone in zones)
        {
            if (zone != null && zone.IsPlayingTimed)
            {
                playingCount++;
            }
        }

        bool overlapActive = playingCount >= 2;

        foreach (DistanceAudioZone zone in zones)
        {
            if (zone != null)
            {
                zone.SetOverlapMode(overlapActive);
            }
        }
    }
}