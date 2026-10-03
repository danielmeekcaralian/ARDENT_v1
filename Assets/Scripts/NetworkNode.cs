using UnityEngine;

public enum NetworkNodeType
{
    PC = 0,
    Switch = 1,
    Backbone = 2
}

// Device identity and cable endpoint only. Connections and topology rules
// will be owned by the network activity, not by individual prefabs.
[DisallowMultipleComponent]
public sealed class NetworkNode : MonoBehaviour
{
    [Header("Network Device")]
    [SerializeField] private string deviceName;
    [SerializeField] private NetworkNodeType nodeType;

    [Header("Cable Attachment")]
    [SerializeField] private Transform connectionPoint;

    public string DeviceName => string.IsNullOrWhiteSpace(deviceName)
        ? gameObject.name : deviceName.Trim();
    public NetworkNodeType NodeType => nodeType;
    public Transform ConnectionPoint => connectionPoint;
    public Vector3 ConnectionPosition => connectionPoint != null
        ? connectionPoint.position : transform.position;

    private MeshFilter backboneMesh;
    public Vector3 CablePosition(float fraction)
    {
        if (nodeType != NetworkNodeType.Backbone) return ConnectionPosition;
        if (backboneMesh == null) backboneMesh = GetComponentInChildren<MeshFilter>();
        if (backboneMesh == null || backboneMesh.sharedMesh == null)
            return transform.TransformPoint(new Vector3(Mathf.Lerp(-.6f, .6f, fraction), .01f, 0));
        // The authored backbone bar runs along its Visual child's local X axis.
        Bounds bounds = backboneMesh.sharedMesh.bounds;
        return backboneMesh.transform.TransformPoint(new Vector3(
            Mathf.Lerp(bounds.min.x, bounds.max.x, fraction), bounds.max.y, bounds.center.z));
    }

#if UNITY_EDITOR
    private void Reset()
    {
        connectionPoint = transform.Find("ConnectionPoint");
    }
#endif
}
