using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlacementManager : MonoBehaviour
{
    [Header("AR References")]
    [SerializeField]
    private ARRaycastManager raycastManager;

    [Header("Content")]
    [SerializeField]
    private Transform contentParent;

    [Header("Placement Indicator")]
    [SerializeField]
    private GameObject placementIndicator;

    [Header("Assembly Zoom (1x = calibrated size)")]
    [SerializeField, Min(1f)] private float maximumAssemblyMultiplier = 4f;

    private Transform assemblyRoot;
    public Transform AssemblyRoot => assemblyRoot;
    public bool AllowMovement => currentActivity == null || currentActivity.allowMovement;
    public bool AllowRotation => currentActivity == null || currentActivity.allowRotation;
    public bool AllowScaling => currentActivity == null || currentActivity.allowScaling;

    public bool CanPlaceObjects
    {
        get
        {
            if (!IsAssemblyActivity) return true;
            var manager = FindFirstObjectByType<ARAssemblyManager>();
            return manager == null || manager.CanPlaceObjects;
        }
    }

    public void CancelPlacement()
    {
        isPlacing = false;
        selectedObjectData = null;
        if (placementIndicator != null) placementIndicator.SetActive(false);
    }

    private float assemblyScale = 1f;
    public bool IsAssemblyActivity =>
        currentActivity != null &&
        currentActivity.activityType == ARActivityType.Assembly;

    public void ScaleAssembly(float amount)
    {
        if (!IsAssemblyActivity || !currentActivity.allowScaling ||
            assemblyRoot == null)
            return;

        assemblyScale = ARScaleMath.NextMultiplier(assemblyScale, amount, maximumAssemblyMultiplier);

        assemblyRoot.localScale = Vector3.one * assemblyScale;
        ARAssemblyManager manager = FindFirstObjectByType<ARAssemblyManager>();
        if (manager != null)
            manager.SetAssemblyScale(assemblyScale);
    }

    private Transform GetPlacementParent(Pose pose)
    {
        if (!IsAssemblyActivity)
            return contentParent;

        if (assemblyRoot == null)
        {
            assemblyRoot = new GameObject("AssemblyRoot").transform;
            assemblyRoot.SetParent(contentParent, false);
            // Resize around the first placement rather than the scene origin.
            assemblyRoot.position = pose.position;
            assemblyRoot.localRotation = Quaternion.identity;
            assemblyRoot.localScale = Vector3.one * assemblyScale;
        }

        return assemblyRoot;
    }

    private GameObject currentObject;

    private ARActivityData currentActivity;

    private ARObjectData selectedObjectData;

    private bool isPlacing;

    private static readonly List<ARRaycastHit>
        hits =
            new List<ARRaycastHit>();

    private int lastPlacedFrame = -1;

    private Pose placementPose;

    // =========================================================
    // SET ACTIVITY
    // =========================================================

    public void SetActivity(
        ARActivityData activity)
    {
        currentActivity = activity;

        if (currentActivity == null)
        {
            Debug.LogError(
                "ARPlacementManager: " +
                "No AR Activity provided."
            );

            return;
        }

        Debug.Log(
            "AR Activity loaded: " +
            currentActivity.activityTitle
        );
    }

    // =========================================================
    // SELECT OBJECT
    // =========================================================

    public void SelectObject(
        ARObjectData objectData)
    {
        if (!CanPlaceObjects || objectData == null)
            return;

        if (objectData.prefab == null)
        {
            Debug.LogError(
                "Selected AR object has " +
                "no prefab assigned."
            );

            return;
        }

        selectedObjectData =
            objectData;

        isPlacing = true;

        if (placementIndicator != null)
        {
            placementIndicator.SetActive(
                true
            );
        }

        Debug.Log(
            "AR object selected for placement: " +
            objectData.prefab.name
        );
    }

    // =========================================================
    // UPDATE
    // =========================================================

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

        if (!isPlacing || !CanPlaceObjects)
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

        if (Mouse.current.leftButton
            .wasPressedThisFrame)
        {
            PlaceObjectAtPosition(
                mousePosition
            );
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
            if (IsExistingObject(
                    touchPosition))
            {
                return;
            }

            PlaceObjectAtPosition(
                touchPosition
            );
        }
    }

#endif

    // =========================================================
    // CHECK EXISTING OBJECT
    // =========================================================

    private bool IsExistingObject(
        Vector2 screenPosition)
    {
        Camera cam =
            Camera.main;

        if (cam == null)
            return false;

        Ray ray =
            cam.ScreenPointToRay(
                screenPosition
            );

        if (Physics.Raycast(
                ray,
                out RaycastHit hit))
        {
            ARObjectManipulator manipulator =
                hit.collider
                    .GetComponentInParent<
                        ARObjectManipulator>();

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
    // UPDATE PLACEMENT INDICATOR
    // =========================================================

    private void UpdatePlacementIndicator()
    {
        if (!isPlacing)
        {
            if (placementIndicator != null)
            {
                placementIndicator.SetActive(
                    false
                );
            }

            return;
        }

        if (raycastManager == null)
            return;

        Camera cam =
            Camera.main;

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
                TrackableType
                    .PlaneWithinPolygon))
        {
            placementPose =
                hits[0].pose;

            if (placementIndicator != null)
            {
                placementIndicator.SetActive(
                    true
                );

                placementIndicator.transform
                    .SetPositionAndRotation(
                        placementPose.position,
                        placementPose.rotation
                    );
            }
        }
        else
        {
            if (placementIndicator != null)
            {
                placementIndicator.SetActive(
                    false
                );
            }
        }
    }

    // =========================================================
    // PLACE OBJECT
    // =========================================================

    private void PlaceObjectAtPosition(
        Vector2 screenPosition)
    {
        if (!isPlacing || !CanPlaceObjects)
            return;

        if (raycastManager == null)
            return;

        if (currentActivity == null)
            return;

        if (selectedObjectData == null)
        {
            Debug.Log(
                "No AR object selected."
            );

            return;
        }

        if (selectedObjectData.prefab == null)
        {
            Debug.LogError(
                "Selected AR object has " +
                "no prefab assigned."
            );

            return;
        }

        if (!raycastManager.Raycast(
                screenPosition,
                hits,
                TrackableType
                    .PlaneWithinPolygon))
        {
            Debug.Log(
                "Cannot place object: " +
                "no AR plane detected."
            );

            return;
        }

        Pose tapPose =
            hits[0].pose;

        GameObject newObject =
            Instantiate(
                selectedObjectData.prefab,
                tapPose.position,
                tapPose.rotation,
                GetPlacementParent(tapPose)
            );

        if (IsAssemblyActivity)
        {
            // Keep the authored LOCAL size so new parts inherit the shared scale.
            newObject.transform.localScale =
                selectedObjectData.prefab.transform.localScale;
        }
        // Capture the final baseline after placement parenting/scale setup, never
        // infer it from a global raw-scale limit on the first resize gesture.
        var scaleController = newObject.GetComponent<ARObjectManipulator>();
        if (scaleController != null) scaleController.InitializeScaleBaseline();

        // -----------------------------------------------------
        // REGISTER ASSEMBLY ANCHOR
        // -----------------------------------------------------

        if (currentActivity.activityType ==
            ARActivityType.Assembly)
        {
            AssemblyAnchor anchor =
                newObject
                    .GetComponent<AssemblyAnchor>();

            if (anchor != null)
            {
                ARAssemblyManager
                    assemblyManager =
                        FindFirstObjectByType<
                            ARAssemblyManager>();

                if (assemblyManager != null)
                {
                    assemblyManager
                        .RegisterAssemblyAnchor(
                            anchor
                        );
                }
            }
        }

        currentObject =
            newObject;

        lastPlacedFrame =
            Time.frameCount;

        isPlacing = false;

        selectedObjectData = null;

        // Return to Edit mode
        ARModeManager modeManager =
            FindFirstObjectByType<
                ARModeManager>();

        if (modeManager != null)
        {
            modeManager.SetEditMode();
        }

        if (placementIndicator != null)
        {
            placementIndicator.SetActive(
                false
            );
        }

        Debug.Log(
            "AR object placed at tap position: " +
            newObject.name
        );
    }

    // =========================================================
    // RESET LAST OBJECT
    // =========================================================

    public void ResetObject()
    {
        var manager = FindFirstObjectByType<ARAssemblyManager>();
        if (IsAssemblyActivity && manager != null && !manager.CanDeleteObject(currentObject))
            return;

        if (currentObject != null)
        {
            Destroy(
                currentObject
            );

            currentObject = null;
        }

        if (placementIndicator != null)
        {
            placementIndicator.SetActive(
                true
            );
        }

        Debug.Log(
            "Last placed AR object reset."
        );
    }

    // =========================================================
    // PLACEMENT FRAME CHECK
    // =========================================================

    public bool WasObjectPlacedThisFrame()
    {
        return lastPlacedFrame ==
               Time.frameCount;
    }
}