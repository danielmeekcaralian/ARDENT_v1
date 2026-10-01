using UnityEngine;

// Added only to objects placed in the sandbox; prefab assets remain unchanged.
public sealed class SandboxAssemblyPart : MonoBehaviour
{
    public GameObject SourcePrefab { get; private set; }
    public SandboxAssemblyPart Host { get; private set; }
    public AssemblyTarget Target { get; private set; }
    public bool IsAttached => Host != null && Target != null;
    public bool SkipNextSnap { get; private set; }
    private Transform freeParent;
    private Vector3 baselineScale;
    private ARObjectManipulator manipulator;

    public void Initialize(GameObject prefab)
    {
        SourcePrefab = prefab;
        freeParent = transform.parent;
        baselineScale = transform.localScale;
        manipulator = GetComponent<ARObjectManipulator>();
    }

    public float SizeMultiplier => manipulator != null ? manipulator.ScaleMultiplier : 1f;

    public void Attach(SandboxAssemblyPart host, AssemblyTarget target)
    {
        Host = host;
        Target = target;
        transform.SetParent(host.transform, true);
        transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
        // Preserve authored component-to-board proportions, including nonuniform prefab scales.
        transform.localScale = new Vector3(
            baselineScale.x / host.baselineScale.x,
            baselineScale.y / host.baselineScale.y,
            baselineScale.z / host.baselineScale.z);
        if (manipulator != null) manipulator.SetLocked(true);
    }

    public bool Detach()
    {
        if (!IsAttached) return false;
        float scale = Host.SizeMultiplier;
        transform.SetParent(freeParent, true);
        Host = null;
        Target = null;
        if (manipulator != null)
        {
            manipulator.SetLocked(false);
            manipulator.SetScaleMultiplier(scale);
        }
        else transform.localScale = baselineScale * scale;
        // The release of the detaching drag must not immediately reattach the component.
        SkipNextSnap = true;
        return true;
    }

    public bool ConsumeDetachRelease()
    {
        bool skip = SkipNextSnap;
        SkipNextSnap = false;
        return skip;
    }
}
