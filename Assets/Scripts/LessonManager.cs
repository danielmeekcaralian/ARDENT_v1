using UnityEngine;

public class LessonManager : MonoBehaviour
{
    [SerializeField] private LessonData[] lessons;

    [SerializeField] private LessonCard cardPrefab;

    [SerializeField] private Transform content;

    private void Start()
    {
        foreach (LessonData lesson in lessons)
        {
            bool unlocked = IsUnlocked(lesson);

            Medal medal = GetMedal(lesson.lessonID);

            LessonCard card = Instantiate(cardPrefab, content);

            card.Setup(lesson, unlocked, medal);
        }
    }

    bool IsUnlocked(LessonData lesson)
    {
        if (lesson.requiredGoldLessonID == 0)
            return true;

        Medal previous =
            GetMedal(lesson.requiredGoldLessonID);

        return previous == Medal.Gold;
    }

    Medal GetMedal(int lessonID)
    {
        return (Medal)PlayerPrefs.GetInt(
            $"Lesson{lessonID}Medal",
            0
        );
    }
}