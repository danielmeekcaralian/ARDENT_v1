using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.EventSystems;

public class ARInteractionManager : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float rotationSpeed = 0.8f;
    [SerializeField] private float scaleSpeed = 0.003f;

    [Header("Scale Limits")]
    [SerializeField] private float minimumScale = 0.25f;
    [SerializeField] private float maximumScale = 3.0f;

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

    private Vector2 previousMousePosition;

    private Vector2 previousFirstTouchPosition;
    private Vector2 previousSecondTouchPosition;
    private bool twoFingerGestureActive;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

#if UNITY_EDITOR

        HandleEditorInput();

#else

        HandleMobileInput();

#endif
    }

#if UNITY_EDITOR

    private void HandleEditorInput()
    {
        if (Mouse.current == null)
            return;

        // -----------------------------------------
        // LEFT CLICK - SELECT OBJECT
        // -----------------------------------------

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            SelectObject();
        }

        // -----------------------------------------
        // LEFT DRAG - MOVE OBJECT
        // -----------------------------------------

        if (Mouse.current.leftButton.isPressed &&
            selectedObject != null &&
            !IsPointerOverUI())
        {
            MoveObject();
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame &&
            selectedObject != null)
        {
            CheckAssemblyStep(selectedObject.gameObject);
        }

        // -----------------------------------------
        // RIGHT DRAG - ROTATE OBJECT
        // -----------------------------------------

        if (Mouse.current.rightButton.isPressed &&
            selectedObject != null)
        {
            RotateObject();
        }

        // -----------------------------------------
        // MOUSE WHEEL - SCALE OBJECT
        // -----------------------------------------

        if (selectedObject != null)
        {
            float scroll =
                Mouse.current.scroll.ReadValue().y;

            if (Mathf.Abs(scroll) > 0.01f)
            {
                ScaleObject(scroll);
            }
        }
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current.IsPointerOverGameObject();
    }

    private void SelectObject()
    {
        Debug.Log("ARInteractionManager: SelectObject called");

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        
        if (placementManager != null &&
            placementManager.WasObjectPlacedThisFrame())
        {
            return;
        }

        ARInteractionMode mode = GetCurrentMode();
        Debug.Log("Current AR Mode: " + mode);

        // Do not select objects while placing
        if (mode == ARInteractionMode.Place)
            return;

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        Ray ray =
            mainCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(
                ray,
                out RaycastHit hit))
        {
            ARObjectManipulator manipulator =
                hit.collider.GetComponentInParent<ARObjectManipulator>();

            if (manipulator != null)
            {
                // =========================
                // DELETE MODE
                // =========================

                if (mode == ARInteractionMode.Delete)
                {
                    string objectName =
                        manipulator.gameObject.name;

                    Destroy(manipulator.gameObject);

                    if (selectedObject == manipulator)
                    {
                        selectedObject = null;
                    }

                    if (infoCardManager != null)
                    {
                        infoCardManager.HideInfo();
                    }

                    Debug.Log(
                        "Deleted object: " +
                        objectName
                    );

                    return;
                }

                // =========================
                // EDIT MODE
                // =========================

                if (mode == ARInteractionMode.Edit)
                {
                    selectedObject = manipulator;

                    ARObjectInfo objectInfo =
                    selectedObject.GetComponent<ARObjectInfo>();

                    if (objectInfo != null)
                    {
                        if (infoCardManager != null)
                        {
                            infoCardManager.ShowInfo(objectInfo);
                        }

                        if (activityProgress != null)
                        {
                            activityProgress.MarkObjectInspected(objectInfo);
                        }
                    }

                    return;
                }
            }
        }

        // Nothing was selected
        DeselectObject();
    }

    private void MoveObject()
    {
        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        Ray ray =
            mainCamera.ScreenPointToRay(mousePosition);

        Plane horizontalPlane =
            new Plane(Vector3.up, selectedObject.transform.position);

        if (horizontalPlane.Raycast(
                ray,
                out float distance))
        {
            Vector3 worldPosition =
                ray.GetPoint(distance);

            selectedObject.MoveTo(worldPosition);
        }
    }

    private void RotateObject()
    {
        Vector2 currentPosition =
            Mouse.current.position.ReadValue();

        Vector2 delta =
            currentPosition - previousMousePosition;

        selectedObject.Rotate(
            -delta.x * rotationSpeed
        );

        previousMousePosition = currentPosition;
    }

    private void ScaleObject(float scroll)
    {
        float amount =
            scroll * scaleSpeed;

        selectedObject.ChangeScale(
            amount,
            minimumScale,
            maximumScale
        );
    }

#endif

#if !UNITY_EDITOR

    private void HandleMobileInput()
    {
        if (Touchscreen.current == null)
            return;

        var touches = Touchscreen.current.touches;

        int activeTouches = 0;

        foreach (var touch in touches)
        {
            if (touch.press.isPressed)
                activeTouches++;
        }

        // -----------------------------------------
        // NO TOUCHES
        // -----------------------------------------

        if (activeTouches == 0)
        {
            twoFingerGestureActive = false;
            return;
        }

        // -----------------------------------------
        // TWO FINGERS
        // -----------------------------------------

        if (activeTouches >= 2)
        {
            HandleTwoFingerGesture();
            return;
        }

        // -----------------------------------------
        // ONE FINGER
        // -----------------------------------------

        if (activeTouches == 1)
        {
            HandleSingleTouch();
        }
    }

    private void HandleSingleTouch()
    {
        var touch =
            Touchscreen.current.primaryTouch;

        Vector2 position =
            touch.position.ReadValue();

        // -----------------------------------------
        // TOUCH START
        // -----------------------------------------

        if (touch.press.wasPressedThisFrame)
        {
            // Ignore touches on UI
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // Ignore the frame where an object was placed
            if (placementManager != null &&
                placementManager.WasObjectPlacedThisFrame())
            {
                return;
            }

            ARInteractionMode mode =
                GetCurrentMode();

            Debug.Log(
                "Mobile AR Mode: " + mode
            );

            // Placement mode handles its own tap
            if (mode == ARInteractionMode.Place)
            {
                return;
            }

            Ray ray =
                mainCamera.ScreenPointToRay(position);

            if (Physics.Raycast(
                    ray,
                    out RaycastHit hit))
            {
                ARObjectManipulator manipulator =
                    hit.collider.GetComponentInParent<ARObjectManipulator>();

                if (manipulator != null)
                {
                    // =====================================
                    // DELETE MODE
                    // =====================================

                    if (mode == ARInteractionMode.Delete)
                    {
                        string objectName =
                            manipulator.gameObject.name;

                        Destroy(manipulator.gameObject);

                        if (selectedObject == manipulator)
                        {
                            selectedObject = null;
                        }

                        if (infoCardManager != null)
                        {
                            infoCardManager.HideInfo();
                        }

                        Debug.Log(
                            "Deleted object: " +
                            objectName
                        );

                        return;
                    }

                    // =====================================
                    // EDIT MODE
                    // =====================================

                    if (mode == ARInteractionMode.Edit)
                    {
                        selectedObject = manipulator;

                        ARObjectInfo objectInfo =
                            selectedObject.GetComponent<ARObjectInfo>();

                        if (objectInfo != null)
                        {
                            if (infoCardManager != null)
                            {
                                infoCardManager.ShowInfo(
                                    objectInfo
                                );
                            }

                            if (activityProgress != null)
                            {
                                activityProgress.MarkObjectInspected(
                                    objectInfo
                                );
                            }
                        }

                        Debug.Log(
                            "Selected AR object: " +
                            manipulator.gameObject.name
                        );

                        return;
                    }
                }
            }

            // Nothing was hit
            DeselectObject();

            return;
        }

        // -----------------------------------------
        // ONE-FINGER DRAG
        // -----------------------------------------

        if (touch.press.isPressed &&
            selectedObject != null)
        {
            // Don't move while touching UI
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // Don't move in Delete mode
            if (GetCurrentMode() != ARInteractionMode.Edit)
            {
                return;
            }

            MoveObjectMobile(position);
        }
    }

    private void MoveObjectMobile(Vector2 screenPosition)
    {
        Ray ray =
            mainCamera.ScreenPointToRay(screenPosition);

        Plane horizontalPlane =
            new Plane(Vector3.up, selectedObject.transform.position);

        if (horizontalPlane.Raycast(
                ray,
                out float distance))
        {
            Vector3 worldPosition =
                ray.GetPoint(distance);

            selectedObject.MoveTo(worldPosition);
        }
    }

    private void HandleTwoFingerGesture()
    {
        var touches =
            Touchscreen.current.touches;

        TouchControl first = null;
        TouchControl second = null;

        foreach (var touch in touches)
        {
            if (!touch.press.isPressed)
                continue;

            if (first == null)
            {
                first = touch;
            }
            else if (second == null)
            {
                second = touch;
                break;
            }
        }

        if (first == null || second == null)
            return;

        // No object selected
        if (selectedObject == null)
            return;

        // Don't manipulate objects outside Edit mode
        if (GetCurrentMode() != ARInteractionMode.Edit)
            return;

        Vector2 firstPosition =
            first.position.ReadValue();

        Vector2 secondPosition =
            second.position.ReadValue();

        // -----------------------------------------
        // START TWO-FINGER GESTURE
        // -----------------------------------------

        if (!twoFingerGestureActive)
        {
            previousFirstTouchPosition =
                firstPosition;

            previousSecondTouchPosition =
                secondPosition;

            twoFingerGestureActive = true;

            return;
        }

        // -----------------------------------------
        // SCALE
        // -----------------------------------------

        float previousDistance =
            Vector2.Distance(
                previousFirstTouchPosition,
                previousSecondTouchPosition
            );

        float currentDistance =
            Vector2.Distance(
                firstPosition,
                secondPosition
            );

        float distanceDelta =
            currentDistance - previousDistance;

        float scaleAmount =
            distanceDelta * scaleSpeed;

        selectedObject.ChangeScale(
            scaleAmount,
            minimumScale,
            maximumScale
        );

        // -----------------------------------------
        // ROTATION
        // -----------------------------------------

        Vector2 previousDirection =
            previousSecondTouchPosition -
            previousFirstTouchPosition;

        Vector2 currentDirection =
            secondPosition -
            firstPosition;

        float previousAngle =
            Mathf.Atan2(
                previousDirection.y,
                previousDirection.x
            ) * Mathf.Rad2Deg;

        float currentAngle =
            Mathf.Atan2(
                currentDirection.y,
                currentDirection.x
            ) * Mathf.Rad2Deg;

        float angleDelta =
            Mathf.DeltaAngle(
                previousAngle,
                currentAngle
            );

        selectedObject.Rotate(
            -angleDelta * rotationSpeed
        );

        // -----------------------------------------
        // SAVE TOUCH POSITIONS
        // -----------------------------------------

        previousFirstTouchPosition =
            firstPosition;

        previousSecondTouchPosition =
            secondPosition;
    }

#endif

    public void DeselectObject()
    {
        selectedObject = null;

        if (infoCardManager != null)
        {
            infoCardManager.HideInfo();
        }
    }

    private ARInteractionMode GetCurrentMode()
    {
        if (modeManager == null)
            return ARInteractionMode.Edit;

        return modeManager.CurrentMode;
    }

    private void CheckAssemblyStep(GameObject selectedObject)
    {
        if (assemblyManager == null)
            return;

        if (selectedObject == null)
            return;

        assemblyManager.TryCompleteCurrentStep(selectedObject);
    }
}