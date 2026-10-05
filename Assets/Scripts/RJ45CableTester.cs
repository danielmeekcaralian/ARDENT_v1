using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class RJ45CableTester : MonoBehaviour
{
    public RJ45WireArrangement board;
    public RJ45Crimping crimping;
    public GameObject testerVisual;
    public Collider testerCollider;
    public Renderer[] mainLights,remoteLights;
    [HideInInspector] public bool inputEnabled;
    public ARActivityData Activity { get; private set; }
    public bool IsPassed => valid && sequence.Passed && crimping.IsCrimped && board.IsOrderCorrect;
    private readonly RJ45TestSequence sequence=new RJ45TestSequence();
    private readonly List<RaycastResult> uiHits=new List<RaycastResult>();
    private MaterialPropertyBlock colors;
    private bool valid,available,recorded,waitForRelease=true;
    private bool appFocused=true,appPaused;
    private float retryAt;
    private int shownPin=-1;
    private void Awake()
    {
        valid=board!=null&&crimping!=null&&testerVisual!=null&&testerCollider!=null&&mainLights!=null&&remoteLights!=null&&mainLights.Length==8&&remoteLights.Length==8;
        if(valid)for(int i=0;i<8;i++)valid &= mainLights[i]!=null&&remoteLights[i]!=null;
        if(!valid){Debug.LogError("Run Add Tester Demonstration to connect the tester.",this);enabled=false;return;}
        Activity=LessonSession.CurrentLesson!=null?LessonSession.CurrentLesson.arActivity:null;
        colors=new MaterialPropertyBlock();ResetTester();
    }
    public void ResetTester()
    {
        if(!valid)return;
        sequence.Reset();available=false;waitForRelease=true;shownPin=-1;
        testerVisual.SetActive(false);SetLights();
        // Earned lesson completion is deliberately retained when practicing again.
    }
    public void RestorePassedCheckpoint()
    {
        if(!valid||!crimping.IsCrimped||!board.IsOrderCorrect)throw new System.InvalidOperationException("Tester prerequisites are missing.");
        sequence.RestorePassed();available=true;testerVisual.SetActive(true);SetLights();RefreshInstructions();
    }
    public void RefreshInstructions()
    {
        if(!available||board.instructionsText==null)return;
        board.instructionsText.text=sequence.Passed
            ? recorded ? "Simulated cable test passed: pins 1–8 matched.\nAR activity completed! Use the completion button to continue."
                       : "Simulated test passed, but completion has not been saved yet.\nChecking the lesson progress system..."
            :sequence.Running ? "SIMULATED TEST: pin "+sequence.Pin+" → "+sequence.Pin+"\nThe other end is preterminated to the same wiring standard."
            :"Tap the LAN tester to start the simulated 1–8 test.\nBoth ends are assumed connected; the other end uses the same wiring standard.";
        ARCheckpointSession.SaveCurrent();
    }
    private void Update()
    {
        if(!valid)return;
        if(!appFocused||appPaused)return;
        if(!inputEnabled||UIManager.HasOpenPanel||SandboxInventoryPanel.IsOpen||ARCheckpointSession.BlocksInput){waitForRelease=true;return;}
        if(!crimping.IsCrimped)return;
        if(!available){available=true;testerVisual.SetActive(true);waitForRelease=true;RefreshInstructions();}
        if(sequence.Running)
        {
            sequence.Advance(Time.deltaTime);
            if(shownPin!=sequence.Pin||sequence.Passed){SetLights();RefreshInstructions();}
        }
        if(sequence.Passed)
        {
            if(!recorded && Time.unscaledTime>=retryAt)
            {
                retryAt=Time.unscaledTime+2f;
                var progress=FindFirstObjectByType<ARActivityProgress>();
                recorded=progress!=null&&progress.TryCompleteRJ45Activity(this);
                RefreshInstructions();
            }
            return;
        }
        if(sequence.Running)return;
        var screen=Touchscreen.current;int count=0;Vector2 position=default;bool pressed=false;
        if(screen!=null)foreach(var touch in screen.touches)if(touch.press.isPressed){count++;position=touch.position.ReadValue();pressed=touch.press.wasPressedThisFrame;}
        bool mouseHeld=Mouse.current!=null&&Mouse.current.leftButton.isPressed;
        if(count>1){waitForRelease=true;return;}
        if(waitForRelease){if(count==0&&!mouseHeld)waitForRelease=false;return;}
        if(count==0&&Mouse.current!=null){pressed=Mouse.current.leftButton.wasPressedThisFrame;position=Mouse.current.position.ReadValue();}
        if(!pressed||OverUI(position)||board.interactionCamera==null)return;
        if(testerCollider.Raycast(board.interactionCamera.ScreenPointToRay(position),out _,1000)&&sequence.Start(crimping.IsCrimped&&board.IsOrderCorrect))
        {SetLights();RefreshInstructions();}
    }
    private bool OverUI(Vector2 p)
    {
        if(EventSystem.current==null)return false;
        uiHits.Clear();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=p},uiHits);
        foreach(var h in uiHits)if(h.module is GraphicRaycaster)return true;return false;
    }
    private void SetLights()
    {
        shownPin=sequence.Pin;
        for(int i=0;i<8;i++)
        {
            Color value=sequence.Passed||sequence.Running&&i+1==sequence.Pin?new Color(.1f,1,.3f):new Color(.08f,.14f,.10f);
            colors.SetColor("_BaseColor",value);colors.SetColor("_Color",value);
            mainLights[i].SetPropertyBlock(colors);remoteLights[i].SetPropertyBlock(colors);
        }
    }
    private void OnDisable(){waitForRelease=true;}
    private void OnApplicationFocus(bool focus){appFocused=focus;if(!focus)waitForRelease=true;}
    private void OnApplicationPause(bool paused){appPaused=paused;if(paused)waitForRelease=true;}
}


