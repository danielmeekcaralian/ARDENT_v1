using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ChirbitIdentificationTutorial : MonoBehaviour
{
    public ChirbitTutorial tutorial;
    public ARPlacementManager placement;
    public ARActivityProgress progress;
    public Button selectionReadyButton, completionButton;
    public GameObject inventoryPanel, completionPanel;
    public ChirbitTutorial.Step[] introduction, inspection, completion;
    private bool IsIdentification => !ARSandboxSession.IsActive && placement != null && placement.CurrentActivity != null &&
        (placement.CurrentActivity.activityType == ARActivityType.ToolIdentification || placement.CurrentActivity.activityType == ARActivityType.HardwareIdentification);
    private string Prefix => "ARIdentification." + placement.CurrentActivity.activityType;
    private void Awake(){if(tutorial!=null)tutorial.autoStart=false;}
    private void OnEnable(){if(tutorial!=null)tutorial.Completed+=Finished;}
    private void OnDisable(){if(tutorial!=null)tutorial.Completed-=Finished;}
    private static string Key(string id)=>"ARDENT.Chirbit."+id+".v1";
    private bool Seen(string stage)=>PlayerPrefs.GetInt(Key(Prefix+"."+stage),0)!=0;
    private void Finished(bool skipped)
    {
        if(!skipped||!IsIdentification)return;
        foreach(string stage in new[]{"Intro","Inspection","Completion"})PlayerPrefs.SetInt(Key(Prefix+"."+stage),1);
        PlayerPrefs.Save();
    }
    private void Update()
    {
        if(!IsIdentification||tutorial==null||!tutorial.IsReady||tutorial.IsShowing||ARCheckpointSession.BlocksInput||UIManager.HasOpenPanel||SandboxInventoryPanel.IsOpen)return;
        if((inventoryPanel!=null&&inventoryPanel.activeInHierarchy)||(completionPanel!=null&&completionPanel.activeInHierarchy))return;
        if(Mouse.current!=null&&Mouse.current.leftButton.isPressed)return;
        if(Touchscreen.current!=null)foreach(var touch in Touchscreen.current.touches)if(touch.press.isPressed)return;
        // A restored or one-item activity may already be complete; explain the available action first.
        if(completionButton!=null&&completionButton.gameObject.activeInHierarchy&&completionButton.interactable)
        {
            if(!Seen("Completion"))Show("Completion",completion);
            return;
        }
        if(!Seen("Intro")){Show("Intro",introduction);return;}
        if(!Seen("Inspection")&&progress!=null&&progress.CaptureInspectedObjects().Length>0&&
            selectionReadyButton!=null&&selectionReadyButton.gameObject.activeInHierarchy&&selectionReadyButton.interactable)
            Show("Inspection",inspection);
    }
    private void Show(string stage,ChirbitTutorial.Step[] steps)
    {
        if(steps==null||steps.Length==0)return;
        tutorial.sandboxOnly=false;tutorial.tutorialID=Prefix+"."+stage;tutorial.steps=steps;tutorial.Begin();
    }
}
