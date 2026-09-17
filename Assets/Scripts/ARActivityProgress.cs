using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ARActivityProgress : MonoBehaviour
{
    [Header("Progress UI")]
    [SerializeField] private TMP_Text progressText;

    [Header("Completion UI")]
    [SerializeField] private GameObject completionButton;
    [SerializeField] private GameObject completionPanel;

    private HashSet<string> inspectedObjects =
        new HashSet<string>();

    private ARActivityData currentActivity;

    public void SetActivity(ARActivityData activity)
    {
        currentActivity = activity;

        inspectedObjects.Clear();

        if (completionButton != null)
        {
            completionButton.SetActive(false);
        }

        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
        }

        UpdateProgressUI();

        Debug.Log(
            $"AR Activity Progress initialized: " +
            $"{GetRequiredObjectCount()} required objects."
        );
    }

    public void MarkObjectInspected(ARObjectInfo objectInfo)
    {
        if (objectInfo == null)
            return;

        if (inspectedObjects.Contains(objectInfo.objectName))
            return;

        inspectedObjects.Add(objectInfo.objectName);

        UpdateProgressUI();

        Debug.Log(
            $"Inspected: {objectInfo.objectName}"
        );

        CheckCompletion();
    }

    private int GetRequiredObjectCount()
    {
        if (currentActivity == null ||
            currentActivity.availableObjects == null)
        {
            return 0;
        }

        return currentActivity.availableObjects.Length;
    }

    private void UpdateProgressUI()
    {
        if (progressText == null)
            return;

        string label = "Objects Inspected";

        if (currentActivity != null)
        {
            switch (currentActivity.activityType)
            {
                case ARActivityType.ToolIdentification:
                    label = "Tools Inspected";
                    break;

                case ARActivityType.HardwareIdentification:
                    label = "Hardware Inspected";
                    break;
            }
        }

        progressText.text =
            $"{label}: " +
            $"{inspectedObjects.Count} / " +
            $"{GetRequiredObjectCount()}";
    }

    private void CheckCompletion()
    {
        if (currentActivity == null)
            return;

        // Inspection-based completion only applies to
        // Tool Identification and Hardware Identification.
        if (currentActivity.activityType != ARActivityType.ToolIdentification &&
            currentActivity.activityType != ARActivityType.HardwareIdentification)
        {
            return;
        }

        int requiredCount =
            GetRequiredObjectCount();

        if (requiredCount == 0)
            return;

        if (inspectedObjects.Count >= requiredCount)
        {
            Debug.Log(
                "AR Identification Activity COMPLETE!"
            );

            CompleteActivity();
        }
    }

    public void CompleteActivity()
    {
        LessonData currentLesson =
            LessonSession.CurrentLesson;

        if (currentLesson == null)
        {
            Debug.LogError(
                "ARActivityProgress: No current lesson found."
            );

            return;
        }

        if (ProgressManager.Instance == null)
        {
            Debug.LogError(
                "ARActivityProgress: ProgressManager not found."
            );

            return;
        }

        // Save AR activity completion
        ProgressManager.Instance.CompleteARActivity(
            currentLesson
        );

        // Show the button that allows the user
        // to open the completion popup.
        if (completionButton != null)
        {
            completionButton.SetActive(true);
        }

        Debug.Log(
            "AR Identification Activity completed. " +
            "Completion button shown."
        );
    }

    public void ShowCompletionPanel()
    {
        if (completionPanel == null)
        {
            Debug.LogWarning(
                "ARActivityProgress: Completion panel is not assigned."
            );

            return;
        }

        completionPanel.SetActive(true);
    }

    public void ContinueFromCompletion()
    {
        SceneManager.LoadScene("ActivitySelectionScene");
    }
}