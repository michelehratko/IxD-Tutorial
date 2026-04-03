using UnityEngine;

public class fadeOut : MonoBehaviour
{
    public float lifetime = 2f;

    private float startTime;
    private Renderer rend;
    private Color originalColor;
    private Color targetColor = new Color(0.3f, 0.3f, 0.3f, 0f);

    void Start()
    {
        startTime = Time.time;
        rend = GetComponent<Renderer>();
        originalColor = rend.material.color;
        
        rend.material.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
    }

    void Update()
    {
        float age = Time.time - startTime;
        float t = Mathf.Clamp01(age / lifetime);

        float fadeInPortion = 0.2f;

        if (t < fadeInPortion)
        {

            float fadeT = t / fadeInPortion;
            rend.material.color = Color.Lerp(new Color(originalColor.r, originalColor.g, originalColor.b, 0f), originalColor, fadeT);
        }
        else
        {

            float fadeT = Mathf.SmoothStep(0f, 1f, (t - fadeInPortion) / (1f - fadeInPortion));
            rend.material.color = Color.Lerp(originalColor, targetColor, fadeT);
        }

        if (t >= 1f)
            Destroy(gameObject);
    }
}
