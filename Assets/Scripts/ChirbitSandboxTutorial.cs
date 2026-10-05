using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ChirbitSandboxTutorial : MonoBehaviour
{
    public ChirbitTutorial tutorial;
    public Button selectionReadyButton;
    public ChirbitTutorial.Step[] introduction, controls;
    private const string IntroID="ARSandbox.Intro", ControlsID="ARSandbox.Controls";
    private void Awake() { if(tutorial!=null) {tutorial.autoStart=false;tutorial.sandboxOnly=true;} }
    private void OnEnable() { if(tutorial!=null)tutorial.Completed+=OnCompleted; }
    private void OnDisable() { if(tutorial!=null)tutorial.Completed-=OnCompleted; }
    private static bool Seen(string id)=>PlayerPrefs.GetInt("ARDENT.Chirbit."+id+".v1",0)!=0;
    private void Update()
    {
        if(!ARSandboxSession.IsActive||tutorial==null||!tutorial.IsReady||tutorial.IsShowing||UIManager.HasOpenPanel||SandboxInventoryPanel.IsOpen||ARCheckpointSession.BlocksInput)return;
        // Never interrupt an active placement or drag gesture.
        if(Mouse.current!=null&&Mouse.current.leftButton.isPressed)return;
        if(Touchscreen.current!=null)foreach(var touch in Touchscreen.current.touches)if(touch.press.isPressed)return;
        if(!Seen(IntroID))Show(IntroID,introduction);
        else if(!Seen(ControlsID)&&selectionReadyButton!=null&&selectionReadyButton.gameObject.activeInHierarchy&&selectionReadyButton.interactable)
            Show(ControlsID,controls);
    }
    private void Show(string id,ChirbitTutorial.Step[] steps)
    { if(steps==null||steps.Length==0)return;tutorial.sandboxOnly=true;tutorial.tutorialID=id;tutorial.steps=steps;tutorial.Begin(); }
    private void OnCompleted(bool skipped)
    {
        if(!skipped || !ARSandboxSession.IsActive)return;
        PlayerPrefs.SetInt("ARDENT.Chirbit."+IntroID+".v1",1);
        PlayerPrefs.SetInt("ARDENT.Chirbit."+ControlsID+".v1",1);
        PlayerPrefs.Save();
    }
}
