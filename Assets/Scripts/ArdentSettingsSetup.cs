#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ArdentSettingsSetup
{
    [MenuItem("ARDENT/Settings/Build Controls In UI Scene")]
    public static void Build()
    {
        var scene = SceneManager.GetSceneByName("UI_Scene");
        if (!scene.isLoaded) { Debug.LogError("Open UI_Scene first."); return; }
        Transform panel = null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == "settingsPanel") panel=t;
        if(panel==null){Debug.LogError("settingsPanel was not found.");return;}
        if(panel.GetComponent<ArdentSettingsPanel>()!=null)
        {Selection.activeGameObject=panel.gameObject;Debug.Log("Settings controls already connected; edit them in the scene.");return;}
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Create AR settings controls");
        var controller=Undo.AddComponent<ArdentSettingsPanel>(panel.gameObject);
        var scrollRect=Rect("SettingsScrollView",panel,new Vector2(.08f,.10f),new Vector2(.92f,.78f));
        var scroll=Undo.AddComponent<ScrollRect>(scrollRect.gameObject);
        scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
        var viewport=Rect("Viewport",scrollRect,Vector2.zero,Vector2.one);
        var image=Undo.AddComponent<Image>(viewport.gameObject);image.color=new Color(0,0,0,0);
        Undo.AddComponent<RectMask2D>(viewport.gameObject);
        var content=Rect("Content",viewport,new Vector2(0,1),Vector2.one);content.pivot=new Vector2(.5f,1);
        var layout=Undo.AddComponent<VerticalLayoutGroup>(content.gameObject);
        layout.spacing=18;layout.padding=new RectOffset(12,12,12,12);
        layout.childControlHeight=layout.childControlWidth=true;
        layout.childForceExpandHeight=false;layout.childForceExpandWidth=true;
        var fit=Undo.AddComponent<ContentSizeFitter>(content.gameObject);fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        scroll.content=content;scroll.viewport=viewport;
        AddSlider(content,"RotationSensitivity","Rotation sensitivity",.25f,2,1,out controller.rotationSlider,out controller.rotationValue);
        AddSlider(content,"ZoomSensitivity","Zoom sensitivity",.25f,2,1,out controller.zoomSlider,out controller.zoomValue);
        var toggleRow=Row(content,"ShowCards",80);
        var toggleObj=DefaultControls.CreateToggle(new DefaultControls.Resources());
        Undo.RegisterCreatedObjectUndo(toggleObj,"Create settings toggle");toggleObj.name="ShowInfoCardsToggle";
        var tr=toggleObj.GetComponent<RectTransform>();tr.SetParent(toggleRow,false);
        tr.anchorMin=new Vector2(0,.15f);tr.anchorMax=new Vector2(1,.85f);tr.offsetMin=tr.offsetMax=Vector2.zero;
        controller.showCardsToggle=toggleObj.GetComponent<Toggle>();controller.showCardsToggle.isOn=true;
        var old=toggleObj.GetComponentInChildren<Text>();if(old!=null)Undo.DestroyObjectImmediate(old.gameObject);
        var background=toggleObj.transform.Find("Background") as RectTransform;
        if(background!=null){background.anchorMin=background.anchorMax=new Vector2(0,.5f);background.pivot=new Vector2(0,.5f);background.anchoredPosition=Vector2.zero;background.sizeDelta=new Vector2(40,40);}
        var label=Label(tr,"Label","Show info cards",new Vector2(.12f,0),Vector2.one);label.alignment=TextAlignmentOptions.MidlineLeft;
        AddSlider(content,"CardSpacing","Info-card spacing",.01f,.30f,.05f,out controller.cardGapSlider,out controller.cardGapValue);
        var resetRow=Row(content,"Defaults",80);
        var button=Rect("RestoreDefaultsButton",resetRow,Vector2.zero,Vector2.one);
        var bg=Undo.AddComponent<Image>(button.gameObject);bg.color=new Color(.04f,.36f,.44f,1);
        controller.restoreDefaultsButton=Undo.AddComponent<Button>(button.gameObject);
        Label(button,"Label","Restore default settings",Vector2.zero,Vector2.one);
        var note=Row(content,"SaveNote",90);
        var text=Label(note,"Label","Changes save automatically. Lesson progress is not affected.",Vector2.zero,Vector2.one);text.fontSize=22;
        controller.rotationValue.text="1.00x";controller.zoomValue.text="1.00x";controller.cardGapValue.text="5 cm";
        EditorUtility.SetDirty(controller);EditorSceneManager.MarkSceneDirty(scene);Undo.CollapseUndoOperations(group);
        Selection.activeGameObject=panel.gameObject;
        Debug.Log("Settings connected. Save UI_Scene, then test from MainMenu. Existing title, background and Close button were preserved.",controller);
    }
    private static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max)
    {
        var go=new GameObject(name,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(go,"Create settings UI");
        var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;return r;
    }
    private static RectTransform Row(Transform parent,string name,float height)
    {var r=Rect(name,parent,Vector2.zero,Vector2.one);Undo.AddComponent<LayoutElement>(r.gameObject).preferredHeight=height;return r;}
    private static TMP_Text Label(Transform parent,string name,string text,Vector2 min,Vector2 max)
    {
        var t=Undo.AddComponent<TextMeshProUGUI>(Rect(name,parent,min,max).gameObject);
        t.text=text;t.fontSize=28;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;
    }
    private static void AddSlider(Transform parent,string name,string title,float min,float max,float value,out Slider slider,out TMP_Text valueText)
    {
        var row=Row(parent,name,120);
        Label(row,"Title",title,new Vector2(0,.55f),new Vector2(.78f,1)).alignment=TextAlignmentOptions.MidlineLeft;
        valueText=Label(row,"Value","",new Vector2(.78f,.55f),Vector2.one);
        var go=DefaultControls.CreateSlider(new DefaultControls.Resources());Undo.RegisterCreatedObjectUndo(go,"Create settings slider");go.name=name+"Slider";
        var r=go.GetComponent<RectTransform>();r.SetParent(row,false);r.anchorMin=new Vector2(.02f,.05f);r.anchorMax=new Vector2(.98f,.45f);r.offsetMin=r.offsetMax=Vector2.zero;
        slider=go.GetComponent<Slider>();slider.minValue=min;slider.maxValue=max;slider.value=value;
    }
}
#endif
