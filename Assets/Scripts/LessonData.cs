using UnityEngine;

[CreateAssetMenu(fileName = "New Lesson", menuName = "ARDENT/Lesson")]
public class LessonData : ScriptableObject
{
    [Header("Lesson Information")]
    public int lessonID;

    public string cocID;

    public string lessonTitle;

    [TextArea(3, 5)]
    public string description;

    public string lessonSceneName;

    public int requiredGoldLessonID;

    [Header("Learning Objectives")]
    [TextArea(2, 3)]
    public string[] learningObjectives;

    [Header("Subtopics")]
    public LessonSubtopic[] subtopics;

    [Header("Lesson Slides")]
    public LessonSlideData[] slides;

    [Header("Activity Availability")]
    public bool hasARActivity;

    public bool hasQuiz;

    [Header("Completion Requirements")]
    public bool requiresARActivity;

    public bool requiresQuiz;

    [Header("AR Activity")]
    public ARActivityData arActivity;

    [Header("Quiz")]
    public QuizData quizData;
}