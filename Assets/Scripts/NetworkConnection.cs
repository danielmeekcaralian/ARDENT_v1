using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public sealed class NetworkConnection : MonoBehaviour
{
    public NetworkNode StartNode { get; private set; }
    public NetworkNode EndNode { get; private set; }
    private LineRenderer line;
    private float startFraction = .5f, endFraction = .5f;

    public void SetBackboneFraction(NetworkNode backbone, float fraction)
    {
        if (StartNode == backbone) startFraction = Mathf.Clamp01(fraction);
        if (EndNode == backbone) endFraction = Mathf.Clamp01(fraction);
    }

    public void Initialize(NetworkNode first, NetworkNode second)
    {
        StartNode = first;
        EndNode = second;
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        Refresh();
    }

    public bool Joins(NetworkNode a, NetworkNode b) =>
        (StartNode == a && EndNode == b) || (StartNode == b && EndNode == a);

    private void LateUpdate() => Refresh();

    private void Refresh()
    {
        if (StartNode == null || EndNode == null)
        {
            Destroy(gameObject);
            return;
        }
        if (line == null) return;
        line.SetPosition(0, StartNode.CablePosition(startFraction));
        line.SetPosition(1, EndNode.CablePosition(endFraction));
    }
}
