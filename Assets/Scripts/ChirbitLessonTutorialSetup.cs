#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ChirbitLessonTutorialSetup
{
    [MenuItem("ARDENT/Chirbit/Connect Lesson Tutorial")]
    public static void Connect()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().name != "LessonScene")
        { Debug.LogWarning("Open LessonScene outside Play Mode first."); return; }
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
        var slides=Find<Transform>(canvas.transform,"SlideContainer");
        var progress=Find<RectTransform>(canvas.transform,"LessonProgressPanel");
        var popup=Find<RectTransform>(canvas.transform,"LessonCompletePopup");
        var quiz=popup!=null?Find<Button>(popup,"QuizButton"):null;
        var ar=popup!=null?Find<Button>(popup,"ARActivityButton"):null;
        var lessons=popup!=null?Find<Button>(popup,"LessonsMenuButton"):null;
        var missing=new System.Collections.Generic.List<string>();
        if(dialogue==null)missing.Add("DialoguePanel (RectTransform)");
        if(top==null)missing.Add("TopShade (Image)");
        if(bottom==null)missing.Add("BottomShade (Image)");
        if(left==null)missing.Add("LeftShade (Image)");
        if(right==null)missing.Add("RightShade (Image)");
        if(message==null)missing.Add("MessageText (TMP)");
        if(step==null)missing.Add("StepText (TMP)");
        if(back==null)missing.Add("BackButton (Button)");
        if(next==null)missing.Add("NextButton (Button)");
        if(skip==null)missing.Add("SkipButton (Button)");
        if(slides==null)missing.Add("SlideContainer (Transform)");
        if(progress==null)missing.Add("LessonProgressPanel (RectTransform)");
        if(popup==null)missing.Add("LessonCompletePopup (RectTransform)");
        if(lessons==null)missing.Add("LessonsMenuButton inside LessonCompletePopup");
        if(missing.Count>0){Debug.LogError("Chirbit lesson setup is missing: "+string.Join(", ",missing));return;}
        var tutorial=canvas.GetComponent<ChirbitTutorial>();
        if(tutorial==null)tutorial=Undo.AddComponent<ChirbitTutorial>(canvas.gameObject);
        Undo.RecordObject(tutorial,"Connect Chirbit tutorial");
        tutorial.tutorialID="Lesson.Reading"; tutorial.autoStart=false; tutorial.sandboxOnly=false; tutorial.useCorners=false;
        tutorial.root=root;tutorial.dialogue=dialogue;tutorial.topShade=top;tutorial.bottomShade=bottom;tutorial.leftShade=left;tutorial.rightShade=right;
        tutorial.highlight=Find<RectTransform>(root,"HighlightBorder");tutorial.messageText=message;tutorial.stepText=step;
        tutorial.backButton=back;tutorial.nextButton=next;tutorial.skipButton=skip;
        var guide=canvas.GetComponent<ChirbitLessonTutorial>();
        if(guide==null)guide=Undo.AddComponent<ChirbitLessonTutorial>(canvas.gameObject);
        Undo.RecordObject(guide,"Connect lesson tutorial");
        guide.tutorial=tutorial;guide.slideContainer=slides;guide.progressPanel=progress;
        guide.completionPopup=popup.gameObject;guide.quizButton=quiz;guide.arButton=ar;guide.lessonsButton=lessons;
        tutorial.steps=new[]{new ChirbitTutorial.Step{target=null,message=guide.readingMessage}};
        EditorUtility.SetDirty(guide);
        Stretch(root);var overlay=Find<RectTransform>(root,"DimOverlay");if(overlay!=null)Stretch(overlay);
        Undo.RecordObject(root.gameObject,"Hide tutorial until first use");root.gameObject.SetActive(false);
        EditorUtility.SetDirty(tutorial);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeObject=tutorial;
        Debug.Log("Chirbit connected. Save LessonScene, then enter Play Mode. Edit reading messages on the Canvas's Chirbit Lesson Tutorial component. In Play Mode, use the component context menu: Replay This Tutorial.");
    }
    private static T Find<T>(Transform root,string name) where T:Component
    { foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==name)return t.GetComponent<T>();return null; }
    private static void Stretch(RectTransform rect)
    { Undo.RecordObject(rect,"Stretch tutorial overlay");rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero; }
}
#endif
