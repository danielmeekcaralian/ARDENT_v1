#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SandboxInventorySetup
{
    [MenuItem("ARDENT/AR Sandbox/Create Expanded Inventory")]
    public static void Build()
    {
        var scene = SceneManager.GetSceneByName("ARScene");
        if (!scene.isLoaded) { Debug.LogError("Open ARScene first."); return; }
        Transform original = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var existing = HardwareLibraryUI.FindNamed<SandboxInventoryPanel>(root.transform,"SandboxInventoryPanel");
            if (existing != null) { Selection.activeGameObject = existing.gameObject; Debug.Log("Expanded inventory already exists; edit it in the scene."); return; }
            var found = HardwareLibraryUI.FindNamed<Transform>(root.transform,"InventoryPanel");
            if (found != null) original = found;
        }
        var row = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/prefabs/CategoryRow.prefab");
        var tile = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/prefabs/ItemTile.prefab");
        if (original == null || row == null || tile == null) { Debug.LogError("InventoryPanel or library prefabs are missing."); return; }
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Create sandbox inventory");
        var panel = Rect("SandboxInventoryPanel",original.parent,new Vector2(.08f,.08f),new Vector2(.92f,.92f));
        var image = Undo.AddComponent<Image>(panel.gameObject);
        var old = original.GetComponent<Image>();
        if (old != null) { image.sprite = old.sprite; image.type = old.type; image.color = old.color; }
        else image.color = new Color(.035f,.075f,.11f,.97f);
        var view = Undo.AddComponent<SandboxInventoryPanel>(panel.gameObject);
        view.categoryRowPrefab = row; view.itemTilePrefab = tile;
        Text(panel,"Title","Unlocked inventory",new Vector2(.03f,.89f),new Vector2(.60f,.98f),34);
        view.countText = Text(panel,"ItemCount","",new Vector2(.60f,.89f),new Vector2(.82f,.98f),24);
        var close = Rect("CloseButton",panel,new Vector2(.84f,.90f),new Vector2(.97f,.98f));
        var bg=Undo.AddComponent<Image>(close.gameObject); bg.color=new Color(.04f,.36f,.44f,1);
        view.closeButton=Undo.AddComponent<Button>(close.gameObject);
        Text(close,"Label","Close",Vector2.zero,Vector2.one,26);
        var scrollRect=Rect("CategoryScrollView",panel,new Vector2(.02f,.03f),new Vector2(.98f,.86f));
        view.categoryScroll=Undo.AddComponent<ScrollRect>(scrollRect.gameObject);
        view.categoryScroll.horizontal=false;view.categoryScroll.vertical=true;view.categoryScroll.movementType=ScrollRect.MovementType.Clamped;
        var viewport=Rect("Viewport",scrollRect,Vector2.zero,Vector2.one);
        Undo.AddComponent<Image>(viewport.gameObject).color=new Color(0,0,0,0);
        Undo.AddComponent<RectMask2D>(viewport.gameObject);
        var content=Rect("Content",viewport,new Vector2(0,1),Vector2.one);content.pivot=new Vector2(.5f,1);
        var layout=Undo.AddComponent<VerticalLayoutGroup>(content.gameObject);
        layout.spacing=24;layout.padding=new RectOffset(0,0,0,36);
        layout.childControlHeight=layout.childControlWidth=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;
        Undo.AddComponent<ContentSizeFitter>(content.gameObject).verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        view.categoryScroll.content=content;view.categoryScroll.viewport=viewport;
        panel.gameObject.SetActive(false);
        EditorUtility.SetDirty(view);EditorSceneManager.MarkSceneDirty(scene);Undo.CollapseUndoOperations(group);
        Selection.activeGameObject=panel.gameObject;
        Debug.Log("Expanded sandbox inventory created. Save ARScene, then open the sandbox from Hardware Library. Lesson inventory is unchanged.",view);
    }
    private static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max)
    {
        var obj=new GameObject(name,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(obj,"Create inventory UI");
        var rect=obj.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;
    }
    private static TMP_Text Text(Transform parent,string name,string text,Vector2 min,Vector2 max,int size)
    {
        var label=Undo.AddComponent<TextMeshProUGUI>(Rect(name,parent,min,max).gameObject);
        label.text=text;label.fontSize=size;label.alignment=TextAlignmentOptions.MidlineLeft;label.raycastTarget=false;return label;
    }
}
#endif
