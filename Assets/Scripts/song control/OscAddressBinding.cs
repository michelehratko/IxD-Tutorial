using UnityEngine;

public class OscAddressBinding : MonoBehaviour
{
    public string oscAddress;
    public spawn rippleSpawner;

    void Awake()
    {
        if (!rippleSpawner)
            rippleSpawner = GetComponent<spawn>();
    }
}
