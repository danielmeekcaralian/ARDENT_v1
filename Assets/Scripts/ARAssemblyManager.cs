using UnityEngine;
using TMPro;

public class ARAssemblyManager : MonoBehaviour
{
    [Header("Assembly Activity")]
    [SerializeField] private ARAssemblyActivityData assemblyActivity;

    [Header("UI")]
    [SerializeField] private TMP_Text stepTitleText;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_Text stepCounterText;

    private int currentStepIndex = 0;

    private AssemblyStepData CurrentStep
    {
        get
        {
            if (assemblyActivity == null ||
                assemblyActivity.steps == null ||
                currentStepIndex >= assemblyActivity.steps.Length)
            {
                return null;
            }

            return assemblyActivity.steps[currentStepIndex];
        }
    }

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

        currentStepIndex = 0;

        DisplayCurrentStep();
    }

    private void DisplayCurrentStep()
    {
        AssemblyStepData step = CurrentStep;

        if (step == null)
        {
            CompleteAssembly();
            return;
        }

        if (stepTitleText != null)
        {
            stepTitleText.text = step.stepTitle;
        }

        if (instructionText != null)
        {
            instructionText.text = step.instruction;
        }

        if (stepCounterText != null)
        {
            stepCounterText.text =
                $"Step {currentStepIndex + 1} / " +
                $"{assemblyActivity.steps.Length}";
        }

        Debug.Log(
            $"Assembly Step {step.stepNumber}: " +
            $"{step.stepTitle}"
        );
    }

    public void CheckComponentPlacement(
        ARObjectManipulator component)
    {
        if (component == null)
            return;

        AssemblyStepData step = CurrentStep;

        if (step == null)
            return;

        if (step.targetPosition == null)
        {
            Debug.LogWarning(
                "ARAssemblyManager: No target position " +
                "assigned for this step."
            );

            return;
        }

        float distance =
            Vector3.Distance(
                component.transform.position,
                step.targetPosition.position
            );

        Debug.Log(
            $"Distance from target: {distance}"
        );

        if (distance <= step.snapDistance)
        {
            SnapComponent(component);
        }
        else
        {
            Debug.Log(
                "Component is not close enough to the target."
            );
        }
    }

    private void SnapComponent(
        ARObjectManipulator component)
    {
        AssemblyStepData step = CurrentStep;

        if (step == null ||
            step.targetPosition == null)
        {
            return;
        }

        component.transform.position =
            step.targetPosition.position;

        component.transform.rotation =
            step.targetPosition.rotation;

        Debug.Log(
            $"Component successfully installed: " +
            $"{step.stepTitle}"
        );

        currentStepIndex++;

        if (currentStepIndex >=
            assemblyActivity.steps.Length)
        {
            CompleteAssembly();
        }
        else
        {
            DisplayCurrentStep();
        }
    }

    private void CompleteAssembly()
    {
        Debug.Log(
            "AR ASSEMBLY ACTIVITY COMPLETE!"
        );

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

        ProgressManager.Instance.CompleteARActivity(
            currentLesson
        );
    }
}