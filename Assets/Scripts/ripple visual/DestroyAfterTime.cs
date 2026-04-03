using UnityEngine;

public class DestroyAfterTime : MonoBehaviour
{
    public float lifetime = 2f;
    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifetime)
            Destroy(gameObject);
    }
}