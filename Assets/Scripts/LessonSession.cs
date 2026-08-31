using UnityEngine;

public static class LessonSession
{
    public static LessonData CurrentLesson { get; private set; }

    public static string CurrentCOC { get; private set; }

    public static void SetCOC(string cocSceneName)
    {
        CurrentCOC = cocSceneName;
    }

    public static void SetLesson(LessonData lesson)
    {
        CurrentLesson = lesson;
    }

    public static void ClearSession()
    {
        CurrentLesson = null;
        CurrentCOC = null;
    }
}