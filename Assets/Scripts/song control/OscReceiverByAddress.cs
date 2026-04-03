using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class OscReceiverByAddress : MonoBehaviour
{
    public int listenPort = 9000;
    public SignalConvergenceCoordinator coordinator;
    public bool logIncomingMessages = true;
    public bool logUnknownAddresses = true;

    UdpClient udp;
    IPEndPoint anyEndpoint;
    bool running;

    readonly Dictionary<string, OscAddressBinding> bindings = new();

    void Awake()
    {
        if (!coordinator)
            coordinator = FindFirstObjectByType<SignalConvergenceCoordinator>();

        RebuildBindings();
    }

    void OnEnable()
    {
        StartReceiver();
    }

    void OnDisable()
    {
        StopReceiver();
    }

    public void RebuildBindings()
    {
        bindings.Clear();

        foreach (var binding in FindObjectsByType<OscAddressBinding>(FindObjectsSortMode.None))
        {
            if (binding == null) continue;
            if (string.IsNullOrWhiteSpace(binding.oscAddress)) continue;
            if (binding.rippleSpawner == null) continue;

            string addr = binding.oscAddress.Trim();
            if (!addr.StartsWith("/")) addr = "/" + addr;

            bindings[addr] = binding;
            Debug.Log($"[OscReceiverByAddress] Bound {addr} -> stem {binding.rippleSpawner.stemID}");
        }
    }

    public void StartReceiver()
    {
        if (running) return;

        try
        {
            anyEndpoint = new IPEndPoint(IPAddress.Any, listenPort);
            udp = new UdpClient(listenPort);
            running = true;
            udp.BeginReceive(OnUdpData, null);

            Debug.Log($"[OscReceiverByAddress] Listening on UDP {listenPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[OscReceiverByAddress] Failed to start: {e}");
        }
    }

    public void StopReceiver()
    {
        running = false;

        try { udp?.Close(); }
        catch { }

        udp = null;
        anyEndpoint = null;
    }

    void OnUdpData(IAsyncResult ar)
    {
        if (!running || udp == null) return;

        try
        {
            byte[] data = udp.EndReceive(ar, ref anyEndpoint);

            if (TryParseOscMessage(data, out string address, out object value))
            {
                if (logIncomingMessages)
                    Debug.Log($"[OSC IN] {address} = {value}");

                UnityMainThreadDispatch.Enqueue(() => HandleOscMessage(address, value));
            }
        }
        catch (ObjectDisposedException)
        {
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[OscReceiverByAddress] Receive error: {e}");
        }
        finally
        {
            if (running && udp != null)
            {
                try { udp.BeginReceive(OnUdpData, null); }
                catch { }
            }
        }
    }

    Dictionary<string, float> lastValues = new();

    void HandleOscMessage(string address, object value)
    {
        if (string.IsNullOrWhiteSpace(address)) return;
        if (!address.StartsWith("/")) address = "/" + address;

        float normalized = Mathf.Clamp01(ToFloat(value, 0f));

        Debug.Log($"[OSC] {address} raw={value} normalized={normalized}");

        float prev = lastValues.ContainsKey(address) ? lastValues[address] : 0f;
        lastValues[address] = normalized;

        bool isEdge = normalized > 0f && prev <= 0f;

        if (isEdge)
        {
            Debug.Log($"Edge {address} triggered");
        }
        if (!isEdge) return;

        if (bindings.TryGetValue(address, out var binding) && binding != null && binding.rippleSpawner != null)
        {
            binding.rippleSpawner.SetVolumeAndFrequency(normalized, normalized);

            if (coordinator != null)
                coordinator.RegisterSignal(binding.rippleSpawner);
            else
                binding.rippleSpawner.SpawnRipple(normalized);
        }
    }

    static float ToFloat(object value, float fallback)
    {
        if (value is float f) return f;
        if (value is int i) return i;
        if (value is double d) return (float)d;
        if (value is string s && float.TryParse(s, out var parsed)) return parsed;
        return fallback;
    }

    static bool TryParseOscMessage(byte[] data, out string address, out object firstArg)
    {
        address = null;
        firstArg = null;

        if (data == null || data.Length < 8)
            return false;

        int index = 0;
        address = ReadOscString(data, ref index);
        if (string.IsNullOrEmpty(address))
            return false;

        string typeTags = ReadOscString(data, ref index);
        if (string.IsNullOrEmpty(typeTags) || typeTags[0] != ',')
            return false;

        if (typeTags.Length < 2)
        {
            firstArg = 1f;
            return true;
        }

        switch (typeTags[1])
        {
            case 'f': firstArg = ReadFloat32(data, ref index); return true;
            case 'i': firstArg = ReadInt32(data, ref index); return true;
            case 's': firstArg = ReadOscString(data, ref index); return true;
            default: firstArg = 1f; return true;
        }
    }

    static string ReadOscString(byte[] data, ref int index)
    {
        int start = index;
        while (index < data.Length && data[index] != 0) index++;

        string s = Encoding.ASCII.GetString(data, start, index - start);

        index++;
        while (index % 4 != 0) index++;

        return s;
    }

    static int ReadInt32(byte[] data, ref int index)
    {
        int value =
            (data[index] << 24) |
            (data[index + 1] << 16) |
            (data[index + 2] << 8) |
            data[index + 3];

        index += 4;
        return value;
    }

    static float ReadFloat32(byte[] data, ref int index)
    {
        byte[] buf = new byte[4];
        Buffer.BlockCopy(data, index, buf, 0, 4);
        if (BitConverter.IsLittleEndian) Array.Reverse(buf);

        index += 4;
        return BitConverter.ToSingle(buf, 0);
    }
}
