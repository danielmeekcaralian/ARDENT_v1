using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class IslandCameraController : MonoBehaviour
{
    [Header("Camera")]
    public Camera mainCamera;

    [Header("Camera Points")]
    public Transform[] points;

    [Header("COC Label")]
    public TMP_Text cocLabel;

    [Header("Camera Movement")]
    public float moveSpeed = 5f;

    [Header("Swipe Settings")]
    public float minimumSwipeDistance = 100f;

    private int index = 0;

    private float fixedY;
    private float fixedZ;

    private Vector2 swipeStartPosition;
    private bool isSwiping = false;


    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("IslandCameraController: Main Camera not found.");
            return;
        }

        if (points == null || points.Length == 0)
        {
            Debug.LogError("IslandCameraController: No camera points assigned.");
            return;
        }

        // Lock camera Y and Z
        fixedY = mainCamera.transform.position.y;
        fixedZ = mainCamera.transform.position.z;

        UpdateCOCLabel();
    }


    void Update()
    {
        MoveCamera();
        HandleSwipe();
    }


    private void MoveCamera()
    {
        if (mainCamera == null || points.Length == 0)
            return;

        Vector3 targetPos = points[index].position;

        // Keep camera on fixed Y and Z
        targetPos.y = fixedY;
        targetPos.z = fixedZ;

        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position,
            targetPos,
            Time.deltaTime * moveSpeed
        );
    }


    private void HandleSwipe()
    {
        HandleTouchSwipe();
        HandleMouseSwipe();
    }


    private void HandleTouchSwipe()
    {
        if (Touchscreen.current == null)
            return;

        var touch = Touchscreen.current.primaryTouch;

        if (touch.press.wasPressedThisFrame)
        {
            swipeStartPosition = touch.position.ReadValue();
            isSwiping = false;
        }

        if (touch.press.isPressed)
        {
            CheckSwipeDistance(touch.position.ReadValue());
        }

        if (touch.press.wasReleasedThisFrame)
        {
            if (isSwiping)
            {
                PerformSwipe(touch.position.ReadValue());
            }

            isSwiping = false;
        }
    }


    private void HandleMouseSwipe()
    {
        if (Mouse.current == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            swipeStartPosition = mousePosition;
            isSwiping = false;
        }

        if (Mouse.current.leftButton.isPressed)
        {
            CheckSwipeDistance(mousePosition);
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (isSwiping)
            {
                PerformSwipe(mousePosition);
            }

            isSwiping = false;
        }
    }


    private void CheckSwipeDistance(Vector2 currentPosition)
    {
        Vector2 movement = currentPosition - swipeStartPosition;

        if (movement.magnitude >= minimumSwipeDistance)
        {
            isSwiping = true;
        }
    }


    private void PerformSwipe(Vector2 endPosition)
    {
        Vector2 swipeDirection =
            endPosition - swipeStartPosition;

        // Ignore mostly vertical swipes
        if (Mathf.Abs(swipeDirection.x) <
            Mathf.Abs(swipeDirection.y))
        {
            return;
        }

        // Swipe LEFT
        if (swipeDirection.x < 0)
        {
            NextIsland();
        }
        // Swipe RIGHT
        else
        {
            PreviousIsland();
        }
    }


    public void NextIsland()
    {
        if (index < points.Length - 1)
        {
            index++;
            UpdateCOCLabel();
        }
    }


    public void PreviousIsland()
    {
        if (index > 0)
        {
            index--;
            UpdateCOCLabel();
        }
    }


    private void UpdateCOCLabel()
    {
        if (cocLabel == null)
            return;

        cocLabel.text = "Level " + (index + 1);
    }
}