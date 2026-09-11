using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class TouchSelector : MonoBehaviour
{
    [Header("Tap / Swipe Settings")]
    [SerializeField] private float swipeThreshold = 100f;

    // Touch
    private Vector2 touchStartPosition;
    private bool touchActive = false;

    // Mouse
    private Vector2 mouseStartPosition;
    private bool mouseActive = false;


    void Update()
    {
        HandleTouch();
        HandleMouse();
    }


    // =========================================================
    // TOUCH
    // =========================================================

    private void HandleTouch()
    {
        if (Touchscreen.current == null)
            return;

        var touch = Touchscreen.current.primaryTouch;


        // Finger touches the screen
        if (touch.press.wasPressedThisFrame)
        {
            touchStartPosition = touch.position.ReadValue();
            touchActive = true;
        }


        // Finger released
        if (touch.press.wasReleasedThisFrame && touchActive)
        {
            Vector2 touchEndPosition = touch.position.ReadValue();

            float distance = Vector2.Distance(
                touchStartPosition,
                touchEndPosition
            );

            touchActive = false;


            // Swipe → DO NOT select
            if (distance >= swipeThreshold)
            {
                return;
            }


            // Ignore UI
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }


            // Tap → select
            SelectIsland(touchEndPosition);
        }
    }


    // =========================================================
    // MOUSE
    // =========================================================

    private void HandleMouse()
    {
        if (Mouse.current == null)
            return;

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();


        // Mouse button pressed
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            mouseStartPosition = mousePosition;
            mouseActive = true;
        }


        // Mouse button released
        if (Mouse.current.leftButton.wasReleasedThisFrame &&
            mouseActive)
        {
            Vector2 mouseEndPosition = mousePosition;

            float distance = Vector2.Distance(
                mouseStartPosition,
                mouseEndPosition
            );

            mouseActive = false;


            // Drag → DO NOT select
            if (distance >= swipeThreshold)
            {
                return;
            }


            // Ignore UI
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }


            // Click → select
            SelectIsland(mouseEndPosition);
        }
    }


    // =========================================================
    // ISLAND SELECTION
    // =========================================================

    private void SelectIsland(Vector2 screenPosition)
    {
        if (Camera.main == null)
            return;

        Ray ray =
            Camera.main.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Island island =
                hit.collider.GetComponent<Island>();

            if (island != null)
            {
                island.SelectIsland();
            }
        }
    }
}
