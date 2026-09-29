using System.Collections.Generic;
using UnityEngine;

// Uses the same local save system as lesson medals. Item IDs are prefab asset GUIDs.
public static class HardwareLibraryProgress
{
    private const string Prefix = "HardwareLibrary.Item.";

    public static bool IsUnlocked(ARObjectData item)
    {
        return HasIdentity(item) && PlayerPrefs.GetInt(Prefix + item.LibraryItemId, 0) == 1;
    }

    public static List<ARObjectData> UnlockGoldLesson(LessonData lesson)
    {
        var unlocked = new List<ARObjectData>();
        if (lesson == null || !lesson.hasARActivity || lesson.arActivity == null ||
            lesson.arActivity.availableObjects == null ||
            PlayerPrefs.GetInt($"Lesson{lesson.lessonID}Medal", 0) < (int)Medal.Gold)
            return unlocked;

        foreach (var item in lesson.arActivity.availableObjects)
        {
            if (!HasIdentity(item) || IsUnlocked(item)) continue;
            PlayerPrefs.SetInt(Prefix + item.LibraryItemId, 1);
            unlocked.Add(item);
        }
        if (unlocked.Count > 0) PlayerPrefs.Save();
        return unlocked;
    }

    // Idempotent migration for students who earned Gold before the library existed.
    public static void SynchronizeGoldLessons(LessonDatabase database)
    {
        if (database == null || database.lessons == null) return;
        foreach (var lesson in database.lessons) UnlockGoldLesson(lesson);
    }

    private static bool HasIdentity(ARObjectData item)
    {
        return item != null && item.prefab != null && !string.IsNullOrEmpty(item.LibraryItemId);
    }
}
