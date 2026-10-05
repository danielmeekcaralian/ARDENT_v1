#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ChirbitTutorialSetup
{
    [MenuItem("ARDENT/Chirbit/Connect Main Menu Tutorial")]
    public static void Connect()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().name != "MainMenu")
        { Debug.LogWarning("Open MainMenu outside Play Mode first."); return; }
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
        var learn=Find<RectTransform>(canvas.transform,"startLearningButton");
        var library=Find<RectTransform>(canvas.transform,"hardwareLibraryButton");
        var settings=Find<RectTransform>(canvas.transform,"Settings_Icon");
        if(dialogue==null||top==null||bottom==null||left==null||right==null||message==null||step==null||back==null||next==null||skip==null||learn==null||library==null||settings==null)
        { Debug.LogError("Missing a tutorial UI element or Main Menu target. Check the expected hierarchy names.");return; }
        var tutorial=canvas.GetComponent<ChirbitTutorial>();
        if(tutorial==null)tutorial=Undo.AddComponent<ChirbitTutorial>(canvas.gameObject);
        Undo.RecordObject(tutorial,"Connect Chirbit tutorial");
        tutorial.root=root;tutorial.dialogue=dialogue;tutorial.topShade=top;tutorial.bottomShade=bottom;tutorial.leftShade=left;tutorial.rightShade=right;
        tutorial.highlight=Find<RectTransform>(root,"HighlightBorder");tutorial.messageText=message;tutorial.stepText=step;
        tutorial.backButton=back;tutorial.nextButton=next;tutorial.skipButton=skip;
        if(tutorial.steps==null||tutorial.steps.Length==0) tutorial.steps=new[]{
            new ChirbitTutorial.Step{target=learn,message="Hi! I'm Chirbit, your guide! Start Learning opens your lessons. Study each topic, then practice what you learn."},
            new ChirbitTutorial.Step{target=library,message="Visit the Hardware Library to explore your unlocked items, open the AR Sandbox, and check PC component compatibility. Compatibility entries are available right away!"},
            new ChirbitTutorial.Step{target=settings,message="Open Settings to adjust the app to your preferences. You're ready to explore!"}
        };
        Stretch(root);var overlay=Find<RectTransform>(root,"DimOverlay");if(overlay!=null)Stretch(overlay);
        Undo.RecordObject(root.gameObject,"Hide tutorial until first use");root.gameObject.SetActive(false);
        EditorUtility.SetDirty(tutorial);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeObject=tutorial;
        Debug.Log("Chirbit connected. Save MainMenu, then enter Play Mode. Edit steps on the Canvas's Chirbit Tutorial component. Use its Begin method or the Settings replay button to replay.");
    }
    private static T Find<T>(Transform root,string name) where T:Component
    { foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==name)return t.GetComponent<T>();return null; }
    private static void Stretch(RectTransform rect)
    { Undo.RecordObject(rect,"Stretch tutorial overlay");rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero; }
}
#endif
