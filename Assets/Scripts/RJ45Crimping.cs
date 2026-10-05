using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

public sealed class RJ45Crimping : MonoBehaviour
{
    public RJ45WireArrangement board;
    public RJ45ConnectorInsertion insertion;
    public Transform tool;
    public Collider toolCollider;
    public Transform crimpPoint;
    public GameObject targetGuide;
    public Transform[] contactsAndClamps;
    public float snapDistance=.20f;
    [HideInInspector] public bool inputEnabled;
    public bool IsCrimped => stage==Stage.Done;
    private enum Stage { Dormant, Waiting, Seating, Pressing, Done }
    private Stage stage;
    private bool valid,dragging,suppress,waitForRelease;
    private int finger=-1;
    private float elapsed;
    private Vector2 gripOffset;
    private Vector3 home,beforeDrag,seatFrom,seatTo;
    private Vector3[] originalPositions;
    private readonly List<RaycastResult> uiHits=new List<RaycastResult>();
    private void Awake()
    {
        valid=board!=null&&insertion!=null&&tool!=null&&toolCollider!=null&&crimpPoint!=null&&targetGuide!=null&&contactsAndClamps!=null&&contactsAndClamps.Length==18;
        if(valid)foreach(var item in contactsAndClamps)valid &= item!=null;
        if(!valid){Debug.LogError("Run Add Crimping Step to configure the crimper and contacts.",this);enabled=false;return;}
        originalPositions=new Vector3[contactsAndClamps.Length];for(int i=0;i<originalPositions.Length;i++)originalPositions[i]=contactsAndClamps[i].localPosition;
        home=tool.localPosition;ResetCrimping();
    }
    public void ResetCrimping()
    {
        if(!valid)return;
        Cancel();stage=Stage.Dormant;elapsed=0;finger=-1;waitForRelease=true;
        tool.localPosition=home;tool.gameObject.SetActive(false);targetGuide.SetActive(false);
        for(int i=0;i<originalPositions.Length;i++)contactsAndClamps[i].localPosition=originalPositions[i];
    }
    public void RestoreCrimpedCheckpoint()
    {
        if(!valid)throw new System.InvalidOperationException("Crimping is not configured.");
        ResetCrimping();
        for(int i=0;i<originalPositions.Length;i++) contactsAndClamps[i].localPosition=originalPositions[i]+Vector3.forward*.025f;
        stage=Stage.Done;RefreshInstructions();
    }
    public void RefreshInstructions()
    {
        if(stage==Stage.Dormant||board.instructionsText==null)return;
        board.instructionsText.text=stage==Stage.Waiting
            ? "Inspect the inserted wires first. When ready, drag the CRIMPER onto the connector and release.\nAlign its yellow marker with the connector outline."
            :stage==Stage.Seating ? "Positioning the crimper..."
            :stage==Stage.Pressing ? "Crimping: pressing the contacts and securing the jacket..."
            :"Connector crimped. The cable is ready for testing.\nCable testing is the next step to be added.";
        ARCheckpointSession.SaveCurrent();
    }
    private void Update()
    {
        if(!valid)return;
        if(!inputEnabled||UIManager.HasOpenPanel||SandboxInventoryPanel.IsOpen||ARCheckpointSession.BlocksInput){Cancel();return;}
        if(stage==Stage.Dormant)
        {
            if(!insertion.IsInserted)return;
            stage=Stage.Waiting;waitForRelease=true;tool.gameObject.SetActive(true);targetGuide.SetActive(true);RefreshInstructions();
        }
        if(stage==Stage.Done)return;
        if(stage==Stage.Seating)
        {
            elapsed+=Time.deltaTime;float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.35f));
            tool.localPosition=Vector3.Lerp(seatFrom,seatTo,t);
            if(t>=1){stage=Stage.Pressing;elapsed=0;RefreshInstructions();}return;
        }
        if(stage==Stage.Pressing)
        {
            elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/1.1f);
            tool.localPosition=seatTo+Vector3.forward*(Mathf.Sin(t*Mathf.PI)*.055f);
            float pressed=Mathf.SmoothStep(0,1,Mathf.Clamp01(t*2));
            for(int i=0;i<originalPositions.Length;i++)contactsAndClamps[i].localPosition=originalPositions[i]+Vector3.forward*(pressed*.025f);
            if(t>=1){stage=Stage.Done;tool.localPosition=home;tool.gameObject.SetActive(false);RefreshInstructions();}return;
        }
        if(waitForRelease)
        {
            bool held=Mouse.current!=null&&Mouse.current.leftButton.isPressed;
            if(Touchscreen.current!=null)foreach(var touch in Touchscreen.current.touches)held|=touch.press.isPressed;
            if(held)return;waitForRelease=false;return;
        }
        var screen=Touchscreen.current;int count=0;
        if(screen!=null)foreach(var t in screen.touches)if(t.press.isPressed)count++;
        if(count>1){Cancel();finger=-1;suppress=true;return;}
        if(suppress){if(count==0)suppress=false;return;}
        if(finger>=0 && screen!=null)
        {
            foreach(var t in screen.touches)if(t.touchId.ReadValue()==finger)
            {
                var p=t.position.ReadValue();
                if(t.phase.ReadValue()==UnityEngine.InputSystem.TouchPhase.Canceled)Cancel();
                else if(t.press.wasReleasedThisFrame)End(p);
                else if(t.press.isPressed){Move(p);return;}
                else Cancel();finger=-1;return;
            }
            Cancel();finger=-1;return;
        }
        if(count>0){foreach(var t in screen.touches)if(t.press.wasPressedThisFrame){finger=t.touchId.ReadValue();Begin(t.position.ReadValue());break;}return;}
        var mouse=Mouse.current;if(mouse==null)return;var position=mouse.position.ReadValue();
        if(mouse.leftButton.wasPressedThisFrame)Begin(position);
        if(mouse.leftButton.isPressed)Move(position);
        if(mouse.leftButton.wasReleasedThisFrame)End(position);
    }
    private bool OverUI(Vector2 p)
    {
        if(EventSystem.current==null)return false;
        uiHits.Clear();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=p},uiHits);
        foreach(var h in uiHits)if(h.module is GraphicRaycaster)return true;return false;
    }
    private bool Point(Vector2 p,out Vector2 local)
    {
        local=default;if(board.interactionCamera==null)return false;
        var ray=board.interactionCamera.ScreenPointToRay(p);
        if(!new Plane(board.transform.forward,board.transform.position).Raycast(ray,out float distance))return false;
        var value=board.transform.InverseTransformPoint(ray.GetPoint(distance));local=new Vector2(value.x,value.y);return true;
    }
    private void Begin(Vector2 p)
    {
        if(stage!=Stage.Waiting||OverUI(p)||!Point(p,out var local))return;
        if(!toolCollider.Raycast(board.interactionCamera.ScreenPointToRay(p),out _,1000))return;
        beforeDrag=tool.localPosition;var point=board.transform.InverseTransformPoint(crimpPoint.position);
        gripOffset=new Vector2(point.x,point.y)-local;dragging=true;
    }
    private void Move(Vector2 p)
    {
        if(!dragging)return;if(OverUI(p)||!Point(p,out var local)){Cancel();return;}
        var desired=local+gripOffset;desired.x=Mathf.Clamp(desired.x,-1.7f,1.7f);desired.y=Mathf.Clamp(desired.y,-1.3f,1.1f);
        var current=board.transform.InverseTransformPoint(crimpPoint.position);
        tool.position+=board.transform.TransformVector(new Vector3(desired.x-current.x,desired.y-current.y,0));
    }
    private void End(Vector2 p)
    {
        if(!dragging)return;Move(p);if(!dragging)return;dragging=false;
        var point=board.transform.InverseTransformPoint(crimpPoint.position);
        var target=board.transform.InverseTransformPoint(insertion.connector.position);
        if(Vector2.Distance(new Vector2(point.x,point.y),new Vector2(target.x,target.y))<=snapDistance)
        {
            seatFrom=tool.localPosition;seatTo=seatFrom+new Vector3(target.x-point.x,target.y-point.y,0);
            stage=Stage.Seating;elapsed=0;targetGuide.SetActive(false);RefreshInstructions();
        }
        else if(board.instructionsText!=null)board.instructionsText.text="Move the crimper's yellow marker closer to the connector outline, then release.";
    }
    private void Cancel(){if(dragging&&tool!=null)tool.localPosition=beforeDrag;dragging=false;}
    private void OnDisable(){Cancel();finger=-1;suppress=true;waitForRelease=true;}
    private void OnApplicationFocus(bool focused){if(!focused){Cancel();finger=-1;suppress=true;waitForRelease=true;}}
}
