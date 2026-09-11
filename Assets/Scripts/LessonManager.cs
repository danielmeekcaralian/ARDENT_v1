using UnityEngine;

public class LessonManager : MonoBehaviour
{
    [Header("Lesson Database")]
    [SerializeField] private LessonDatabase lessonDatabase;

    [Header("Lesson Card")]
    [SerializeField] private LessonCard cardPrefab;

    [SerializeField] private Transform content;

    private void Start()
    {
        LoadLessonsForCurrentCOC();
    }

    private void LoadLessonsForCurrentCOC()
    {
        string cocID = LessonSession.CurrentCOC;

        if (string.IsNullOrEmpty(cocID))
        {
            Debug.LogError(
                "LessonManager: No COC has been selected."
            );

            return;
        }

        if (lessonDatabase == null)
        {
            Debug.LogError(
                "LessonManager: LessonDatabase is not assigned."
            );

            return;
        }

        LessonData[] lessons =
            lessonDatabase.GetLessonsForCOC(cocID);

        if (lessons.Length == 0)
        {
            Debug.LogWarning(
                $"LessonManager: No lessons found for {cocID}."
            );

            return;
        }

        Debug.Log(
            $"Loading {lessons.Length} lessons for {cocID}"
        );

        foreach (LessonData lesson in lessons)
        {
            if (lesson == null)
                continue;

            bool unlocked = IsUnlocked(lesson);

            Medal medal = GetMedal(lesson.lessonID);

            LessonCard card =
                Instantiate(cardPrefab, content);

            card.Setup(
                lesson,
                unlocked,
                medal
            );
        }
    }

    private bool IsUnlocked(LessonData lesson)
    {
        if (lesson.requiredGoldLessonID == 0)
            return true;

        Medal previous =
            GetMedal(lesson.requiredGoldLessonID);

        return previous == Medal.Gold;
    }

    private Medal GetMedal(int lessonID)
    {
        return (Medal)PlayerPrefs.GetInt(
            $"Lesson{lessonID}Medal",
            0
        );
    }
}