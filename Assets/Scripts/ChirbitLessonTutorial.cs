using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChirbitLessonTutorial : MonoBehaviour
{
    public ChirbitTutorial tutorial;
    public Transform slideContainer;
    public RectTransform progressPanel;
    public GameObject completionPopup;
    public Button quizButton, arButton, lessonsButton;
    [TextArea(2,5)] public string readingMessage="Read the learning objectives first, then work through the lesson's text and images at your own pace.";
    [TextArea(2,5)] public string navigationMessage="Use the lesson's Next button to continue and Back to revisit a slide. Chirbit's buttons only advance this tutorial.";
    [TextArea(2,5)] public string progressMessage="This area shows your current subtopic and completed subtopics. Finish reading the slides to reach the lesson-complete options.";
    private const string IntroID="Lesson.Reading";
    private const string SkipKey="ARDENT.Chirbit.Lesson.SkipFollowup.v1";
    private void Awake(){if(tutorial!=null){tutorial.autoStart=false;tutorial.sandboxOnly=false;}}
    private void OnEnable(){if(tutorial!=null)tutorial.Completed+=Finished;}
    private void OnDisable(){if(tutorial!=null)tutorial.Completed-=Finished;}
    private static bool Seen(string id)=>PlayerPrefs.GetInt("ARDENT.Chirbit."+id+".v1",0)!=0;
    private void Finished(bool skipped){if(skipped){PlayerPrefs.SetInt(SkipKey,1);PlayerPrefs.Save();}}
    private void Update()
    {
        if(tutorial==null||!tutorial.IsReady||tutorial.IsShowing||LessonSession.CurrentLesson==null)return;
        if(completionPopup!=null&&completionPopup.activeInHierarchy)
        {
            if(PlayerPrefs.GetInt(SkipKey,0)!=0)return;
            bool quiz=Available(quizButton), ar=Available(arButton);
            string id="Lesson.Completion."+(quiz?"Quiz":"NoQuiz")+"."+(ar?"AR":"NoAR");
            if(Seen(id))return;
            var steps=new List<ChirbitTutorial.Step>();
            if(quiz)steps.Add(new ChirbitTutorial.Step{target=quizButton.transform as RectTransform,message="Ready to check your understanding? This button opens the quiz for this lesson."});
            if(ar)steps.Add(new ChirbitTutorial.Step{target=arButton.transform as RectTransform,message="This lesson also has an AR activity. Use this button to put what you've learned into practice."});
            if(Available(lessonsButton))steps.Add(new ChirbitTutorial.Step{target=lessonsButton.transform as RectTransform,message="Return to the lessons menu here when you're ready to explore another topic."});
            if(steps.Count>0)Show(id,steps.ToArray());
            return;
        }
        if(Seen(IntroID)||slideContainer==null)return;
        // Slides are generated at runtime: bind only the active slide's navigation.
        RectTransform next=null;
        foreach(var button in slideContainer.GetComponentsInChildren<Button>())
            if(button.gameObject.activeInHierarchy&&button.name.ToLowerInvariant().Contains("next")){next=button.transform as RectTransform;break;}
        if(next==null)return;
        RectTransform readingTarget=null;
        foreach(Transform slide in slideContainer)
            if(slide.gameObject.activeInHierarchy){readingTarget=slide.GetComponent<RectTransform>();break;}
        Show(IntroID,new[]{
            new ChirbitTutorial.Step{target=readingTarget,message=readingMessage},
            new ChirbitTutorial.Step{target=next,message=navigationMessage},
            new ChirbitTutorial.Step{target=progressPanel,message=progressMessage}
        });
    }
    private static bool Available(Button button)=>button!=null&&button.gameObject.activeInHierarchy&&button.interactable;
    private void Show(string id,ChirbitTutorial.Step[] steps){tutorial.tutorialID=id;tutorial.steps=steps;tutorial.Begin();}
}
