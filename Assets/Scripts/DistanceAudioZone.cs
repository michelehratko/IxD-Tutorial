using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DistanceAudioZone : MonoBehaviour
{
    public GameObject wand;
    public AudioClip audioClip;
    public float activationRadius = 1.0f;

    public Color inactiveColor = Color.blue;
    public Color activeColor = Color.green;

    public float playDuration = 20f;

    private AudioSource audioSource;
    private Renderer rend;
    private bool wasActive = false;
    private bool isPlayingTimed = false;

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
        audioSource.volume = 1.0f;
        audioSource.spatialBlend = 0.0f;

        if (rend != null)
        {
            rend.material.color = inactiveColor;
        }
    }

    void Update()
    {
        if (wand == null)
        {
            Debug.LogError(gameObject.name + ": Wand is not assigned");
            return;
        }

        float dist = Vector3.Distance(transform.position, wand.transform.position);
        bool isActive = dist <= activationRadius;

        if (isActive && !wasActive && !isPlayingTimed)
        {
            Debug.Log(gameObject.name + " ENTERED zone -> start 20s audio");

            if (rend != null)
            {
                rend.material.color = activeColor;
            }

            StartCoroutine(PlayForDuration());
        }

        if (!isActive && wasActive)
        {
            if (rend != null && !isPlayingTimed)
            {
                rend.material.color = inactiveColor;
            }
        }

        wasActive = isActive;
    }

    IEnumerator PlayForDuration()
    {
        isPlayingTimed = true;

        audioSource.Play();

        yield return new WaitForSeconds(playDuration);

        audioSource.Stop();

        Debug.Log(gameObject.name + " audio stopped after " + playDuration + " seconds");

        isPlayingTimed = false;

        if (rend != null)
        {
            rend.material.color = inactiveColor;
        }
    }
}