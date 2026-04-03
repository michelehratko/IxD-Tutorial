using UnityEngine;

public class emitterMove : MonoBehaviour
{
    public enum MotionType { Circle, FigureEight, Spiral }

    public MotionType motionType = MotionType.Circle;
    public float radius = 5f;
    public float speed = 1f;
    public Vector3 centerOffset = Vector3.zero;

    void Update()
    {
        float t = Time.time * speed;
        Vector3 pos = Vector3.zero;

        switch (motionType)
        {
            case MotionType.Circle:
                pos = new Vector3(Mathf.Cos(t), 0f, Mathf.Sin(t)) * radius;
                break;
            case MotionType.FigureEight:
                pos = new Vector3(Mathf.Sin(t), 0f, Mathf.Sin(t) * Mathf.Cos(t)) * radius;
                break;
            case MotionType.Spiral:
                float spiralR = radius * t * 0.1f;
                pos = new Vector3(Mathf.Cos(t), 0f, Mathf.Sin(t)) * spiralR;
                break;
        }

        transform.position = centerOffset + pos;
    }
}
