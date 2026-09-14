using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlacementManager : MonoBehaviour
{
    [Header("AR References")]
    [SerializeField] private ARRaycastManager raycastManager;

    [Header("Content")]
    [SerializeField] private Transform contentParent;

    [Header("Placement Indicator")]
    [SerializeField] private GameObject placementIndicator;

    private GameObject currentObject;
    private ARActivityData currentActivity;
    private ARObjectData selectedObjectData;
    private bool isPlacing;

    private static readonly List<ARRaycastHit> hits =
        new List<ARRaycastHit>();

    private int lastPlacedFrame = -1;
    private Pose placementPose;

    public void SetActivity(ARActivityData activity)
    {
        currentActivity = activity;

        Debug.Log(
            "AR Activity loaded: " +
            activity.activityTitle
        );
    }

    public void SelectObject(ARObjectData objectData)
    {
        if (objectData == null)
            return;

        if (objectData.prefab == null)
        {
            Debug.LogError("Selected AR object has no prefab assigned.");
            return;
        }

        selectedObjectData = objectData;
        isPlacing = true;

        if (placementIndicator != null)
        {
            placementIndicator.SetActive(true);
        }

        Debug.Log(
            "AR object selected for placement: " +
            objectData.prefab.name
        );
    }

    private void Update()
    {
        if (currentActivity == null)
            return;

        UpdatePlacementIndicator();

#if UNITY_EDITOR
        HandleEditorInput();
#else
    HandleTouchInput();
#endif
    }

    // =========================================================
    // EDITOR INPUT
    // =========================================================

#if UNITY_EDITOR

    private void HandleEditorInput()
    {
        if (Mouse.current == null)
            return;

        if (!isPlacing)
            return;

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        if (float.IsNaN(mousePosition.x) ||
            float.IsNaN(mousePosition.y) ||
            float.IsInfinity(mousePosition.x) ||
            float.IsInfinity(mousePosition.y))
        {
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            PlaceObjectAtPosition(mousePosition);
        }
    }

#endif

    // =========================================================
    // MOBILE INPUT
    // =========================================================

#if !UNITY_EDITOR

    private void HandleTouchInput()
{
    if (Touchscreen.current == null)
        return;

    var touch =
        Touchscreen.current.primaryTouch;

    Vector2 touchPosition =
        touch.position.ReadValue();

    if (touch.press.wasPressedThisFrame)
    {
        if (IsExistingObject(touchPosition))
            return;

        PlaceObjectAtPosition(touchPosition);
    }
}

#endif

    // =========================================================
    // CHECK EXISTING AR OBJECT
    // =========================================================

    private bool IsExistingObject(Vector2 screenPosition)
    {
        Camera cam = Camera.main;

        if (cam == null)
            return false;

        Ray ray =
            cam.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(
                ray,
                out RaycastHit hit))
        {
            ARObjectManipulator manipulator =
                hit.collider.GetComponentInParent<ARObjectManipulator>();

            if (manipulator != null)
            {
                Debug.Log(
                    "Tapped existing object: " +
                    manipulator.gameObject.name
                );

                return true;
            }
        }

        return false;
    }

    // =========================================================
    // UPDATE PLACEMENT POSITION
    // =========================================================

    private void UpdatePlacementPose(Vector2 screenPosition)
    {
        if (raycastManager == null)
            return;

        if (raycastManager.Raycast(
                screenPosition,
                hits,
                TrackableType.PlaneWithinPolygon))
        {
            placementPose = hits[0].pose;

            if (placementIndicator != null)
            {
                placementIndicator.SetActive(true);

                placementIndicator.transform.SetPositionAndRotation(
                    placementPose.position,
                    placementPose.rotation
                );
            }
        }
        else
        {
            if (placementIndicator != null)
            {
                placementIndicator.SetActive(false);
            }
        }
    }

    private void UpdatePlacementIndicator()
    {
        if (!isPlacing)
        {
            if (placementIndicator != null)
            {
                placementIndicator.SetActive(false);
            }

            return;
        }

        if (raycastManager == null)
            return;

        Camera cam = Camera.main;

        if (cam == null)
            return;

        Vector2 screenCenter =
            new Vector2(
                Screen.width / 2f,
                Screen.height / 2f
            );

        if (raycastManager.Raycast(
                screenCenter,
                hits,
                TrackableType.PlaneWithinPolygon))
        {
            placementPose = hits[0].pose;

            if (placementIndicator != null)
            {
                placementIndicator.SetActive(true);

                placementIndicator.transform.SetPositionAndRotation(
                    placementPose.position,
                    placementPose.rotation
                );
            }
        }
        else
        {
            if (placementIndicator != null)
            {
                placementIndicator.SetActive(false);
            }
        }
    }

    // =========================================================
    // PLACE OBJECT
    // =========================================================

    private void PlaceObjectAtPosition(Vector2 screenPosition)
    {
        if (!isPlacing)
        {
            return;
        }
        if (raycastManager == null)
            return;

        if (currentActivity == null)
            return;

        if (selectedObjectData == null)
        {
            Debug.Log("No AR object selected.");

            return;
        }

        if (selectedObjectData.prefab == null)
        {
            Debug.LogError(
                "Selected AR object has no prefab assigned."
            );

            return;
        }

        if (!raycastManager.Raycast(
                screenPosition,
                hits,
                TrackableType.PlaneWithinPolygon))
        {
            Debug.Log(
                "Cannot place object: no AR plane detected at tap position."
            );

            return;
        }

        Pose tapPose = hits[0].pose;

        GameObject newObject = Instantiate(
            selectedObjectData.prefab,
            tapPose.position,
            tapPose.rotation,
            contentParent
        );

        ARAssemblyManager assemblyManager = FindFirstObjectByType<ARAssemblyManager>();

        if (assemblyManager != null)
        {
            ARObjectInfo objectInfo =
                newObject.GetComponent<ARObjectInfo>();

            if (objectInfo != null &&
                objectInfo.objectName == "Motherboard")
            {
                assemblyManager.SetAssemblyAnchor(newObject);
            }
        }

        currentObject = newObject;
        lastPlacedFrame = Time.frameCount;

        isPlacing = false;
        selectedObjectData = null;

        if (placementIndicator != null)
        {
            placementIndicator.SetActive(false);
        }

        Debug.Log(
            "AR object placed at tap position: " +
            newObject.name
        );
    }

    // =========================================================
    // RESET
    // =========================================================

    public void ResetObject()
    {
        if (currentObject != null)
        {
            Destroy(currentObject);
            currentObject = null;
        }

        if (placementIndicator != null)
        {
            placementIndicator.SetActive(true);
        }

        Debug.Log("Last placed AR object reset.");
    }
    public bool WasObjectPlacedThisFrame()
    {
        return lastPlacedFrame == Time.frameCount;
    }
}