using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class NetworkCableWorkstation : MonoBehaviour
{
    public NetworkCableConnector[] connectors;
    public NetworkCableTarget[] targets;
    [Min(0.02f)] public float snapDistance = 0.075f;
    [HideInInspector] public Camera interactionCamera;
    [HideInInspector] public bool inputEnabled;

    private NetworkCableARSession session;
    private NetworkCableConnector dragging;
    private Plane dragPlane;
    private int completedSteps;

    public int CompletedSteps => completedSteps;
    public NetworkCableKind ExpectedKind => completedSteps < targets.Length
        ? targets[completedSteps].kind : NetworkCableKind.FiberOptic;

    public void Initialize(NetworkCableARSession owner, Camera camera)
    {
        session = owner;
        interactionCamera = camera;
        if (connectors == null || targets == null || connectors.Length != NetworkCableCheckpointRules.TotalSteps ||
            targets.Length != NetworkCableCheckpointRules.TotalSteps)
        {
            Debug.LogError("Network cable workstation requires four connectors and four targets.", this);
            inputEnabled = false;
            return;
        }
        foreach (var connector in connectors) connector?.CaptureHome();
        RefreshTargets();
    }

    private void Update()
    {
        if (!inputEnabled || interactionCamera == null || ARCheckpointSession.BlocksInput ||
            UIManager.HasOpenPanel || SandboxInventoryPanel.IsOpen) return;

        if (PointerPressed(out var pressed)) BeginDrag(pressed);
        if (dragging != null && PointerHeld(out var held)) ContinueDrag(held);
        if (dragging != null && PointerReleased(out var released)) EndDrag(released);
    }

    private void BeginDrag(Vector2 screen)
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        Ray ray = interactionCamera.ScreenPointToRay(screen);
        if (!Physics.Raycast(ray, out var hit, 20f)) return;
        var connector = hit.collider.GetComponentInParent<NetworkCableConnector>();
        if (connector == null || connector.IsLocked) return;
        dragging = connector;
        dragPlane = new Plane(transform.up, connector.transform.position);
    }

    private void ContinueDrag(Vector2 screen)
    {
        Ray ray = interactionCamera.ScreenPointToRay(screen);
        if (dragPlane.Raycast(ray, out float distance)) dragging.MoveTo(ray.GetPoint(distance));
    }

    private void EndDrag(Vector2 screen)
    {
        ContinueDrag(screen);
        var connector = dragging;
        dragging = null;
        if (completedSteps >= targets.Length) { connector.ResetToHome(); return; }

        var target = targets[completedSteps];
        float distance = Vector3.Distance(connector.transform.position, target.snapPoint.position);
        if (distance > snapDistance)
        {
            connector.ResetToHome();
            session?.ReportMiss("Move the connector into the glowing port before releasing it.");
            return;
        }

        if (connector.kind != target.kind)
        {
            connector.ResetToHome();
            session?.ReportWrongCable(connector.kind, target.kind);
            return;
        }

        connector.SnapTo(target.snapPoint);
        completedSteps++;
        RefreshTargets();
        session?.ReportCorrectCable(connector.kind, completedSteps);
    }

    public void ResetProgress()
    {
        completedSteps = 0;
        dragging = null;
        foreach (var connector in connectors) connector?.ResetToHome();
        RefreshTargets();
    }

    public void RestoreProgress(int steps)
    {
        ResetProgress();
        if (steps < 0 || steps > targets.Length) return;
        for (int i = 0; i < steps; i++)
        {
            var target = targets[i];
            var connector = FindConnector(target.kind);
            if (connector == null) throw new System.InvalidOperationException("A saved cable is missing from the workstation.");
            connector.SnapTo(target.snapPoint);
            completedSteps++;
        }
        RefreshTargets();
    }

    private NetworkCableConnector FindConnector(NetworkCableKind kind)
    {
        foreach (var connector in connectors)
            if (connector != null && connector.kind == kind) return connector;
        return null;
    }

    private void RefreshTargets()
    {
        if (targets == null) return;
        for (int i = 0; i < targets.Length; i++)
            targets[i]?.SetState(i == completedSteps, i < completedSteps);
    }

    private static bool PointerPressed(out Vector2 position)
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        { position = Touchscreen.current.primaryTouch.position.ReadValue(); return true; }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        { position = Mouse.current.position.ReadValue(); return true; }
        position = default; return false;
    }

    private static bool PointerHeld(out Vector2 position)
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        { position = Touchscreen.current.primaryTouch.position.ReadValue(); return true; }
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        { position = Mouse.current.position.ReadValue(); return true; }
        position = default; return false;
    }

    private static bool PointerReleased(out Vector2 position)
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
        { position = Touchscreen.current.primaryTouch.position.ReadValue(); return true; }
        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
        { position = Mouse.current.position.ReadValue(); return true; }
        position = default; return false;
    }
}
