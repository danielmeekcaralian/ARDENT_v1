using UnityEngine;

[CreateAssetMenu(fileName = "New Lesson", menuName = "ARDENT/Lesson")]
public class LessonData : ScriptableObject
{
    public int lessonID;

    public string lessonTitle;

    [TextArea(3, 5)]
    public string description;

    public string lessonSceneName;

    public int requiredGoldLessonID;

    [Header("Learning Objectives")]
    [TextArea(2, 3)]
    public string[] learningObjectives;

    [Header("Lesson Slides")]
    public LessonSlideData[] slides;

    [Header("AR Activity")]
    public ARActivityData arActivity;

    [Header("Quiz")]
    public QuizData quizData;
}