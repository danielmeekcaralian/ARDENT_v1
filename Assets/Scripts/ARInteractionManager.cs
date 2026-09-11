using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class ARInteractionManager : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float rotationSpeed = 0.2f;
    [SerializeField] private float scaleSpeed = 0.01f;

    [Header("Scale Limits")]
    [SerializeField] private float minimumScale = 0.25f;
    [SerializeField] private float maximumScale = 2.0f;

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
            SelectObject();
        }

        // -----------------------------------------
        // LEFT DRAG - MOVE OBJECT
        // -----------------------------------------

        if (Mouse.current.leftButton.isPressed &&
            selectedObject != null)
        {
            MoveObject();
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

    private void SelectObject()
    {
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
                selectedObject = manipulator;

                Debug.Log(
                    "Selected: " +
                    selectedObject.gameObject.name
                );
            }
        }
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

        var touchscreen =
            Touchscreen.current;

        var touches =
            touchscreen.touches;

        int activeTouches = 0;

        foreach (var touch in touches)
        {
            if (touch.press.isPressed)
                activeTouches++;
        }

        // -----------------------------------------
        // RESET TWO-FINGER GESTURE
        // -----------------------------------------

        if (activeTouches < 2)
        {
            twoFingerGestureActive = false;
        }

        // -----------------------------------------
        // ONE FINGER
        // -----------------------------------------

        if (activeTouches == 1)
        {
            HandleSingleTouch();
        }

        // -----------------------------------------
        // TWO FINGERS
        // -----------------------------------------

        if (activeTouches == 2)
        {
            HandleTwoFingerGesture();
        }
    }

    private void HandleSingleTouch()
    {
        var touch =
            Touchscreen.current.primaryTouch;

        Vector2 position =
            touch.position.ReadValue();

        if (touch.press.wasPressedThisFrame)
        {
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
                    selectedObject = manipulator;
                }
            }
        }

        if (touch.press.isPressed &&
            selectedObject != null)
        {
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
            first = touch;

        else if (second == null)
        {
            second = touch;
            break;
        }
    }

    if (first == null || second == null)
        return;

    if (selectedObject == null)
        return;

    Vector2 firstPosition =
        first.position.ReadValue();

    Vector2 secondPosition =
        second.position.ReadValue();

    // -----------------------------------------
    // INITIALIZE TWO-FINGER GESTURE
    // -----------------------------------------

    if (!twoFingerGestureActive)
    {
        previousFirstTouchPosition = firstPosition;
        previousSecondTouchPosition = secondPosition;

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
    // SAVE POSITIONS
    // -----------------------------------------

    previousFirstTouchPosition = firstPosition;
    previousSecondTouchPosition = secondPosition;
}

#endif

    public void DeselectObject()
    {
        selectedObject = null;
    }
}