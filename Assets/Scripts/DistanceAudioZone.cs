using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DistanceAudioZone : MonoBehaviour
{
    public GameObject wand1;
    public GameObject wand2;
    public AudioClip audioClip;
    public float activationRadius = 1.0f;

    public Color inactiveColor = Color.blue;
    public Color activeColor = Color.green;
    public Color overlapColor = Color.red;

    public float playDuration = 20f;
    public float normalVolume = 1.0f;
    public float overlapVolume = 2.0f;

    private AudioSource audioSource;
    private Renderer rend;
    private bool wasActive = false;
    private bool isPlayingTimed = false;
    private bool overlapMode = false;

    public spawn rippleSpawner;
    public SignalConvergenceCoordinator convergenceCoordinator;
    public float rippleStrength = 1f;

    public bool IsPlayingTimed
    {
        get { return isPlayingTimed; }
    }

    void Start()
    {
        Debug.Log(gameObject.name + ": Start running");

        audioSource = GetComponent<AudioSource>();
        rend = GetComponent<Renderer>();

        if (audioSource == null)
        {
            Debug.LogError(gameObject.name + ": No AudioSource found");
            return;
        }

        if (audioClip == null)
        {
            Debug.LogError(gameObject.name + ": No audioClip assigned in Inspector");
        }
        else
        {
            Debug.Log(gameObject.name + ": Audio clip assigned = " + audioClip.name);
        }

        audioSource.clip = audioClip;
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = normalVolume;
        audioSource.spatialBlend = 0.0f;

        if (rend != null)
        {
            rend.material.color = inactiveColor;
        }
        if (rippleSpawner == null)
        {
            rippleSpawner = GetComponent<spawn>();
        }

        if (convergenceCoordinator == null)
        {
            convergenceCoordinator = FindFirstObjectByType<SignalConvergenceCoordinator>();
        }
    }

    void Update()
    {
        if (wand1 == null && wand2 == null)
        {
            Debug.LogError(gameObject.name + ": Neither wand is assigned");
            return;
        }

        bool isActive = false;

        if (wand1 != null)
        {
            float dist1 = Vector3.Distance(transform.position, wand1.transform.position);
            if (dist1 <= activationRadius)
            {
                isActive = true;
            }
        }

        if (wand2 != null)
        {
            float dist2 = Vector3.Distance(transform.position, wand2.transform.position);
            if (dist2 <= activationRadius)
            {
                isActive = true;
            }
        }

        if (isActive && !wasActive && !isPlayingTimed)
        {
        Debug.Log(gameObject.name + " ENTERED zone -> start 20s audio");

        if (rend != null)
        {
            rend.material.color = activeColor;
        }

        if (rippleSpawner != null)
        {
            rippleSpawner.SpawnRipple(rippleStrength);
        }

        if (convergenceCoordinator != null && rippleSpawner != null)
        {
            convergenceCoordinator.RegisterSignal(rippleSpawner);
        }

        StartCoroutine(PlayForDuration());
        }

        if (!isActive && wasActive)
        {
            if (rend != null && !isPlayingTimed && !overlapMode)
            {
                rend.material.color = inactiveColor;
            }
        }

        wasActive = isActive;
    }

    IEnumerator PlayForDuration()
    {
        isPlayingTimed = true;

        if (audioSource.clip != null)
        {
            audioSource.Play();
        }

        yield return new WaitForSeconds(playDuration);

        audioSource.Stop();

        Debug.Log(gameObject.name + " audio stopped after " + playDuration + " seconds");

        isPlayingTimed = false;

        if (rend != null)
        {
            rend.material.color = inactiveColor;
        }

        audioSource.volume = normalVolume;
        overlapMode = false;
    }

    public void SetOverlapMode(bool isOverlapping)
    {
        overlapMode = isOverlapping;

        if (audioSource != null)
        {
            audioSource.volume = isOverlapping ? overlapVolume : normalVolume;
        }

        if (rend != null)
        {
            if (isOverlapping && isPlayingTimed)
            {
                rend.material.color = overlapColor;
            }
            else if (isPlayingTimed)
            {
                rend.material.color = activeColor;
            }
            else
            {
                rend.material.color = inactiveColor;
            }
        }
    }
}