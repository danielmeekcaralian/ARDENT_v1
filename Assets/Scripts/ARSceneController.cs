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

    [Header("Activity Routing")]
    [SerializeField] private ARActivityRouter activityRouter;

    private LessonData currentLesson;
    private ARActivityData activityData;


    private void Start()
    {
        foreach (var root in gameObject.scene.GetRootGameObjects())
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                if (canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace &&
                    canvas.GetComponent<ARCanvasTextRefresh>() == null)
                    canvas.gameObject.AddComponent<ARCanvasTextRefresh>();

        if (ARSandboxSession.IsActive) LoadSandbox();
        else LoadCurrentLesson();
    }

    private void LoadSandbox()
    {
        activityData = ARSandboxSession.Activity;
        if (activityRouter != null) activityRouter.RouteActivity(activityData);
        if (placementManager != null) placementManager.SetActivity(activityData);
        if (inventoryUI != null) inventoryUI.SetActivity(activityData);
        if (activityProgress != null) activityProgress.SetActivity(activityData);
        if (activityTitleText != null) activityTitleText.text = "AR Sandbox";
        var assembly = FindFirstObjectByType<ARAssemblyManager>();
        if (assembly != null) assembly.EnterSandbox();
        // No checkpoint session is initialized; saved lesson checkpoints remain untouched.
        foreach (var root in gameObject.scene.GetRootGameObjects())
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == "ARCheckpointUI") child.gameObject.SetActive(false);
        if (ARSandboxSession.SelectedItem != null && placementManager != null)
            placementManager.SelectObject(ARSandboxSession.SelectedItem);
        else if (inventoryUI != null) inventoryUI.OpenInventory();
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

        if (activityData == null)
        {
            Debug.LogError(
                "ARSceneController: No AR Activity Data " +
                "assigned to this lesson."
            );

            return;
        }

        if (activityRouter != null)
        {
            activityRouter.RouteActivity(activityData);
        }

        Debug.Log(
            "AR Activity Type: " +
            activityData.activityType
        );

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

        if (activityData.activityType == ARActivityType.RJ45Termination)
        {
            var rj45 = GetComponent<RJ45ARSession>();
            if (rj45 == null) rj45 = gameObject.AddComponent<RJ45ARSession>();
            rj45.Initialize(placementManager);
            return;
        }

        var checkpoints = GetComponent<ARCheckpointSession>();
        if (checkpoints == null) checkpoints = gameObject.AddComponent<ARCheckpointSession>();
        checkpoints.Initialize(currentLesson, activityProgress, placementManager);

        Debug.Log(
            "AR Activity: " +
            activityData.activityTitle
        );
    }
}
