using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(
    fileName = "LessonDatabase",
    menuName = "ARDENT/Lesson Database"
)]
public class LessonDatabase : ScriptableObject
{
    [Header("All Lessons")]
    public LessonData[] lessons;

    public LessonData GetLessonByID(int lessonID)
    {
        foreach (LessonData lesson in lessons)
        {
            if (lesson != null && lesson.lessonID == lessonID)
            {
                return lesson;
            }
        }

        return null;
    }

    public LessonData GetNextLesson(int currentLessonID)
    {
        LessonData nextLesson = null;

        foreach (LessonData lesson in lessons)
        {
            if (lesson == null)
                continue;

            if (lesson.lessonID > currentLessonID)
            {
                if (nextLesson == null ||
                    lesson.lessonID < nextLesson.lessonID)
                {
                    nextLesson = lesson;
                }
            }
        }

        return nextLesson;
    }

    // NEW: Get all lessons belonging to a specific COC
    public LessonData[] GetLessonsForCOC(string cocID)
    {
        List<LessonData> cocLessons =
            new List<LessonData>();

        foreach (LessonData lesson in lessons)
        {
            if (lesson == null)
                continue;

            if (lesson.cocID == cocID)
            {
                cocLessons.Add(lesson);
            }
        }

        return cocLessons.ToArray();
    }
}