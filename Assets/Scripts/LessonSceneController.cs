using UnityEngine;
using TMPro;

public class LessonSceneController : MonoBehaviour
{
    [Header("Lesson UI")]
    [SerializeField] private TMP_Text lessonTitleText;
    [SerializeField] private TMP_Text lessonDescriptionText;

    [Header("Learning Objectives")]
    [SerializeField] private TMP_Text objectivesText;

    private void Start()
    {
        LessonData lesson = LessonSession.CurrentLesson;

        if (lesson == null)
        {
            Debug.LogError("No lesson selected!");
            return;
        }

        DisplayLesson(lesson);
    }

    private void DisplayLesson(LessonData lesson)
    {
        lessonTitleText.text = lesson.lessonTitle;
        lessonDescriptionText.text = lesson.description;

        objectivesText.text = "";

        foreach (string objective in lesson.learningObjectives)
        {
            objectivesText.text += "• " + objective + "\n";
        }

        Debug.Log("Loaded lesson: " + lesson.lessonTitle);
    }
}