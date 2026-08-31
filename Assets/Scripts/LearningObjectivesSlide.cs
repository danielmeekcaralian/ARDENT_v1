using TMPro;
using UnityEngine;

public class LearningObjectivesSlide : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text lessonTitleText;
    [SerializeField] private Transform objectivesContainer;

    [Header("Prefab")]
    [SerializeField] private GameObject objectivePrefab;

    private void Start()
    {
        if (LessonSession.CurrentLesson != null)
        {
            Setup(LessonSession.CurrentLesson);
        }
        else
        {
            Debug.LogError("No current lesson found!");
        }
    }

    public void Setup(LessonData lesson)
    {
        if (lessonTitleText == null)
        {
            Debug.LogError("Lesson Title Text is not assigned!");
            return;
        }

        if (objectivesContainer == null)
        {
            Debug.LogError("Objectives Container is not assigned!");
            return;
        }

        if (objectivePrefab == null)
        {
            Debug.LogError("Objective Prefab is not assigned!");
            return;
        }

        lessonTitleText.text = lesson.lessonTitle;

        foreach (Transform child in objectivesContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (string objective in lesson.learningObjectives)
        {
            GameObject objectiveObject =
                Instantiate(
                    objectivePrefab,
                    objectivesContainer
                );

            TMP_Text objectiveText =
                objectiveObject.GetComponentInChildren<TMP_Text>();

            if (objectiveText != null)
            {
                objectiveText.text = objective;
            }
            else
            {
                Debug.LogError(
                    "No TMP_Text found on the objective prefab!"
                );
            }
        }
    }
}