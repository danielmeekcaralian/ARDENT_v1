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
        if (progressText != null) progressText.gameObject.SetActive(activity == null || (activity.activityType != ARActivityType.Sandbox && activity.activityType != ARActivityType.NetworkDesign && activity.activityType != ARActivityType.RJ45Termination && activity.activityType != ARActivityType.NetworkCables));

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
        if (objectInfo == null || currentActivity == null ||
            (currentActivity.activityType != ARActivityType.ToolIdentification &&
             currentActivity.activityType != ARActivityType.HardwareIdentification))
            return;

        if (inspectedObjects.Contains(objectInfo.objectName))
            return;

        inspectedObjects.Add(objectInfo.objectName);

        UpdateProgressUI();

        Debug.Log(
            $"Inspected: {objectInfo.objectName}"
        );

        CheckCompletion();
        ARCheckpointSession.SaveCurrent();
    }

    public string[] CaptureInspectedObjects()
    {
        var names = new string[inspectedObjects.Count];
        inspectedObjects.CopyTo(names);
        return names;
    }

    public void RestoreInspectedObjects(string[] names)
    {
        inspectedObjects.Clear();
        if (names != null && currentActivity != null && currentActivity.availableObjects != null)
        {
            var valid = new HashSet<string>();
            foreach (var item in currentActivity.availableObjects)
            {
                var info = item?.prefab != null ? item.prefab.GetComponent<ARObjectInfo>() : null;
                if (info != null) valid.Add(info.objectName);
            }
            foreach (var name in names) if (name != null && valid.Contains(name)) inspectedObjects.Add(name);
        }
        UpdateProgressUI();
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

        if (currentActivity != null && currentActivity.activityType == ARActivityType.Assembly)
        {
            var manager = FindFirstObjectByType<ARAssemblyManager>();
            if (manager != null) manager.RefreshProgress();
            return;
        }

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

    public void SetAssemblyProgress(string phase, int done, int total)
    {
        if (progressText != null)
            progressText.text = $"{phase}: {done} / {total}";
    }

    public bool TryCompleteNetworkActivity(ARActivityData expectedActivity)
    {
        var lesson = LessonSession.CurrentLesson;
        if (ARSandboxSession.IsActive || expectedActivity == null || currentActivity != expectedActivity ||
            expectedActivity.activityType != ARActivityType.NetworkDesign || lesson == null ||
            lesson.arActivity != expectedActivity || ProgressManager.Instance == null) return false;
        CompleteActivity();
        return true;
    }

    public bool TryCompleteRJ45Activity(RJ45CableTester tester)
    {
        var lesson = LessonSession.CurrentLesson;
        if (ARSandboxSession.IsActive || tester == null || !tester.isActiveAndEnabled ||
            tester.gameObject.scene != gameObject.scene || !tester.IsPassed ||
            currentActivity == null || currentActivity.activityType != ARActivityType.RJ45Termination ||
            tester.Activity != currentActivity || lesson == null || lesson.arActivity != currentActivity ||
            ProgressManager.Instance == null) return false;
        CompleteActivity();
        return true;
    }

    public bool TryCompleteNetworkCableActivity(ARActivityData expectedActivity)
    {
        var lesson = LessonSession.CurrentLesson;
        if (ARSandboxSession.IsActive || expectedActivity == null || currentActivity != expectedActivity ||
            expectedActivity.activityType != ARActivityType.NetworkCables || lesson == null ||
            lesson.arActivity != expectedActivity || ProgressManager.Instance == null) return false;
        CompleteActivity();
        return true;
    }

    public void CompleteActivity()
    {
        if (ARSandboxSession.IsActive || (currentActivity != null && currentActivity.activityType == ARActivityType.Sandbox)) return;
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

        ARCheckpointSession.CompleteCurrent();
        ArdentAudioManager.Play(ArdentSound.Success);

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

        ArdentMotion.SetPanelVisible(completionPanel, true);
    }

    public void ContinueFromCompletion()
    {
        if (ARSandboxSession.IsActive) { ArdentMotion.LoadScene("Hardware_Library"); return; }
        ArdentMotion.LoadScene("ActivitySelectionScene");
    }
}


