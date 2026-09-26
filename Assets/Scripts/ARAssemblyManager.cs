using System.Collections.Generic;
using TMPro;
using UnityEngine;

public partial class ARAssemblyManager : MonoBehaviour
{
    private ARAssemblyActivityData assemblyActivity;

    [Header("Assembly UI")]
    [SerializeField] private TMP_Text stepTitleText;
    [SerializeField] private TMP_Text instructionsText;

    private readonly Dictionary<string, AssemblyAnchor>
        assemblyAnchors =
            new Dictionary<string, AssemblyAnchor>();

    private int currentStepIndex = 0;
    private float assemblyScaleFactor = 1f;

    public void SetAssemblyScale(float factor)
    {
        assemblyScaleFactor = Mathf.Max(0.001f, factor);
    }

    // =========================================================
    // SET ACTIVITY
    // =========================================================

    public void SetActivity(
        ARAssemblyActivityData activity)
    {
        ResetCombinedActivity();
        assemblyActivity = activity;

        currentStepIndex = 0;

        assemblyAnchors.Clear();

        if (assemblyActivity == null)
        {
            Debug.LogError(
                "ARAssemblyManager: " +
                "No Assembly Activity provided."
            );

            return;
        }

        if (assemblyActivity.steps == null ||
            assemblyActivity.steps.Length == 0)
        {
            Debug.LogError(
                "ARAssemblyManager: " +
                "No assembly steps found."
            );

            return;
        }

        Debug.Log(
            "Assembly started: " +
            assemblyActivity.activityTitle
        );

        Phase = ActivityPhase.Assembly;
        ShowCurrentStep();
    }

    // =========================================================
    // REGISTER ANCHOR
    // =========================================================

    public void RegisterAssemblyAnchor(
        AssemblyAnchor anchor)
    {
        if (anchor == null)
            return;

        if (string.IsNullOrWhiteSpace(
                anchor.anchorID))
        {
            Debug.LogError(
                "ARAssemblyManager: " +
                "AssemblyAnchor has no Anchor ID."
            );

            return;
        }

        if (assemblyAnchors.ContainsKey(
                anchor.anchorID))
        {
            Debug.LogWarning(
                "ARAssemblyManager: Replacing existing " +
                "assembly anchor: " +
                anchor.anchorID
            );
        }

        assemblyAnchors[anchor.anchorID] =
            anchor;

        Debug.Log(
            "Assembly anchor registered: " +
            anchor.anchorID
        );
    }

    // =========================================================
    // SHOW CURRENT STEP
    // =========================================================

    private void ShowCurrentStep()
    {
        RefreshProgress();
        if (assemblyActivity == null)
            return;

        if (currentStepIndex < 0 ||
            currentStepIndex >=
            assemblyActivity.steps.Length)
        {
            return;
        }

        AssemblyStepData step =
            assemblyActivity.steps[
                currentStepIndex
            ];

        if (stepTitleText != null)
        {
            stepTitleText.text =
                step.stepTitle;
        }

        if (instructionsText != null)
        {
            instructionsText.text =
                step.instruction;
        }

        Debug.Log(
            "Assembly Step " +
            step.stepNumber +
            ": " +
            step.stepTitle
        );

        Debug.Log(
            "Instruction: " +
            step.instruction
        );
    }

    // =========================================================
    // COMPLETE CURRENT STEP
    // =========================================================

    public bool TryAlignCurrentComponentRotation(GameObject candidate)
    {
        // Optional placement aid: keep position and size under the user's control.
        if (Phase != ActivityPhase.Assembly || !IsCurrentStepComponent(candidate)) return false;
        var manipulator = candidate.GetComponent<ARObjectManipulator>();
        if (manipulator == null || manipulator.IsLocked) return false;
        var step = assemblyActivity.steps[currentStepIndex];
        var anchor = FindAnchor(step.anchorID);
        var target = anchor != null ? anchor.FindTarget(step.targetID) : null;
        if (target == null)
        {
            if (instructionsText != null)
                instructionsText.text = step.instruction + "\nPlace the required base before aligning.";
            return false;
        }
        candidate.transform.rotation = target.transform.rotation;
        if (instructionsText != null)
            instructionsText.text = step.instruction + "\nRotation aligned. Move the component to its target.";
        return true;
    }

    public bool IsCurrentStepComponent(GameObject candidate)
    {
        if (candidate == null) return false;
        if (Phase == ActivityPhase.Disassembly)
            return removalIndex >= 0 && removalIndex < installedParts.Count &&
                candidate == installedParts[removalIndex];
        if (Phase != ActivityPhase.Assembly || assemblyActivity == null ||
            assemblyActivity.steps == null || currentStepIndex < 0 ||
            currentStepIndex >= assemblyActivity.steps.Length || installedParts.Contains(candidate))
            return false;
        var step = assemblyActivity.steps[currentStepIndex];
        var required = step.component?.prefab != null
            ? step.component.prefab.GetComponent<ARObjectInfo>() : null;
        var actual = candidate.GetComponent<ARObjectInfo>();
        return required != null && actual != null && required.objectName == actual.objectName;
    }

    public bool TryCompleteCurrentStep(
        GameObject placedObject)
    {
        if (Phase == ActivityPhase.Disassembly)
            return TryRemoveCurrentPart(placedObject);
        if (Phase != ActivityPhase.Assembly || assemblyActivity == null)
            return false;
        if (installedParts.Contains(placedObject))
            return false;

        if (assemblyActivity.steps == null ||
            assemblyActivity.steps.Length == 0)
        {
            return false;
        }

        if (currentStepIndex < 0 ||
            currentStepIndex >=
            assemblyActivity.steps.Length)
        {
            Debug.Log(
                "Assembly activity is already completed."
            );

            return false;
        }

        if (placedObject == null)
            return false;

        AssemblyStepData step =
            assemblyActivity.steps[
                currentStepIndex
            ];

        // -----------------------------------------------------
        // CHECK COMPONENT
        // -----------------------------------------------------

        ARObjectInfo objectInfo =
            placedObject.GetComponent<ARObjectInfo>();

        if (objectInfo == null)
        {
            Debug.Log(
                "Placed object has no ARObjectInfo."
            );

            return false;
        }

        if (step.component == null ||
            step.component.prefab == null)
        {
            Debug.LogError(
                "Assembly step has no " +
                "component assigned."
            );

            return false;
        }

        ARObjectInfo requiredObjectInfo =
            step.component.prefab
                .GetComponent<ARObjectInfo>();

        if (requiredObjectInfo == null)
        {
            Debug.LogError(
                "Required component prefab " +
                "has no ARObjectInfo."
            );

            return false;
        }

        string requiredName =
            requiredObjectInfo.objectName;

        if (objectInfo.objectName !=
            requiredName)
        {
            if (instructionsText != null)
                instructionsText.text = step.instruction + "\nSelect " + requiredName + " to continue.";
            Debug.Log(
                "Wrong component. Required: " +
                requiredName
            );

            return false;
        }

        // -----------------------------------------------------
        // FIND ANCHOR
        // -----------------------------------------------------

        AssemblyAnchor anchor =
            FindAnchor(step.anchorID);

        if (anchor == null)
        {
            if (instructionsText != null)
                instructionsText.text = step.instruction + "\nPlace the required base first: " + step.anchorID;
            Debug.Log(
                "Required assembly anchor " +
                "has not been placed: " +
                step.anchorID
            );

            return false;
        }

        // -----------------------------------------------------
        // FIND TARGET
        // -----------------------------------------------------

        AssemblyTarget target =
            anchor.FindTarget(
                step.targetID
            );

        if (target == null)
        {
            Debug.LogError(
                "Assembly target not found. " +
                "Anchor: " +
                step.anchorID +
                ", Target: " +
                step.targetID
            );

            return false;
        }

        // -----------------------------------------------------
        // CHECK DISTANCE
        // -----------------------------------------------------

        float distance =
            Vector3.Distance(
                placedObject.transform.position,
                target.transform.position
            );

        float allowedDistance = step.snapDistance * assemblyScaleFactor;
        float angleError = Quaternion.Angle(
            placedObject.transform.rotation, target.transform.rotation);
        bool closeEnough = distance <= allowedDistance;
        bool aligned = angleError <= step.rotationTolerance;
        if (!closeEnough || !aligned)
        {
            string guidance = !closeEnough && !aligned ? "Move closer and rotate to match the target."
                : !closeEnough ? "Move closer to the target." : "Rotate to match the target.";
            string measurements = $"Distance: {distance:F3} (max {allowedDistance:F3}) | " +
                $"Angle: {angleError:F1} deg (max {step.rotationTolerance:F1})";
            if (instructionsText != null)
                instructionsText.text = step.instruction + "\n" + guidance + "\n" + measurements;
            Debug.Log($"Snap pending for {requiredName}: {measurements}");
            return false;
        }
        placedObject.transform.position =
            target.transform.position;

        placedObject.transform.rotation =
            target.transform.rotation;

        // Parent component to the anchor.
        placedObject.transform.SetParent(
            anchor.transform,
            true
        );

        // -----------------------------------------------------
        // LOCK COMPONENT
        // -----------------------------------------------------

        ARObjectManipulator manipulator =
            placedObject
                .GetComponent<ARObjectManipulator>();

        if (manipulator != null)
        {
            manipulator.SetLocked(true);
        }

        Debug.Log(
            "Assembly step completed: " +
            step.stepTitle
        );

        installedParts.Add(placedObject);
        AdvanceStep();

        return true;
    }

    // =========================================================
    // FIND ANCHOR
    // =========================================================

    private AssemblyAnchor FindAnchor(
        string anchorID)
    {
        if (string.IsNullOrWhiteSpace(
                anchorID))
        {
            Debug.LogError(
                "Assembly step has no Anchor ID."
            );

            return null;
        }

        if (assemblyAnchors.TryGetValue(
                anchorID,
                out AssemblyAnchor anchor))
        {
            return anchor;
        }

        return null;
    }

    // =========================================================
    // ADVANCE STEP
    // =========================================================

    private void AdvanceStep()
    {
        currentStepIndex++;

        if (currentStepIndex >= assemblyActivity.steps.Length)
        {
            FinishAssemblyPhase();
            return;
        }

        ShowCurrentStep();
    }
}