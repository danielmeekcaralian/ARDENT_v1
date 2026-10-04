using UnityEngine;

public sealed class RJ45DraggableWire : MonoBehaviour
{
    public RJ45WireColor identity;
    public Collider pickCollider;
    public RJ45WireTube bendingTube;
    [HideInInspector] public Vector3 homePosition;
}

