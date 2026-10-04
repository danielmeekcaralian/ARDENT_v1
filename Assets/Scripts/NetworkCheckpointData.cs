using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class NetworkCheckpointData
{
    public int version = 1;
    public int topology;
    public int passedMask;
    public int nextLabel = 1;
    public NetworkSavedNode[] nodes = new NetworkSavedNode[0];
    public NetworkSavedEdge[] edges = new NetworkSavedEdge[0];
}

[Serializable]
public sealed class NetworkSavedNode
{
    public string id;
    public string label;
    public int prefabIndex;
    public Vector3 position;
    public Quaternion rotation;
    public float multiplier;
}

[Serializable]
public sealed class NetworkSavedEdge
{
    public string a, b;
}

public static class NetworkCheckpointRules
{
    public static bool IsValid(NetworkCheckpointData data, int prefabCount)
    {
        if (data == null || data.version != 1 || data.topology < 0 || data.topology > 2 ||
            data.passedMask < 0 || data.passedMask > 7 || data.nextLabel < 1 || data.nextLabel > 1000000 ||
            data.nodes == null || data.nodes.Length > 100 || data.edges == null || data.edges.Length > 4950 || prefabCount < 1)
            return false;
        var ids = new HashSet<string>();
        foreach (var n in data.nodes)
        {
            if (n == null || string.IsNullOrEmpty(n.id) || n.id.Length > 64 || !ids.Add(n.id) ||
                n.label == null || n.label.Length > 256 || n.prefabIndex < 0 || n.prefabIndex >= prefabCount ||
                !Finite(n.position.x) || !Finite(n.position.y) || !Finite(n.position.z) ||
                Math.Abs(n.position.x) > 100 || Math.Abs(n.position.y) > 100 || Math.Abs(n.position.z) > 100 ||
                !Finite(n.multiplier) || n.multiplier < 1 || n.multiplier > 100 ||
                !Finite(n.rotation.x) || !Finite(n.rotation.y) || !Finite(n.rotation.z) || !Finite(n.rotation.w)) return false;
            float norm = n.rotation.x*n.rotation.x + n.rotation.y*n.rotation.y + n.rotation.z*n.rotation.z + n.rotation.w*n.rotation.w;
            if (Math.Abs(norm - 1f) > .01f) return false;
        }
        var pairs = new HashSet<(string, string)>();
        foreach (var e in data.edges)
        {
            if (e == null || e.a == null || e.b == null || e.a == e.b || !ids.Contains(e.a) || !ids.Contains(e.b)) return false;
            var pair = string.CompareOrdinal(e.a, e.b) < 0 ? (e.a, e.b) : (e.b, e.a);
            if (!pairs.Add(pair)) return false;
        }
        return true;
    }
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
