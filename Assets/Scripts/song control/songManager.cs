using UnityEngine;
using System.Collections.Generic;

public class songManager : MonoBehaviour
{
    [System.Serializable]
    public class OSCSource
    {
        public string address;
        public AudioClip clip;
        public Transform homePoint;

        [Header("DEBUG")]
        public bool debugActive;

        [Header("Audio Sensitivity")]
        public float threshold = 0.001f;

        [System.NonSerialized] public GameObject emitter;
        [System.NonSerialized] public spawn spawner;
        [System.NonSerialized] public rippleAnalyzer analyzer;
        [System.NonSerialized] public bool isActive;
    }

    [Header("Sources")]
    public List<OSCSource> sources = new();

    [Header("Prefabs")]
    public GameObject emitterPrefab;
    public GameObject ripplePrefab;

    [Header("Ripple Settings")]
    public float rippleMinRadius = 4f;
    public float rippleMaxRadius = 12f;
    public float rippleMinLifetime = 2.5f;
    public float rippleMaxLifetime = 6.5f;

    void Start()
    {
        InitializeSources();
    }

    void InitializeSources()
    {
        for (int i = 0; i < sources.Count; i++)
        {
            var s = sources[i];
            if (!s.homePoint) continue;

            Debug.Log($"INIT SOURCE {i}");

            GameObject emitter = Instantiate(emitterPrefab, s.homePoint.position, Quaternion.identity);
            s.emitter = emitter;

            var audio = emitter.GetComponent<AudioSource>() ?? emitter.AddComponent<AudioSource>();
            audio.clip = s.clip;
            audio.loop = true;
            audio.playOnAwake = true;
            audio.Play();

            var spawner = emitter.GetComponent<spawn>() ?? emitter.AddComponent<spawn>();
            spawner.ripplePrefab = ripplePrefab.GetComponentInChildren<ripples>();
            spawner.stemID = i;

            spawner.radiusMin = rippleMinRadius;
            spawner.radiusMax = rippleMaxRadius;
            spawner.lifetimeMin = rippleMinLifetime;
            spawner.lifetimeMax = rippleMaxLifetime;

            s.spawner = spawner;

            var analyzer = emitter.GetComponent<rippleAnalyzer>() ?? emitter.AddComponent<rippleAnalyzer>();
            analyzer.rippleSpawner = spawner;
            analyzer.isActive = false;
            analyzer.threshold = s.threshold;

            s.analyzer = analyzer;
        }
    }

    void Update()
    {
        for (int i = 0; i < sources.Count; i++)
        {
            var s = sources[i];
            if (s.analyzer == null) continue;

            if (s.isActive != s.debugActive)
            {
                s.isActive = s.debugActive;
                s.analyzer.isActive = s.debugActive;

                Debug.Log($"[TOGGLE] Source {i} active: {s.isActive}");
            }

            if (s.emitter && s.homePoint)
            {
                if (!IsBeingMoved(s.emitter))
                {
                    s.emitter.transform.position = s.homePoint.position;
                }
            }
        }
    }
   
    bool IsBeingMoved(GameObject obj)
    {
        return obj.GetComponent<BeingMoved>() != null;
    }
}