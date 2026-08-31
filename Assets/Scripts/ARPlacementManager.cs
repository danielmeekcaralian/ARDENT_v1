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

    private static readonly List<ARRaycastHit> hits =
        new List<ARRaycastHit>();

    private bool placementPoseIsValid;
    private Pose placementPose;

    public void SetActivity(ARActivityData activity)
    {
        currentActivity = activity;

        Debug.Log(
            "AR Activity loaded: " +
            activity.activityTitle
        );
    }

    private void Update()
    {
        if (currentActivity == null)
            return;

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

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        UpdatePlacementPose(mousePosition);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            PlaceObject();
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

        UpdatePlacementPose(touchPosition);

        if (touch.press.wasPressedThisFrame)
        {
            PlaceObject();
        }
    }

#endif

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
            placementPoseIsValid = true;
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
            placementPoseIsValid = false;

            if (placementIndicator != null)
            {
                placementIndicator.SetActive(false);
            }
        }
    }

    // =========================================================
    // PLACE OBJECT
    // =========================================================

    private void PlaceObject()
    {
        if (!placementPoseIsValid)
        {
            Debug.Log(
                "Cannot place object: " +
                "no AR surface detected."
            );

            return;
        }

        if (currentActivity == null)
            return;

        if (currentActivity.modelPrefab == null)
        {
            Debug.LogError(
                "No model prefab assigned to AR Activity."
            );

            return;
        }

        if (currentObject == null)
        {
            currentObject = Instantiate(
                currentActivity.modelPrefab,
                placementPose.position,
                placementPose.rotation,
                contentParent
            );

            Debug.Log(
                "AR object placed: " +
                currentActivity.modelPrefab.name
            );

            if (placementIndicator != null)
            {
                placementIndicator.SetActive(false);
            }
        }
        else
        {
            currentObject.transform.SetPositionAndRotation(
                placementPose.position,
                placementPose.rotation
            );

            Debug.Log("AR object moved.");
        }
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

        Debug.Log("AR object reset.");
    }
}