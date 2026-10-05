#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ChirbitLibraryTutorialSetup
{
    [MenuItem("ARDENT/Chirbit/Connect Hardware Library Tutorial")]
    public static void Connect()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().name != "Hardware_Library")
        { Debug.LogWarning("Open Hardware_Library outside Play Mode first."); return; }
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
        var categories=Find<RectTransform>(canvas.transform,"CategoryScrollView");
        var details=Find<RectTransform>(canvas.transform,"ItemDetailsPanel");
        var compatibility=Find<RectTransform>(canvas.transform,"CheckCompatibilityButton");
        var sandbox=Find<RectTransform>(canvas.transform,"ViewInARButton");
        if(dialogue==null||top==null||bottom==null||left==null||right==null||message==null||step==null||back==null||next==null||skip==null||categories==null||details==null||compatibility==null||sandbox==null)
        { Debug.LogError("Missing a tutorial UI element or Hardware Library target. Check the expected hierarchy names.");return; }
        var tutorial=canvas.GetComponent<ChirbitTutorial>();
        if(tutorial==null)tutorial=Undo.AddComponent<ChirbitTutorial>(canvas.gameObject);
        Undo.RecordObject(tutorial,"Connect Chirbit tutorial");
        tutorial.tutorialID="Hardware_Library";
        tutorial.root=root;tutorial.dialogue=dialogue;tutorial.topShade=top;tutorial.bottomShade=bottom;tutorial.leftShade=left;tutorial.rightShade=right;
        tutorial.highlight=Find<RectTransform>(root,"HighlightBorder");tutorial.messageText=message;tutorial.stepText=step;
        tutorial.backButton=back;tutorial.nextButton=next;tutorial.skipButton=skip;
        if(tutorial.steps==null||tutorial.steps.Length==0) tutorial.steps=new[]{
            new ChirbitTutorial.Step{target=categories,message="Welcome to the Hardware Library! Browse categories vertically and swipe each row sideways. Earn Gold in lessons to unlock their items."},
            new ChirbitTutorial.Step{target=details,message="After this tour, tap an item to see its details here. Unlocked items show a description and model preview; locked items show the lesson needed to unlock them."},
            new ChirbitTutorial.Step{target=compatibility,message="Check Compatibility compares your chosen motherboard, CPU, RAM, and GPU. All listed parts are available here, even without lesson unlocks or 3D models."},
            new ChirbitTutorial.Step{target=sandbox,message="Open AR Sandbox to place and explore your unlocked items. Unlock at least one item first. You're ready to try the library!"}
        };
        Stretch(root);var overlay=Find<RectTransform>(root,"DimOverlay");if(overlay!=null)Stretch(overlay);
        Undo.RecordObject(root.gameObject,"Hide tutorial until first use");root.gameObject.SetActive(false);
        EditorUtility.SetDirty(tutorial);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeObject=tutorial;
        Debug.Log("Chirbit connected. Save Hardware_Library, then enter Play Mode. Edit steps on the Canvas's Chirbit Tutorial component. In Play Mode, use the component context menu: Replay This Tutorial.");
    }
    private static T Find<T>(Transform root,string name) where T:Component
    { foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==name)return t.GetComponent<T>();return null; }
    private static void Stretch(RectTransform rect)
    { Undo.RecordObject(rect,"Stretch tutorial overlay");rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero; }
}
#endif
