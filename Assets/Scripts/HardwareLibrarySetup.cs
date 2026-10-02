#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class HardwareLibrarySetup
{
    [MenuItem("ARDENT/Hardware Library/Connect Current Scene")]
    public static void Connect()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "Hardware_Library") { Debug.LogError("Open Hardware_Library before running this command."); return; }
        Transform root = null;
        foreach (var obj in scene.GetRootGameObjects())
        {
            var found = HardwareLibraryUI.FindNamed<Transform>(obj.transform, "LibraryPanel");
            if (found != null) { root = found; break; }
        }
        if (root == null) { Debug.LogError("LibraryPanel was not found."); return; }
        var scroll = HardwareLibraryUI.FindNamed<ScrollRect>(root, "CategoryScrollView");
        var details = HardwareLibraryUI.FindNamed<RectTransform>(root, "ItemDetailsPanel");
        var database = AssetDatabase.LoadAssetAtPath<LessonDatabase>("Assets/Scripts/LessonDatabase.asset");
        var row = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/prefabs/CategoryRow.prefab");
        var tile = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/prefabs/ItemTile.prefab");
        if (scroll == null || scroll.content == null || details == null || database == null || row == null || tile == null)
        { Debug.LogError("Library setup needs the category Scroll View, details panel, lesson database and both prefabs."); return; }
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Connect Hardware Library");
        var ui = root.GetComponent<HardwareLibraryUI>();
        if (ui == null) ui = Undo.AddComponent<HardwareLibraryUI>(root.gameObject);
        Undo.RecordObject(ui, "Assign library references");
        ui.lessonDatabase = database; ui.categoryScrollView = scroll;
        ui.categoryRowPrefab = row; ui.itemTilePrefab = tile;
        ui.collectionCountText = HardwareLibraryUI.FindNamed<TMP_Text>(root, "CollectionCountText");
        // Preserve the hand-authored sample row, but hide it so only populated rows display.
        foreach (Transform child in scroll.content)
            if (child.name == "CategoryRow") { Undo.RecordObject(child.gameObject,"Hide sample row"); child.gameObject.SetActive(false); }
        ui.itemIcon = HardwareLibraryUI.FindNamed<Image>(details, "ItemIcon");
        if (ui.itemIcon == null)
        {
            var rect = CreateRect("ItemIcon", details, new Vector2(.38f,.73f), new Vector2(.62f,.98f));
            ui.itemIcon = Undo.AddComponent<Image>(rect.gameObject);
            ui.itemIcon.preserveAspect = true; ui.itemIcon.raycastTarget = false;
        }
        ui.itemNameText = HardwareLibraryUI.FindNamed<TMP_Text>(details, "ItemNameText");
        if (ui.itemNameText == null)
        {
            var rect = CreateRect("ItemNameText", details, new Vector2(.05f,.60f), new Vector2(.95f,.73f));
            ui.itemNameText = MakeText(rect, "Select an item", 32);
        }
        var detailScroll = HardwareLibraryUI.FindNamed<ScrollRect>(details, "DescriptionScrollView");
        if (detailScroll == null)
        {
            var rect = CreateRect("DescriptionScrollView", details, new Vector2(.05f,.05f), new Vector2(.95f,.60f));
            detailScroll = Undo.AddComponent<ScrollRect>(rect.gameObject);
            detailScroll.horizontal = false; detailScroll.vertical = true;
            detailScroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = CreateRect("Viewport", rect, Vector2.zero, Vector2.one);
            Undo.AddComponent<RectMask2D>(viewport.gameObject);
            var background = Undo.AddComponent<Image>(viewport.gameObject);
            background.color = new Color(0,0,0,0);
            var content = CreateRect("Content", viewport, new Vector2(0,1),Vector2.one);
            content.pivot = new Vector2(.5f,1);
            var layout = Undo.AddComponent<VerticalLayoutGroup>(content.gameObject);
            layout.spacing = 16; layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            var fitter = Undo.AddComponent<ContentSizeFitter>(content.gameObject);
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            detailScroll.content = content; detailScroll.viewport = viewport;
        }
        ui.itemDescriptionText = HardwareLibraryUI.FindNamed<TMP_Text>(details, "ItemDescriptionText");
        if (ui.itemDescriptionText == null) ui.itemDescriptionText = MakeText(CreateRect("ItemDescriptionText",detailScroll.content,Vector2.zero,Vector2.one),"Browse a category to learn about its items.",26);
        ui.unlockRequirementText = HardwareLibraryUI.FindNamed<TMP_Text>(details, "UnlockRequirementText");
        if (ui.unlockRequirementText == null) ui.unlockRequirementText = MakeText(CreateRect("UnlockRequirementText",detailScroll.content,Vector2.zero,Vector2.one),"",24);
        ui.itemDescriptionText.alignment = ui.unlockRequirementText.alignment = TextAlignmentOptions.TopLeft;
        var back = HardwareLibraryUI.FindNamed<Button>(root, "BackButton");
        if (back == null)
        {
            var header = HardwareLibraryUI.FindNamed<Transform>(root, "Header");
            if (header != null)
            {
                var rect = CreateRect("BackButton",header,new Vector2(0,.1f),new Vector2(.15f,.9f));
                var image = Undo.AddComponent<Image>(rect.gameObject); image.color = new Color(.04f,.36f,.44f,1);
                back = Undo.AddComponent<Button>(rect.gameObject);
                MakeText(CreateRect("Label",rect,Vector2.zero,Vector2.one),"Back",24);
            }
        }
        if (back != null && back.onClick.GetPersistentEventCount() == 0)
        { Undo.RecordObject(back,"Connect Back button"); UnityEventTools.AddPersistentListener(back.onClick, ui.BackToMenu); }
        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(group);
        Selection.activeGameObject = root.gameObject;
        Debug.Log("Hardware Library connected. Save the scene and enter Play Mode. All new details fields remain editable in the scene.", ui);
    }

    private static RectTransform CreateRect(string name,Transform parent,Vector2 min,Vector2 max)
    {
        var obj = new GameObject(name, typeof(RectTransform)); Undo.RegisterCreatedObjectUndo(obj,"Create library UI");
        var rect = obj.GetComponent<RectTransform>(); rect.SetParent(parent,false);
        rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
        return rect;
    }
    private static TMP_Text MakeText(RectTransform rect,string text,float size)
    {
        var label = Undo.AddComponent<TextMeshProUGUI>(rect.gameObject);
        label.text=text;label.fontSize=size;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
        return label;
    }
}
#endif
