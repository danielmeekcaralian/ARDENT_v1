using UnityEngine;

[DisallowMultipleComponent]
public sealed class NetworkCableConnector : MonoBehaviour
{
    public NetworkCableKind kind;
    public Transform fixedEnd;
    public LineRenderer cableLine;

    private Vector3 homePosition;
    private Quaternion homeRotation;
    private bool homeCaptured;

    public bool IsLocked { get; private set; }

    private void Awake()
    {
        CaptureHome();
        RefreshCable();
    }

    private void LateUpdate()
    {
        RefreshCable();
    }

    public void CaptureHome()
    {
        if (homeCaptured) return;
        homePosition = transform.localPosition;
        homeRotation = transform.localRotation;
        homeCaptured = true;
    }

    public void MoveTo(Vector3 worldPosition)
    {
        if (IsLocked) return;
        transform.position = worldPosition;
        RefreshCable();
    }

    public void ResetToHome()
    {
        CaptureHome();
        IsLocked = false;
        transform.localPosition = homePosition;
        transform.localRotation = homeRotation;
        RefreshCable();
    }

    public void SnapTo(Transform snapPoint)
    {
        if (snapPoint == null) return;
        transform.SetPositionAndRotation(snapPoint.position, snapPoint.rotation);
        IsLocked = true;
        RefreshCable();
    }

    private void RefreshCable()
    {
        if (cableLine == null || fixedEnd == null) return;
        cableLine.useWorldSpace = true;
        cableLine.positionCount = 2;
        cableLine.SetPosition(0, fixedEnd.position);
        cableLine.SetPosition(1, transform.position);
    }
}
