using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ARSandboxSession
{
    public static ARActivityData Activity { get; private set; }
    public static ARObjectData SelectedItem { get; private set; }
    public static bool IsActive => Activity != null;
    private static readonly Dictionary<string, HardwareLibraryUI.Category> categories = new Dictionary<string, HardwareLibraryUI.Category>();
    public static HardwareLibraryUI.Category CategoryFor(ARObjectData item)
    {
        return categories.TryGetValue(item.LibraryItemId, out var category)
            ? category : HardwareLibraryUI.ResolveCategory(item.prefab, null);
    }

    public static List<ARObjectData> GetUnlockedItems(LessonDatabase database)
    {
        var result = new List<ARObjectData>();
        var ids = new HashSet<string>();
        if (database?.lessons == null) return result;
        foreach (var lesson in database.lessons)
        {
            if (lesson == null || !lesson.hasARActivity || lesson.arActivity == null) continue;
            foreach (var item in lesson.arActivity.LibraryObjects())
                if (item?.prefab != null && HardwareLibraryProgress.IsUnlocked(item) && ids.Add(item.LibraryItemId)) result.Add(item);
        }
        result.Sort((a,b) => string.Compare(Name(a),Name(b),System.StringComparison.OrdinalIgnoreCase));
        return result;
    }

    public static bool Begin(LessonDatabase database, ARObjectData selected, HardwareLibraryUI.CategoryOverride[] overrides = null)
    {
        HardwareLibraryProgress.SynchronizeGoldLessons(database);
        var items = GetUnlockedItems(database);
        if (items.Count == 0) return false;
        Reset();
        Activity = ScriptableObject.CreateInstance<ARActivityData>();
        Activity.activityID = "hardware-library-sandbox";
        Activity.activityTitle = "AR Sandbox";
        Activity.activityType = ARActivityType.Sandbox;
        Activity.availableObjects = items.ToArray();
        SandboxAssemblyRules.Build(database, items);
        foreach (var item in items) categories[item.LibraryItemId] = HardwareLibraryUI.ResolveCategory(item.prefab, overrides);
        Activity.allowMovement = Activity.allowRotation = Activity.allowScaling = true;
        if (selected != null) SelectedItem = items.Find(item => item.LibraryItemId == selected.LibraryItemId);
        SceneManager.sceneLoaded += OnSceneLoaded;
        return true;
    }

    private static string Name(ARObjectData item)
    {
        return HardwareProfileCatalog.DisplayName(item?.prefab);
    }
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single && scene.name != "ARScene") Reset();
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Activity != null) Object.Destroy(Activity);
        Activity = null; SelectedItem = null; categories.Clear();
        SandboxAssemblyRules.Clear();
    }
}
