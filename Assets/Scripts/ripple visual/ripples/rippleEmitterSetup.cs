using UnityEngine;

public class rippleEmitterSetup : MonoBehaviour
{
    public spawn rippleSpawner;
    public emitterMove motion;

    public void ApplyMotionSettings(float radius, float speed, Vector3 offset, emitterMove.MotionType type)
    {
        if (motion != null)
        {
            motion.radius = radius;
            motion.speed = speed;
            motion.centerOffset = offset;
            motion.motionType = type;
        }
    }
}
