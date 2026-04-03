using UnityEngine;
using System.Collections.Generic;

public class rippleLine : MonoBehaviour
{
    public List<Transform> rippleEmitters;
    public Material lineMaterial;
    public float maxVisibleDistance = 10f;

    [Header("Stability")]
    public float minDistance = 0.15f;

    private List<LineRenderer> lineRenderers = new();
    private Gradient sharedGradient;

    void Start()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // shared gradient
        sharedGradient = new Gradient();
        sharedGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.6f, 0.6f, 0.6f), 0f),
                new GradientColorKey(new Color(0.4f, 0.4f, 0.4f), 0.5f),
                new GradientColorKey(new Color(0.6f, 0.6f, 0.6f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0.3f, 0.35f),
                new GradientAlphaKey(0.3f, 0.65f),
                new GradientAlphaKey(0.8f, 1f)
            }
        );

        for (int i = 0; i < rippleEmitters.Count; i++)
        {
            for (int j = i + 1; j < rippleEmitters.Count; j++)
            {
                GameObject lineObj = new GameObject($"Line_{i}_{j}");
                lineObj.transform.parent = this.transform;

                LineRenderer lr = lineObj.AddComponent<LineRenderer>();

                lr.material = lineMaterial != null
                    ? lineMaterial
                    : new Material(Shader.Find("Sprites/Default"));

                lr.positionCount = 3;
                lr.startWidth = 0.02f;
                lr.endWidth = 0.02f;
                lr.useWorldSpace = true;

                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;

                lr.colorGradient = sharedGradient;
                lr.enabled = false;

                lineRenderers.Add(lr);
            }
        }
    }

    void Update()
    {
        int index = 0;

        for (int i = 0; i < rippleEmitters.Count; i++)
        {
            for (int j = i + 1; j < rippleEmitters.Count; j++)
            {
                if (index >= lineRenderers.Count) continue;

                Transform a = rippleEmitters[i];
                Transform b = rippleEmitters[j];
                LineRenderer lr = lineRenderers[index++];

                if (a == null || b == null)
                {
                    lr.enabled = false;
                    continue;
                }

                Vector3 start = a.position;
                Vector3 end = b.position;

                float dist = Vector3.Distance(start, end);

                if (dist < minDistance || dist > maxVisibleDistance)
                {
                    lr.enabled = false;
                    continue;
                }

                lr.enabled = true;

                Vector3 mid = (start + end) * 0.5f;

                lr.SetPosition(0, start);
                lr.SetPosition(1, mid);
                lr.SetPosition(2, end);
            }
        }
    }
}