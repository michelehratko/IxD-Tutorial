using UnityEngine;
using System.Collections.Generic;

public class FirstScript : MonoBehaviour
{
    public int myInt = 5;
    public float myFloat = 3.14f;
    public string myString = "Hello, World!";
    public bool myBool = true;
    public char myChar = 'A';
    public List<int> myList = new List<int>();

    public GameObject myGameObject;
    public Transform myTransform;
    public Rigidbody myRigidbody;
    public Camera myCamera;
    public Light myLight;

    [Header("Spin + Erratic Movement")]
    public float loopDeLoopDegreesPerSecond = 180f;
    public Vector3 spinDegreesPerSecond = new Vector3(30f, 180f, 60f);

    public float wanderRadius = 1.5f;
    public float wanderSpeed = 1.0f;
    public float jitterAmplitude = 0.15f;
    public float jitterHz = 12f;

    private Vector3 _startPos;
    private float _noiseSeed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (myGameObject != null)
        {
            _startPos = myGameObject.transform.position;
        }
        else
        {
            _startPos = transform.position;
        }

        _noiseSeed = Random.Range(-1000f, 1000f);
    }

    // Update is called once per frame
    void Update()
    {
        if (myGameObject == null) return;

        float dt = Time.deltaTime;
        float t = Time.time;

        // Loop-de-loop (pitch) + extra spin to make it feel lively
        myGameObject.transform.Rotate(Vector3.right, loopDeLoopDegreesPerSecond * dt, Space.Self);
        myGameObject.transform.Rotate(spinDegreesPerSecond * dt, Space.Self);

        // Smooth-ish erratic movement using Perlin noise
        float nx = Mathf.PerlinNoise(_noiseSeed + t * wanderSpeed, 0.13f) * 2f - 1f;
        float ny = Mathf.PerlinNoise(0.42f, _noiseSeed + t * wanderSpeed) * 2f - 1f;
        float nz = Mathf.PerlinNoise(_noiseSeed + t * wanderSpeed, 0.91f) * 2f - 1f;
        Vector3 wanderOffset = new Vector3(nx, ny, nz) * wanderRadius;

        // Higher-frequency jitter layered on top
        float jx = Mathf.Sin((t + _noiseSeed) * Mathf.PI * 2f * jitterHz);
        float jy = Mathf.Sin((t + _noiseSeed * 0.37f) * Mathf.PI * 2f * (jitterHz * 1.17f));
        float jz = Mathf.Sin((t + _noiseSeed * 0.73f) * Mathf.PI * 2f * (jitterHz * 0.91f));
        Vector3 jitterOffset = new Vector3(jx, jy, jz) * jitterAmplitude;

        myGameObject.transform.position = _startPos + wanderOffset + jitterOffset;
    }
}
