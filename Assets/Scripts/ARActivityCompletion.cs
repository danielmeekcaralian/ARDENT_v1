using UnityEngine;
using UnityEngine.UI;

public class ARActivityCompletion : MonoBehaviour
{
    [Header("Completion Button")]
    [SerializeField] private Button completeButton;

    private LessonData currentLesson;

    private void Start()
    {
        currentLesson = LessonSession.CurrentLesson;

        if (currentLesson == null)
        {
            Debug.LogError(
                "ARActivityCompletion: No current lesson found."
            );

            return;
        }

        if (completeButton != null)
        {
            completeButton.onClick.AddListener(CompleteActivity);
        }
    }

    public void CompleteActivity()
    {
        if (currentLesson == null)
        {
            Debug.LogError(
                "ARActivityCompletion: No current lesson."
            );

            return;
        }

        if (ProgressManager.Instance == null)
        {
            Debug.LogError(
                "ARActivityCompletion: ProgressManager not found."
            );

            return;
        }

        ProgressManager.Instance.CompleteARActivity(
            currentLesson
        );

        Debug.Log(
            $"AR Activity completed for Lesson {currentLesson.lessonID}."
        );
    }
}