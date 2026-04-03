using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SignalConvergenceCoordinator : MonoBehaviour
{
    public float cooldown = 1.5f;

    public float moveTime = 2f;
    public float holdTime = 0.4f;
    public float returnTime = 2f;

    public GameObject dotTrailPrefab;

    private float lastTriggerTime = -999f;
    private bool isRunning = false;

    private class Signal
    {
        public spawn spawner;
        public int stemID;
    }

    private List<Signal> signals = new();

    public void RegisterSignal(spawn spawner)
    {
        if (spawner == null) return;

        float now = Time.time;

        signals.RemoveAll(s => s.stemID == spawner.stemID);

        foreach (var s in signals)
        {
            if (s.stemID == spawner.stemID) continue;

            if (!isRunning && now - lastTriggerTime > cooldown)
            {
                Debug.Log($"[MATCH] {s.stemID} + {spawner.stemID}");

                lastTriggerTime = now;
                isRunning = true;

                StartCoroutine(Converge(s.spawner, spawner));
                signals.Clear();
                return;
            }
        }

        signals.Add(new Signal
        {
            spawner = spawner,
            stemID = spawner.stemID
        });
    }

    IEnumerator Converge(spawn a, spawn b)
    {
        if (!a || !b)
        {
            isRunning = false;
            yield break;
        }

        if (!a.GetComponent<BeingMoved>()) a.gameObject.AddComponent<BeingMoved>();
        if (!b.GetComponent<BeingMoved>()) b.gameObject.AddComponent<BeingMoved>();

        Transform ta = a.transform;
        Transform tb = b.transform;

        Vector3 startA = ta.position;
        Vector3 startB = tb.position;

        // midpoint between the two activated spheres
        Vector3 dynamicCenter = (startA + startB) * 0.5f;

        CreateTrail(ta);
        CreateTrail(tb);

        float speedA = Vector3.Distance(startA, dynamicCenter) / Mathf.Max(0.01f, moveTime);
        float speedB = Vector3.Distance(startB, dynamicCenter) / Mathf.Max(0.01f, moveTime);

        float rampTime = 0.3f;
        float elapsed = 0f;

        // MOVE IN
        while (Vector3.Distance(ta.position, dynamicCenter) > 0.01f ||
               Vector3.Distance(tb.position, dynamicCenter) > 0.01f)
        {
            elapsed += Time.deltaTime;
            float ramp = Mathf.Clamp01(elapsed / rampTime);

            Vector3 dirA = dynamicCenter - ta.position;
            Vector3 dirB = dynamicCenter - tb.position;

            if (dirA.sqrMagnitude > 0.0001f)
                ta.rotation = Quaternion.LookRotation(dirA.normalized);

            if (dirB.sqrMagnitude > 0.0001f)
                tb.rotation = Quaternion.LookRotation(dirB.normalized);

            ta.position = Vector3.MoveTowards(
                ta.position,
                dynamicCenter,
                speedA * ramp * Time.deltaTime
            );

            tb.position = Vector3.MoveTowards(
                tb.position,
                dynamicCenter,
                speedB * ramp * Time.deltaTime
            );

            yield return null;
        }

        yield return new WaitForSeconds(holdTime);

        // RETURN
        elapsed = 0f;

        while (Vector3.Distance(ta.position, startA) > 0.01f ||
               Vector3.Distance(tb.position, startB) > 0.01f)
        {
            elapsed += Time.deltaTime;
            float ramp = Mathf.Clamp01(elapsed / rampTime);

            ta.position = Vector3.MoveTowards(
                ta.position,
                startA,
                speedA * ramp * Time.deltaTime
            );

            tb.position = Vector3.MoveTowards(
                tb.position,
                startB,
                speedB * ramp * Time.deltaTime
            );

            yield return null;
        }

        Destroy(a.GetComponent<BeingMoved>());
        Destroy(b.GetComponent<BeingMoved>());

        isRunning = false;
    }

    void CreateTrail(Transform target)
    {
        if (!dotTrailPrefab) return;

        GameObject trail = Instantiate(dotTrailPrefab, target.position, Quaternion.identity, target);

        var dt = trail.GetComponent<dotTrail>();
        if (dt != null)
        {
            dt.SetTarget(target);
            dt.ResetTrailImmediate();
        }

        Destroy(trail, moveTime + holdTime + returnTime + 0.5f);
    }
}