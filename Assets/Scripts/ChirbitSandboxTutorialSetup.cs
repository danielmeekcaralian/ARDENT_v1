#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ChirbitSandboxTutorialSetup
{
    [MenuItem("ARDENT/Chirbit/Connect AR Sandbox Tutorial")]
    public static void Connect()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().name != "ARScene")
        { Debug.LogWarning("Open ARScene outside Play Mode first."); return; }
        RectTransform root = null;
        foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
        { root = Find<RectTransform>(go.transform,"ChirbitTutorialRoot"); if(root != null) break; }
        if(root == null || root.parent == null) { Debug.LogError("ChirbitTutorialRoot was not found beneath a Canvas."); return; }
        var canvas = root.GetComponentInParent<Canvas>();
        if(canvas == null) { Debug.LogError("Place ChirbitTutorialRoot under your Canvas."); return; }
        var dialogue=Find<RectTransform>(root,"DialoguePanel");
        var top=Find<Image>(root,"TopShade"); var bottom=Find<Image>(root,"BottomShade");
        var left=Find<Image>(root,"LeftShade"); var right=Find<Image>(root,"RightShade");
        var message=Find<TMP_Text>(root,"MessageText"); var step=Find<TMP_Text>(root,"StepText");
        var back=Find<Button>(root,"BackButton");var next=Find<Button>(root,"NextButton");var skip=Find<Button>(root,"SkipButton");
        var inventory=Find<RectTransform>(canvas.transform,"InventoryButton");
        var select=Find<RectTransform>(canvas.transform,"EditButton");
        var adjust=Find<Button>(canvas.transform,"AdjustButton");
        if(dialogue==null||top==null||bottom==null||left==null||right==null||message==null||step==null||back==null||next==null||skip==null||inventory==null||select==null||adjust==null)
        { Debug.LogError("Missing a tutorial UI element or AR Sandbox target. Check the expected hierarchy names.");return; }
        var tutorial=canvas.GetComponent<ChirbitTutorial>();
        if(tutorial==null)tutorial=Undo.AddComponent<ChirbitTutorial>(canvas.gameObject);
        Undo.RecordObject(tutorial,"Connect Chirbit tutorial");
        tutorial.tutorialID="ARSandbox.Intro"; tutorial.useCorners=true; tutorial.sandboxOnly=true; tutorial.autoStart=false;
        tutorial.root=root;tutorial.dialogue=dialogue;tutorial.topShade=top;tutorial.bottomShade=bottom;tutorial.leftShade=left;tutorial.rightShade=right;
        tutorial.highlight=Find<RectTransform>(root,"HighlightBorder");tutorial.messageText=message;tutorial.stepText=step;
        tutorial.backButton=back;tutorial.nextButton=next;tutorial.skipButton=skip;
        var guide=canvas.GetComponent<ChirbitSandboxTutorial>();
        if(guide==null)guide=Undo.AddComponent<ChirbitSandboxTutorial>(canvas.gameObject);
        Undo.RecordObject(guide,"Connect Sandbox guide");
        guide.tutorial=tutorial;guide.selectionReadyButton=adjust;
        if(guide.introduction==null||guide.introduction.Length==0)guide.introduction=new[]{
            new ChirbitTutorial.Step{message="Welcome to AR Sandbox! Slowly move your phone to scan a flat surface. After this introduction, choose an item, then tap a detected surface to place it."},
            new ChirbitTutorial.Step{target=inventory,message="Open the inventory to choose an unlocked item. When this message closes, place one on a surface and select it. I'll explain the controls next!"}
        };
        if(guide.controls==null||guide.controls.Length==0)guide.controls=new[]{
            new ChirbitTutorial.Step{target=select,message="Use Select mode to choose a model. Drag to move it, pinch to resize it, and twist two fingers to rotate it."},
            new ChirbitTutorial.Step{target=adjust.transform as RectTransform,message="Adjust opens the raise, lower, and tilt controls for the selected model. Use them to reach positions that are difficult with dragging alone."},
            new ChirbitTutorial.Step{target=select,message="To assemble supported PC parts, move a compatible component near its matching target until it snaps. Not every inventory item has an assembly target. Try it after this tour!"}
        };
        tutorial.steps=guide.introduction;
        EditorUtility.SetDirty(guide);
        Stretch(root);var overlay=Find<RectTransform>(root,"DimOverlay");if(overlay!=null)Stretch(overlay);
        Undo.RecordObject(root.gameObject,"Hide tutorial until first use");root.gameObject.SetActive(false);
        EditorUtility.SetDirty(tutorial);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeObject=tutorial;
        Debug.Log("Chirbit connected. Save ARScene, then enter Play Mode. Edit introduction and controls on the Canvas's Chirbit Sandbox Tutorial component. In Play Mode, use the component context menu: Replay This Tutorial.");
    }
    private static T Find<T>(Transform root,string name) where T:Component
    { foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==name)return t.GetComponent<T>();return null; }
    private static void Stretch(RectTransform rect)
    { Undo.RecordObject(rect,"Stretch tutorial overlay");rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero; }
}
#endif
