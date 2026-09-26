using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.EventSystems;

public partial class ARInteractionManager : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float rotationSpeed = 0.6f;
    [SerializeField, Min(0.00001f)] private float mouseZoomSensitivity = 0.001f;
    [SerializeField, Min(0.1f)] private float pinchZoomSensitivity = 1f;
    [SerializeField, Min(1f)] private float dragThresholdPixels = 10f;

    [Header("Object Zoom (1x = calibrated size)")]
    [SerializeField, Min(1f)] private float maximumObjectMultiplier = 4f;
    [Header("Info Card")]
    [SerializeField] private ARInfoCardManager infoCardManager;
    [SerializeField] private ARActivityProgress activityProgress;
    [SerializeField] private ARModeManager modeManager;
    [Header("Placement")]
    [SerializeField] private ARPlacementManager placementManager;
    [Header("Assembly")]
    [SerializeField] private ARAssemblyManager assemblyManager;

    private Camera mainCamera;
    private ARObjectManipulator selectedObject;
    private ARObjectManipulator dragObject;
    private Plane dragPlane;
    private Vector2 dragStartScreen;
    private Vector3 dragOffset;
    private bool dragging;
#if UNITY_EDITOR
    private Vector2 previousMousePosition;
    private ARObjectManipulator rotationObject;
    private bool rotated;
#else
    private Vector2 previousFirstTouchPosition;
    private Vector2 previousSecondTouchPosition;
    private bool twoFingerGestureActive;
    private bool touchGestureChanged;
#endif

    private void Awake() { mainCamera = Camera.main; }

    private void OnValidate()
    {
        mouseZoomSensitivity = Mathf.Max(0.00001f, mouseZoomSensitivity);
        pinchZoomSensitivity = Mathf.Max(0.1f, pinchZoomSensitivity);
        maximumObjectMultiplier = Mathf.Max(1f, maximumObjectMultiplier);
    }

    private void Update()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (UpdatePrecisionControls())
        {
            CancelWorldDrag();
            return;
        }
        if (mainCamera == null) return;
#if UNITY_EDITOR
        HandleEditorInput();
#else
        HandleMobileInput();
#endif
    }

#if UNITY_EDITOR
    private void HandleEditorInput()
    {
        if (Mouse.current == null) return;
        var mouse = Mouse.current;
        Vector2 position = mouse.position.ReadValue();
        if (mouse.leftButton.wasPressedThisFrame) SelectAt(position);
        if (mouse.leftButton.isPressed) DragTo(position);
        if (mouse.leftButton.wasReleasedThisFrame) EndWorldDrag();

        if (mouse.rightButton.wasPressedThisFrame)
        {
            previousMousePosition = position;
            rotationObject = !IsScreenPositionOverUI(position) && CanManipulateSelection()
                ? selectedObject : null;
            rotated = false;
        }
        if (mouse.rightButton.isPressed && rotationObject != null &&
            rotationObject == selectedObject && CanManipulateSelection() &&
            !IsScreenPositionOverUI(position) && (placementManager == null || placementManager.AllowRotation))
        {
            float amount = -(position.x - previousMousePosition.x) * rotationSpeed;
            rotationObject.Rotate(amount);
            rotated |= Mathf.Abs(amount) > 0.001f;
        }
        previousMousePosition = position;
        if (mouse.rightButton.wasReleasedThisFrame)
        {
            var target = rotationObject;
            rotationObject = null;
            if (rotated && target != null && target == selectedObject)
                CheckAssemblyStep(target.gameObject);
            rotated = false;
        }
        if (selectedObject != null && GetCurrentMode() == ARInteractionMode.Edit &&
            !IsScreenPositionOverUI(position))
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f) ApplyScale(scroll * mouseZoomSensitivity);
        }
    }
#else
    private void HandleMobileInput()
    {
        if (Touchscreen.current == null) return;
        var primary = Touchscreen.current.primaryTouch;
        if (primary.press.wasReleasedThisFrame)
        {
            bool checkGesture = touchGestureChanged;
            touchGestureChanged = false;
            var target = selectedObject;
            bool checkedDrag = EndWorldDrag();
            if (!checkedDrag && checkGesture && target != null && target == selectedObject)
                CheckAssemblyStep(target.gameObject);
            twoFingerGestureActive = false;
            return;
        }
        TouchControl first = null, second = null;
        foreach (var touch in Touchscreen.current.touches)
        {
            if (!touch.press.isPressed) continue;
            if (first == null) first = touch;
            else { second = touch; break; }
        }
        if (first == null)
        {
            CancelWorldDrag();
            twoFingerGestureActive = false;
            touchGestureChanged = false;
            return;
        }
        if (second != null)
        {
            touchGestureChanged |= dragging;
            CancelWorldDrag();
            HandleTwoFingerGesture(first.position.ReadValue(), second.position.ReadValue());
            return;
        }
        // A pinch ending must not turn into a fresh one-finger drag.
        twoFingerGestureActive = false;
        Vector2 position = primary.position.ReadValue();
        if (primary.press.wasPressedThisFrame)
        {
            touchGestureChanged = false;
            SelectAt(position);
        }
        if (primary.press.isPressed) DragTo(position);
    }

    private void HandleTwoFingerGesture(Vector2 first, Vector2 second)
    {
        if (selectedObject == null || GetCurrentMode() != ARInteractionMode.Edit ||
            IsScreenPositionOverUI(first) || IsScreenPositionOverUI(second))
        {
            twoFingerGestureActive = false;
            return;
        }
        if (!twoFingerGestureActive)
        {
            previousFirstTouchPosition = first;
            previousSecondTouchPosition = second;
            twoFingerGestureActive = true;
            return;
        }
        float distanceDelta = Vector2.Distance(first, second) -
            Vector2.Distance(previousFirstTouchPosition, previousSecondTouchPosition);
        float previousDistance = Vector2.Distance(previousFirstTouchPosition, previousSecondTouchPosition);
        float currentDistance = Vector2.Distance(first, second);
        if (previousDistance > 1f && currentDistance > 1f)
            ApplyScale(Mathf.Log(currentDistance / previousDistance) * pinchZoomSensitivity);
        Vector2 previous = previousSecondTouchPosition - previousFirstTouchPosition;
        Vector2 current = second - first;
        float angleDelta = Mathf.DeltaAngle(Mathf.Atan2(previous.y, previous.x) * Mathf.Rad2Deg,
            Mathf.Atan2(current.y, current.x) * Mathf.Rad2Deg);
        if (placementManager == null || placementManager.AllowRotation)
            selectedObject.Rotate(-angleDelta * rotationSpeed);
        touchGestureChanged |= Mathf.Abs(distanceDelta) > 0.01f || Mathf.Abs(angleDelta) > 0.01f;
        previousFirstTouchPosition = first;
        previousSecondTouchPosition = second;
    }
#endif

    private bool CanManipulateSelection()
    {
        return selectedObject != null && !selectedObject.IsLocked && GetCurrentMode() == ARInteractionMode.Edit;
    }

    private void SelectAt(Vector2 screenPosition)
    {
        CancelWorldDrag();
        if (IsScreenPositionOverUI(screenPosition) ||
            (placementManager != null && placementManager.WasObjectPlacedThisFrame())) return;
        var mode = GetCurrentMode();
        if (mode == ARInteractionMode.Place) return;
        var candidate = PickObject(mainCamera.ScreenPointToRay(screenPosition), mode);
        if (candidate == null) { DeselectObject(); return; }
        if (mode == ARInteractionMode.Delete)
        {
            if (assemblyManager != null && !assemblyManager.CanDeleteObject(candidate.gameObject)) return;
            if (selectedObject == candidate) DeselectObject();
            Destroy(candidate.gameObject);
            if (infoCardManager != null) infoCardManager.HideInfo();
            return;
        }
        if (mode != ARInteractionMode.Edit) return;
        CancelPrecisionAdjustment();
        selectedObject = candidate;
        var info = candidate.GetComponent<ARObjectInfo>();
        if (info != null)
        {
            if (infoCardManager != null) infoCardManager.ShowInfo(info);
            if (activityProgress != null) activityProgress.MarkObjectInspected(info);
        }
        if (!CanManipulateSelection() || (placementManager != null && !placementManager.AllowMovement)) return;
        dragPlane = new Plane(Vector3.up, candidate.transform.position);
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        if (!dragPlane.Raycast(ray, out float distance)) return;
        // Keep the grabbed location under the pointer instead of teleporting the pivot.
        dragOffset = candidate.transform.position - ray.GetPoint(distance);
        dragStartScreen = screenPosition;
        dragObject = candidate;
    }

    private ARObjectManipulator PickObject(Ray ray, ARInteractionMode mode)
    {
        ARObjectManipulator closest = null, required = null;
        float closestDistance = float.PositiveInfinity, requiredDistance = float.PositiveInfinity;
        foreach (RaycastHit hit in Physics.RaycastAll(ray))
        {
            var candidate = hit.collider.GetComponentInParent<ARObjectManipulator>();
            if (candidate == null) continue;
            if (hit.distance < closestDistance) { closest = candidate; closestDistance = hit.distance; }
            // The current step's component remains selectable through a surrounding case collider.
            if (mode == ARInteractionMode.Edit && assemblyManager != null && !candidate.IsLocked &&
                assemblyManager.IsCurrentStepComponent(candidate.gameObject) && hit.distance < requiredDistance)
            {
                required = candidate;
                requiredDistance = hit.distance;
            }
        }
        return required != null ? required : closest;
    }

    private void DragTo(Vector2 position)
    {
        if (dragObject == null || dragObject != selectedObject || !CanManipulateSelection() ||
            (placementManager != null && !placementManager.AllowMovement))
        { CancelWorldDrag(); return; }
        if (IsScreenPositionOverUI(position)) { CancelWorldDrag(); return; }
        if (!dragging && (position - dragStartScreen).sqrMagnitude <
            dragThresholdPixels * dragThresholdPixels) return;
        Ray ray = mainCamera.ScreenPointToRay(position);
        if (!dragPlane.Raycast(ray, out float distance)) return;
        dragging = true;
        dragObject.MoveTo(ray.GetPoint(distance) + dragOffset);
    }

    private bool EndWorldDrag()
    {
        var target = dragObject;
        bool moved = dragging;
        CancelWorldDrag();
        if (!moved || target == null || target != selectedObject) return false;
        CheckAssemblyStep(target.gameObject);
        return true;
    }

    private void CancelWorldDrag() { dragObject = null; dragging = false; }

    private readonly System.Collections.Generic.List<RaycastResult> uiHits =
        new System.Collections.Generic.List<RaycastResult>();
    private bool IsScreenPositionOverUI(Vector2 position)
    {
        if (EventSystem.current == null) return false;
        var pointer = new PointerEventData(EventSystem.current) { position = position };
        uiHits.Clear();
        EventSystem.current.RaycastAll(pointer, uiHits);
        foreach (var hit in uiHits)
            if (hit.module is UnityEngine.UI.GraphicRaycaster) return true;
        return false;
    }

    private void ApplyScale(float amount)
    {
        if (placementManager != null && !placementManager.AllowScaling) return;
        if (placementManager != null && placementManager.IsAssemblyActivity)
        {
            placementManager.ScaleAssembly(amount);
            return;
        }
        if (selectedObject != null) selectedObject.ChangeScale(amount, maximumObjectMultiplier);
    }
    public void DeselectObject()
    {
        CancelWorldDrag();
        CancelPrecisionAdjustment();
        selectedObject = null;
        if (infoCardManager != null) infoCardManager.HideInfo();
    }
    private ARInteractionMode GetCurrentMode()
    {
        if (assemblyManager != null && assemblyManager.RequiresEditMode) return ARInteractionMode.Edit;
        return modeManager != null ? modeManager.CurrentMode : ARInteractionMode.Edit;
    }
    private void CheckAssemblyStep(GameObject candidate)
    {
        if (assemblyManager != null && candidate != null) assemblyManager.TryCompleteCurrentStep(candidate);
    }
}
