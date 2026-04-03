using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatch : MonoBehaviour
{
    static readonly Queue<Action> _queue = new Queue<Action>();
    static UnityMainThreadDispatch _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        if (_instance != null) return;

        var go = new GameObject("UnityMainThreadDispatch");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<UnityMainThreadDispatch>();
    }

    public static void Enqueue(Action action)
    {
        if (action == null) return;

        lock (_queue)
        {
            _queue.Enqueue(action);
        }
    }

    void Update()
    {
        lock (_queue)
        {
            while (_queue.Count > 0)
            {
                try
                {
                    _queue.Dequeue()?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
