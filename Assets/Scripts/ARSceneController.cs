using TMPro;
using UnityEngine;

public class ARSceneController : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private ARPlacementManager placementManager;
    [SerializeField] private ARInventoryUI inventoryUI;
    [SerializeField] private ARActivityProgress activityProgress;

    [Header("AR UI")]
    [SerializeField] private TMP_Text activityTitleText;

    private LessonData currentLesson;
    private ARActivityData activityData;


    private void Start()
    {
        LoadCurrentLesson();
    }

    private void LoadCurrentLesson()
    {
        currentLesson = LessonSession.CurrentLesson;

        if (currentLesson == null)
        {
            Debug.LogError(
                "ARSceneController: No current lesson found."
            );

            return;
        }

        Debug.Log(
            "Current Lesson: " +
            currentLesson.lessonTitle
        );

        if (!currentLesson.hasARActivity)
        {
            Debug.LogWarning(
                "This lesson does not have an AR activity."
            );

            return;
        }

        activityData = currentLesson.arActivity;

        Debug.Log(
            "AR Activity Type: " +
            activityData.activityType
        );

        if (activityData == null)
        {
            Debug.LogError(
                "ARSceneController: No AR Activity Data " +
                "assigned to this lesson."
            );

            return;
        }

        // Set AR activity
        if (placementManager != null)
        {
            placementManager.SetActivity(activityData);
        }

        // Set inventory activity
        if (inventoryUI != null)
        {
            inventoryUI.SetActivity(activityData);
        }

        // Set activity progress
        if (activityProgress != null)
        {
            activityProgress.SetActivity(activityData);
        }

        // Set activity title
        if (activityTitleText != null)
        {
            activityTitleText.text =
                activityData.activityTitle;
        }

        Debug.Log(
            "AR Activity: " +
            activityData.activityTitle
        );
    }
}