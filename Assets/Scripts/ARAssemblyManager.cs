using TMPro;
using UnityEngine;

public class ARAssemblyManager : MonoBehaviour
{
    [Header("Assembly Activity")]
    [SerializeField] private ARAssemblyActivityData assemblyActivity;

    [Header("Assembly UI")]
    [SerializeField] private TMP_Text stepTitleText;
    [SerializeField] private TMP_Text instructionsText;

    private Transform assemblyTargets;
    private GameObject assemblyAnchor;

    private int currentStepIndex = 0;

    private void Start()
    {
        if (assemblyActivity == null)
        {
            Debug.LogError(
                "ARAssemblyManager: No Assembly Activity assigned."
            );
            return;
        }

        if (assemblyActivity.steps == null ||
            assemblyActivity.steps.Length == 0)
        {
            Debug.LogError(
                "ARAssemblyManager: No assembly steps found."
            );
            return;
        }

        Debug.Log(
            "Assembly started: " +
            assemblyActivity.activityTitle
        );

        ShowCurrentStep();
    }

    public void SetAssemblyAnchor(GameObject motherboard)
    {
        if (motherboard == null)
            return;

        Transform targets =
            motherboard.transform.Find("AssemblyTargets");

        if (targets == null)
        {
            Debug.LogError(
                "ARAssemblyManager: AssemblyTargets not found on motherboard."
            );

            return;
        }

        assemblyAnchor = motherboard;
        assemblyTargets = targets;

        Debug.Log(
            "Assembly anchor set to motherboard: " +
            motherboard.name
        );
    }

    private void ShowCurrentStep()
    {
        AssemblyStepData step =
            assemblyActivity.steps[currentStepIndex];

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

    public bool TryCompleteCurrentStep(
    GameObject placedObject)
    {
        if (assemblyActivity == null)
        {
            Debug.LogError(
                "ARAssemblyManager: No assembly activity assigned."
            );

            return false;
        }

        if (assemblyActivity.steps.Length == 0)
        {
            Debug.LogError(
                "ARAssemblyManager: No assembly steps available."
            );

            return false;
        }

        if (currentStepIndex < 0 ||
            currentStepIndex >= assemblyActivity.steps.Length)
        {
            Debug.Log(
                "Assembly activity is already completed."
            );

            return false;
        }

        if (placedObject == null)
            return false;

        AssemblyStepData step =
            assemblyActivity.steps[currentStepIndex];

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
                "Assembly step has no component assigned."
            );

            return false;
        }

        ARObjectInfo requiredObjectInfo =
            step.component.prefab.GetComponent<ARObjectInfo>();

        if (requiredObjectInfo == null)
        {
            Debug.LogError(
                "Required component prefab has no ARObjectInfo."
            );

            return false;
        }

        string requiredName =
            requiredObjectInfo.objectName;

        if (objectInfo.objectName != requiredName)
        {
            Debug.Log(
                "Wrong component. Required: " +
                requiredName
            );

            return false;
        }

        AssemblyTarget target =
            FindTarget(step.targetID);

        if (target == null)
        {
            Debug.LogError(
                "Assembly target not found: " +
                step.targetID
            );

            return false;
        }

        float distance =
            Vector3.Distance(
                placedObject.transform.position,
                target.transform.position
            );

        if (distance > step.snapDistance)
        {
            Debug.Log(
                "Component is not close enough to the target."
            );

            return false;
        }

        // Snap component to target
        placedObject.transform.position =
            target.transform.position;

        placedObject.transform.rotation =
            target.transform.rotation;

        // Attach component to motherboard
        if (assemblyAnchor != null)
        {
            placedObject.transform.SetParent(
                assemblyAnchor.transform,
                true
            );
        }

        // Lock individual manipulation
        ARObjectManipulator manipulator =
            placedObject.GetComponent<ARObjectManipulator>();

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

    private void AdvanceStep()
    {
        currentStepIndex++;

        if (currentStepIndex >=
            assemblyActivity.steps.Length)
        {
            Debug.Log("Assembly completed!");

            LessonData currentLesson =
                LessonSession.CurrentLesson;

            if (currentLesson == null)
            {
                Debug.LogError(
                    "ARAssemblyManager: No current lesson found."
                );

                return;
            }

            if (ProgressManager.Instance == null)
            {
                Debug.LogError(
                    "ARAssemblyManager: ProgressManager not found."
                );

                return;
            }

            // Save AR activity completion
            ProgressManager.Instance.CompleteARActivity(
                currentLesson
            );

            Debug.Log(
                "Assembly AR activity completed and saved."
            );

            return;
        }

        ShowCurrentStep();
    }

    private AssemblyTarget FindTarget(string targetID)
    {
        if (assemblyTargets == null)
            return null;

        AssemblyTarget[] targets =
            assemblyTargets.GetComponentsInChildren<AssemblyTarget>();

        foreach (AssemblyTarget target in targets)
        {
            if (target.targetID == targetID)
                return target;
        }

        return null;
    }
}