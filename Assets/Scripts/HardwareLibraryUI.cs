using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HardwareLibraryUI : MonoBehaviour
{
    public LessonDatabase lessonDatabase;
    public ScrollRect categoryScrollView;
    public GameObject categoryRowPrefab;
    public GameObject itemTilePrefab;
    public TMP_Text collectionCountText;
    public Image itemIcon;
    public Button viewInARButton;
    private ARObjectData selectedSandboxItem;
    public RawImage modelPreview;
    private HardwareModelPreview preview;
    public TMP_Text itemNameText, itemDescriptionText, unlockRequirementText;

    [System.Serializable]
    public class CategoryOverride { public GameObject prefab; public Category category; }
    public enum Category { PCComponents, Peripherals, Tools, SafetyEquipment }
    [Tooltip("Optional overrides for items whose automatic category needs changing.")]
    public CategoryOverride[] categoryOverrides;

    private class Entry
    {
        public ARObjectData item;
        public Category category;
        public readonly List<LessonData> lessons = new List<LessonData>();
    }
    private readonly List<GameObject> generatedRows = new List<GameObject>();
    private static readonly string[] Titles = { "PC Components", "Peripherals", "Tools", "Safety Equipment" };

    private void Awake() { Screen.orientation = ScreenOrientation.Portrait; }
    private void Start()
    {
        if (viewInARButton == null) viewInARButton = FindNamed<Button>(transform, "ViewInARButton");
        if (viewInARButton != null) viewInARButton.onClick.AddListener(OpenSandbox);
        if (modelPreview == null) modelPreview = FindNamed<RawImage>(transform, "ModelPreview");
        if (modelPreview != null)
        {
            preview = modelPreview.GetComponent<HardwareModelPreview>();
            if (preview == null) preview = modelPreview.gameObject.AddComponent<HardwareModelPreview>();
            preview.Hide();
        }
        else Debug.LogWarning("Add a Raw Image named ModelPreview beneath LibraryPanel to show 3D models.", this);
        Refresh();
        var compatibility = GetComponent<HardwareCompatibilityPanel>();
        if (compatibility == null) compatibility = gameObject.AddComponent<HardwareCompatibilityPanel>();
        compatibility.Initialize(this);
    }

    public void Refresh()
    {
        if (lessonDatabase == null || lessonDatabase.lessons == null || categoryScrollView == null ||
            categoryScrollView.content == null || categoryRowPrefab == null || itemTilePrefab == null)
        { Debug.LogError("Hardware Library: run ARDENT > Hardware Library > Connect Current Scene.", this); return; }
        if (preview != null) preview.Hide();
        HardwareLibraryProgress.SynchronizeGoldLessons(lessonDatabase);
        foreach (var row in generatedRows) if (row != null) { row.SetActive(false); Destroy(row); }
        generatedRows.Clear();
        var entries = new List<Entry>();
        var lookup = new Dictionary<string, Entry>();
        foreach (var lesson in lessonDatabase.lessons)
        {
            if (lesson == null || !lesson.hasARActivity || lesson.arActivity == null) continue;
            foreach (var item in lesson.arActivity.LibraryObjects())
            {
                if (item?.prefab == null) continue;
                if (string.IsNullOrEmpty(item.LibraryItemId))
                { Debug.LogWarning("Library item is missing its ID: " + item.prefab.name); continue; }
                if (!lookup.TryGetValue(item.LibraryItemId, out var entry))
                {
                    entry = new Entry { item = item, category = GetCategory(item.prefab) };
                    lookup.Add(item.LibraryItemId, entry); entries.Add(entry);
                }
                if (!entry.lessons.Contains(lesson)) entry.lessons.Add(lesson);
            }
        }
        entries.Sort((a,b) => string.Compare(DisplayName(a.item), DisplayName(b.item), System.StringComparison.OrdinalIgnoreCase));
        int unlocked = 0;
        foreach (var entry in entries) if (HardwareLibraryProgress.IsUnlocked(entry.item)) unlocked++;
        selectedSandboxItem = null;
        if (viewInARButton != null) viewInARButton.interactable = unlocked > 0;
        if (collectionCountText != null) collectionCountText.text = $"{unlocked} / {entries.Count} unlocked";
        for (int category = 0; category < Titles.Length; category++)
        {
            var group = entries.FindAll(e => (int)e.category == category);
            if (group.Count == 0) continue;
            var row = Instantiate(categoryRowPrefab, categoryScrollView.content);
            generatedRows.Add(row); row.SetActive(true);
            var title = FindNamed<TMP_Text>(row.transform, "CategoryTitleText");
            var scroll = FindNamed<ScrollRect>(row.transform, "ItemScrollView");
            if (title != null) title.text = Titles[category];
            if (scroll == null || scroll.content == null || scroll.viewport == null)
            { Debug.LogError("CategoryRow needs its ItemScrollView content and viewport references.", row); continue; }
            foreach (Transform child in scroll.content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            var drag = scroll.viewport.gameObject.GetComponent<LibraryRowDrag>();
            if (drag == null) drag = scroll.viewport.gameObject.AddComponent<LibraryRowDrag>();
            drag.horizontal = scroll; drag.vertical = categoryScrollView;
            foreach (var entry in group)
            {
                var tile = Instantiate(itemTilePrefab, scroll.content); tile.SetActive(true);
                var icon = FindNamed<Image>(tile.transform, "ItemIcon");
                var name = FindNamed<TMP_Text>(tile.transform, "ItemNameText");
                var overlay = FindNamed<Transform>(tile.transform, "LockOverlay");
                bool owned = HardwareLibraryProgress.IsUnlocked(entry.item);
                if (icon != null) { icon.sprite = entry.item.inventoryImage; icon.preserveAspect = true; icon.color = owned ? Color.white : Color.black; }
                if (name != null) name.text = DisplayName(entry.item);
                if (overlay != null) overlay.gameObject.SetActive(!owned);
                // The root Button owns clicks; its children should not intercept them.
                foreach (var graphic in tile.GetComponentsInChildren<Graphic>(true))
                    if (graphic.gameObject != tile) graphic.raycastTarget = false;
                var button = tile.GetComponent<Button>();
                if (button != null) { var selected = entry; button.onClick.AddListener(() => Select(selected)); }
            }
        }
        if (itemIcon != null) itemIcon.gameObject.SetActive(false);
        if (itemNameText != null) itemNameText.text = "Select an item";
        if (itemDescriptionText != null) itemDescriptionText.text = "Browse a category to learn about its items.";
        if (unlockRequirementText != null) unlockRequirementText.text = "";
        Canvas.ForceUpdateCanvases();
        categoryScrollView.verticalNormalizedPosition = 1;
    }

    private void Select(Entry entry)
    {
        selectedSandboxItem = entry.item;
        bool owned = HardwareLibraryProgress.IsUnlocked(entry.item);
        bool showingModel = false;
        if (preview != null)
        {
            if (owned) showingModel = preview.Show(entry.item.prefab);
            else preview.Hide();
        }
        if (itemIcon != null)
        {
            itemIcon.gameObject.SetActive(!showingModel); itemIcon.sprite = entry.item.inventoryImage;
            itemIcon.preserveAspect = true; itemIcon.color = owned ? Color.white : Color.black;
        }
        if (itemNameText != null) itemNameText.text = DisplayName(entry.item);
        var info = entry.item.prefab.GetComponent<ARObjectInfo>();
        if (itemDescriptionText != null) itemDescriptionText.text = owned
            ? (info != null && !string.IsNullOrWhiteSpace(info.information) ? info.information : "No description available yet.")
            : "Complete a lesson below with a Gold medal to unlock this item.";
        if (owned && itemDescriptionText != null)
        {
            var profile = HardwareProfileCatalog.ForPrefab(entry.item.prefab);
            if (profile != null)
                itemDescriptionText.text = profile.SpecificationSummary() + "\n\n" + itemDescriptionText.text;
        }
        var lessons = new List<string>();
        foreach (var lesson in entry.lessons) lessons.Add(lesson.lessonTitle);
        if (unlockRequirementText != null) unlockRequirementText.text = owned ? "Unlocked" : "Earn Gold in any of these lessons:\n" + string.Join("\n", lessons);
        var descriptionScroll = itemDescriptionText != null ? itemDescriptionText.GetComponentInParent<ScrollRect>() : null;
        if (descriptionScroll != null) { Canvas.ForceUpdateCanvases(); descriptionScroll.verticalNormalizedPosition = 1; }
    }

    private Category GetCategory(GameObject prefab) => ResolveCategory(prefab, categoryOverrides);

    public static Category ResolveCategory(GameObject prefab, CategoryOverride[] categoryOverrides)
    {
        if (categoryOverrides != null) foreach (var entry in categoryOverrides)
            if (entry != null && entry.prefab == prefab) return entry.category;
        string name = prefab.name.ToLowerInvariant();
        if (name == "gloves" || name == "goggles" || name == "dustmask" || name == "rubbersole" || name.StartsWith("anti-static")) return Category.SafetyEquipment;
        if (name == "keyboard" || name == "mouse" || name == "lcd" || name == "printer" || name == "speakers" || name == "modem" || name == "flashdrive") return Category.Peripherals;
        if (name.StartsWith("cpu") || name.StartsWith("mb_") || name.StartsWith("ram_") || name.StartsWith("gpu_") || name.StartsWith("psu_") || name.StartsWith("pc_case") || name == "hdd" || name == "dvd" || name == "floppy" || name == "lancard") return Category.PCComponents;
        return Category.Tools;
    }

    private static string DisplayName(ARObjectData item)
    {
        return HardwareProfileCatalog.DisplayName(item?.prefab);
    }
    public static T FindNamed<T>(Transform root, string name) where T : Component
    {
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child.GetComponent<T>();
        return null;
    }
    public void OpenSandbox()
    {
        if (!ARSandboxSession.Begin(lessonDatabase, selectedSandboxItem, categoryOverrides))
        {
            if (unlockRequirementText != null) unlockRequirementText.text = "Earn Gold in a lesson to unlock items for the AR Sandbox.";
            return;
        }
        ArdentMotion.LoadScene("ARScene");
    }
    private void OnDestroy()
    {
        if (viewInARButton != null) viewInARButton.onClick.RemoveListener(OpenSandbox);
    }
    public void BackToMenu() { ArdentMotion.LoadScene("MainMenu"); }
}
