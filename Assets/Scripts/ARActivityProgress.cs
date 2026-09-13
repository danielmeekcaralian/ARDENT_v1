using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ARActivityProgress : MonoBehaviour
{
    [Header("Progress UI")]
    [SerializeField] private TMP_Text progressText;

    private HashSet<string> inspectedObjects =
        new HashSet<string>();

    private ARActivityData currentActivity;

    public void SetActivity(ARActivityData activity)
    {
        currentActivity = activity;

        inspectedObjects.Clear();

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

        progressText.text =
            $"Tools Inspected: " +
            $"{inspectedObjects.Count} / " +
            $"{GetRequiredObjectCount()}";
    }

    private void CheckCompletion()
    {
        int requiredCount =
            GetRequiredObjectCount();

        if (requiredCount == 0)
            return;

        if (inspectedObjects.Count >= requiredCount)
        {
            Debug.Log(
                "AR ACTIVITY COMPLETE!"
            );

            CompleteActivity();
        }
    }

    private void CompleteActivity()
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

        ProgressManager.Instance.CompleteARActivity(
            currentLesson
        );
    }
}