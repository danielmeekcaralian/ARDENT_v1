using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ARAssemblyManager : MonoBehaviour
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

    public bool TryCompleteCurrentStep(
        GameObject placedObject)
    {
        if (assemblyActivity == null)
        {
            return false;
        }

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

        if (distance >
            step.snapDistance * assemblyScaleFactor)
        {
            if (instructionsText != null)
                instructionsText.text = step.instruction + "\nMove the component closer to the target.";
            Debug.Log(
                "Component is not close enough " +
                "to the target."
            );

            return false;
        }

        // -----------------------------------------------------
        // SNAP COMPONENT
        // -----------------------------------------------------

        float angleError = Quaternion.Angle(
            placedObject.transform.rotation,
            target.transform.rotation
        );

        if (angleError > step.rotationTolerance)
        {
            if (instructionsText != null)
                instructionsText.text = step.instruction +
                    "\nRotate the component to match the target.";

            Debug.Log($"Incorrect rotation: {angleError:F1} degrees; " +
                $"allowed: {step.rotationTolerance:F1} degrees.");
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
            Debug.Log("Assembly completed!");

            ARActivityProgress activityProgress =
                FindFirstObjectByType<ARActivityProgress>();

            if (activityProgress == null)
            {
                Debug.LogError(
                    "ARAssemblyManager: ARActivityProgress not found."
                );

                return;
            }

            // Save completion and show ViewCompletionButton.
            activityProgress.CompleteActivity();

            return;
        }

        ShowCurrentStep();
    }
}