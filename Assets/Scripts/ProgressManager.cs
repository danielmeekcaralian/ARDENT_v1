using UnityEngine;

public class ProgressManager : MonoBehaviour
{
    public static ProgressManager Instance;

    [Header("Lesson Database")]
    [SerializeField] private LessonDatabase lessonDatabase;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public int GetUnlockedIsland()
    {
        return PlayerPrefs.GetInt("UnlockedIsland", 0);
    }

    public void UnlockNextIsland(int completedIslandIndex)
    {
        int unlocked = GetUnlockedIsland();

        if (completedIslandIndex >= unlocked)
        {
            int nextIsland = completedIslandIndex + 1;

            PlayerPrefs.SetInt(
                "UnlockedIsland",
                nextIsland
            );

            PlayerPrefs.Save();

            Debug.Log(
                $"Island {nextIsland} unlocked!"
            );
        }
    }

    public bool IsLessonComplete(LessonData lesson)
    {
        if (lesson == null)
            return false;

        // Quiz must ALWAYS be 100%
        int bestScore =
            PlayerPrefs.GetInt(
                $"Lesson{lesson.lessonID}BestScore",
                0
            );

        if (bestScore < 100)
            return false;

        // AR is required only when the lesson says so
        if (lesson.requiresARActivity)
        {
            bool arCompleted =
                PlayerPrefs.GetInt(
                    $"Lesson{lesson.lessonID}ARCompleted",
                    0
                ) == 1;

            if (!arCompleted)
                return false;
        }

        return true;
    }

    public bool AreAllCOCLessonsComplete(string cocID)
    {
        if (lessonDatabase == null)
        {
            Debug.LogError(
                "ProgressManager: LessonDatabase is not assigned."
            );

            return false;
        }

        LessonData[] cocLessons =
            lessonDatabase.GetLessonsForCOC(cocID);

        if (cocLessons.Length == 0)
        {
            Debug.LogWarning(
                $"No lessons found for {cocID}."
            );

            return false;
        }

        foreach (LessonData lesson in cocLessons)
        {
            if (!IsLessonComplete(lesson))
            {
                return false;
            }
        }

        return true;
    }

    public void CheckCOCCompletion(string cocID)
    {
        if (!AreAllCOCLessonsComplete(cocID))
        {
            Debug.Log(
                $"{cocID} is not yet complete."
            );

            return;
        }

        Debug.Log(
            $"All lessons in {cocID} are complete!"
        );

        string numberPart =
            cocID.Replace("COC_", "");

        if (!int.TryParse(
                numberPart,
                out int cocNumber))
        {
            Debug.LogError(
                $"Invalid COC ID: {cocID}"
            );

            return;
        }

        // COC_01 = Island 0
        // COC_02 = Island 1
        // COC_03 = Island 2

        int islandIndex = cocNumber - 1;

        UnlockNextIsland(islandIndex);
    }

    public void CompleteARActivity(LessonData lesson)
    {
        if (lesson == null)
        {
            Debug.LogError(
                "ProgressManager: Cannot complete AR activity. Lesson is null."
            );

            return;
        }

        PlayerPrefs.SetInt(
            $"Lesson{lesson.lessonID}ARCompleted",
            1
        );

        PlayerPrefs.Save();

        UpdateLessonMedal(lesson);

        Debug.Log(
            $"Lesson {lesson.lessonID}: AR Activity completed."
        );

        CheckCOCCompletion(lesson.cocID);
    }

    public void CompleteQuiz(
        LessonData lesson,
        int percentage)
    {
        if (lesson == null)
        {
            Debug.LogError(
                "ProgressManager: Cannot save quiz. Lesson is null."
            );

            return;
        }

        if (lesson.quizData != null &&
            percentage >= lesson.quizData.passingPercentage)
        {
            PlayerPrefs.SetInt(
                $"Lesson{lesson.lessonID}QuizCompleted",
                1
            );
        }

        int previousBestScore =
            PlayerPrefs.GetInt(
                $"Lesson{lesson.lessonID}BestScore",
                0
            );

        if (percentage > previousBestScore)
        {
            PlayerPrefs.SetInt(
                $"Lesson{lesson.lessonID}BestScore",
                percentage
            );
        }

        PlayerPrefs.Save();

        UpdateLessonMedal(lesson);

        Debug.Log(
            $"Lesson {lesson.lessonID}: Quiz score = {percentage}%"
        );

        CheckCOCCompletion(lesson.cocID);
    }

    public void UpdateLessonMedal(LessonData lesson)
    {
        if (lesson == null)
            return;

        int lessonID = lesson.lessonID;

        bool arCompleted =
            PlayerPrefs.GetInt(
                $"Lesson{lessonID}ARCompleted",
                0
            ) == 1;

        int bestScore =
            PlayerPrefs.GetInt(
                $"Lesson{lessonID}BestScore",
                0
            );

        bool quizPerfect =
            bestScore >= 100;

        Medal medal;

        // GOLD
        if (arCompleted && quizPerfect)
        {
            medal = Medal.Gold;
        }
        // SILVER
        else if (arCompleted || quizPerfect)
        {
            medal = Medal.Silver;
        }
        // BRONZE
        else
        {
            medal = Medal.Bronze;
        }

        Medal previousMedal =
            (Medal)PlayerPrefs.GetInt(
                $"Lesson{lessonID}Medal",
                0
            );

        if (medal > previousMedal)
        {
            PlayerPrefs.SetInt(
                $"Lesson{lessonID}Medal",
                (int)medal
            );

            PlayerPrefs.Save();

            Debug.Log(
                $"Lesson {lessonID}: Medal upgraded to {medal}."
            );
        }
    }

    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey("UnlockedIsland");

        PlayerPrefs.Save();

        Debug.Log(
            "Island progress reset."
        );
    }
}