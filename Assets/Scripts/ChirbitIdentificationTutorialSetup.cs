#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ChirbitIdentificationTutorialSetup
{
    [MenuItem("ARDENT/Chirbit/Connect AR Identification Tutorial")]
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
        var progressText=Find<RectTransform>(canvas.transform,"ProgressText");
        var finish=Find<Button>(canvas.transform,"ViewCompletionButton");
        var inventoryPanel=Find<RectTransform>(canvas.transform,"InventoryPanel");
        var completionPanel=Find<RectTransform>(canvas.transform,"ARCompletionPanel");
        var placement=Object.FindFirstObjectByType<ARPlacementManager>();
        var progress=Object.FindFirstObjectByType<ARActivityProgress>();
        if(progressText==null||finish==null||inventoryPanel==null||completionPanel==null||placement==null||progress==null)
        {Debug.LogError("Missing ProgressText, ViewCompletionButton, InventoryPanel, ARCompletionPanel, ARPlacementManager, or ARActivityProgress.");return;}
        if(dialogue==null||top==null||bottom==null||left==null||right==null||message==null||step==null||back==null||next==null||skip==null||inventory==null||select==null||adjust==null)
        { Debug.LogError("Missing a tutorial UI element or AR Sandbox target. Check the expected hierarchy names.");return; }
        var tutorial=canvas.GetComponent<ChirbitTutorial>();
        if(tutorial==null)tutorial=Undo.AddComponent<ChirbitTutorial>(canvas.gameObject);
        Undo.RecordObject(tutorial,"Connect Chirbit tutorial");
        tutorial.tutorialID="ARIdentification.ToolIdentification.Intro"; tutorial.useCorners=true; tutorial.sandboxOnly=false; tutorial.autoStart=false;
        tutorial.root=root;tutorial.dialogue=dialogue;tutorial.topShade=top;tutorial.bottomShade=bottom;tutorial.leftShade=left;tutorial.rightShade=right;
        tutorial.highlight=Find<RectTransform>(root,"HighlightBorder");tutorial.messageText=message;tutorial.stepText=step;
        tutorial.backButton=back;tutorial.nextButton=next;tutorial.skipButton=skip;
        var guide=canvas.GetComponent<ChirbitIdentificationTutorial>();
        if(guide==null)guide=Undo.AddComponent<ChirbitIdentificationTutorial>(canvas.gameObject);
        Undo.RecordObject(guide,"Connect identification guide");
        guide.tutorial=tutorial;guide.placement=placement;guide.progress=progress;
        guide.selectionReadyButton=adjust;guide.completionButton=finish;
        guide.inventoryPanel=inventoryPanel.gameObject;guide.completionPanel=completionPanel.gameObject;
        if(guide.introduction==null||guide.introduction.Length==0)guide.introduction=new[]{
            new ChirbitTutorial.Step{message="In this activity, explore and identify the lesson's objects. Slowly move your phone to detect a flat surface."},
            new ChirbitTutorial.Step{target=inventory,message="After this introduction, open the inventory, choose an item, then tap a detected surface to place it."},
            new ChirbitTutorial.Step{target=select,message="Use Select mode and tap the placed object. Selecting it records an inspection and displays its information card when cards are enabled."}
        };
        if(guide.inspection==null||guide.inspection.Length==0)guide.inspection=new[]{
            new ChirbitTutorial.Step{target=progressText,message="Your selection has been counted here. After closing this message, read the object's information card. Re-selecting the same named object does not add another inspection."},
            new ChirbitTutorial.Step{target=inventory,message="Choose and inspect the remaining lesson items. When all required items have been inspected, the completion button becomes available."}
        };
        if(guide.completion==null||guide.completion.Length==0)guide.completion=new[]{
            new ChirbitTutorial.Step{target=finish.transform as RectTransform,message="All required items have been inspected and activity completion has been recorded. After this message closes, use this button to view the completion panel and continue."}
        };
        tutorial.steps=guide.introduction;EditorUtility.SetDirty(guide);
        Stretch(root);var overlay=Find<RectTransform>(root,"DimOverlay");if(overlay!=null)Stretch(overlay);
        Undo.RecordObject(root.gameObject,"Hide tutorial until first use");root.gameObject.SetActive(false);
        EditorUtility.SetDirty(tutorial);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeObject=tutorial;
        Debug.Log("Chirbit connected. Save ARScene, then enter Play Mode. Edit messages on the Canvas's Chirbit Identification Tutorial component. In Play Mode, use the component context menu: Replay This Tutorial.");
    }
    private static T Find<T>(Transform root,string name) where T:Component
    { foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==name)return t.GetComponent<T>();return null; }
    private static void Stretch(RectTransform rect)
    { Undo.RecordObject(rect,"Stretch tutorial overlay");rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero; }
}
#endif
