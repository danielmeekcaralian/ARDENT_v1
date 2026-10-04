using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

public sealed class RJ45ConnectorInsertion : MonoBehaviour
{
    public RJ45WireArrangement board;
    public RJ45WireTrimming trimming;
    public Transform connector;
    public Collider connectorCollider;
    public Transform target;
    public GameObject targetGuide;
    public GameObject inspectionView;
    public Renderer[] inspectionWires;
    public GameObject[] inspectionStripes;
    public float snapDistance=.18f;
    [HideInInspector] public bool inputEnabled;
    public bool IsInserted => stage==Stage.Inspect;
    private enum Stage { Dormant, Waiting, Seating, Inspect }
    private Stage stage;
    private bool valid,dragging,suppress,waitForRelease;
    private int finger=-1;
    private Vector2 gripOffset;
    private Vector3 home,beforeDrag,seatFrom;
    private float elapsed;
    private readonly List<RaycastResult> uiHits=new List<RaycastResult>();
    private void Awake()
    {
        valid=board!=null&&trimming!=null&&connector!=null&&connectorCollider!=null&&target!=null&&targetGuide!=null&&inspectionView!=null&&inspectionWires!=null&&inspectionWires.Length==8&&inspectionStripes!=null&&inspectionStripes.Length==8;
        if(valid)for(int i=0;i<8;i++)valid &= inspectionWires[i]!=null&&inspectionStripes[i]!=null;
        if(!valid){Debug.LogError("Run Add Connector Insertion to configure the placeholder.",this);enabled=false;return;}
        home=connector.localPosition;ResetInsertion();
    }
    public void ResetInsertion()
    {
        if(!valid)return;
        Cancel();stage=Stage.Dormant;elapsed=0;finger=-1;waitForRelease=true;
        connector.localPosition=home;connector.gameObject.SetActive(false);targetGuide.SetActive(false);inspectionView.SetActive(false);
    }
    public void RefreshInstructions()
    {
        if(stage==Stage.Dormant||board.instructionsText==null)return;
        board.instructionsText.text=stage==Stage.Waiting
            ? "Drag the RJ45 connector onto the outlined target at the trimmed wire ends, then release.\nThe clip stays underneath; wire order is preserved."
            :stage==Stage.Seating ? "Seating the conductors inside the connector..."
            :"Connector inserted. Inspect the enlarged view: pin order, wire tips at the front, and jacket inside the rear.\nThis is an uncrimped placeholder. Crimping comes next.";
    }
    private void Update()
    {
        if(!valid)return;
        if(!inputEnabled||UIManager.HasOpenPanel||SandboxInventoryPanel.IsOpen||ARCheckpointSession.BlocksInput){Cancel();return;}
        if(stage==Stage.Dormant)
        {
            if(!trimming.IsTrimmed)return;
            stage=Stage.Waiting;waitForRelease=true;connector.gameObject.SetActive(true);targetGuide.SetActive(true);RefreshInstructions();
        }
        if(stage==Stage.Inspect)return;
        if(stage==Stage.Seating)
        {
            elapsed+=Time.deltaTime;float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.45f));
            connector.localPosition=Vector3.Lerp(seatFrom,board.transform.InverseTransformPoint(target.position),t);
            if(t>=1){stage=Stage.Inspect;ShowInspection();RefreshInstructions();}
            return;
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
        if(!connectorCollider.Raycast(board.interactionCamera.ScreenPointToRay(p),out _,1000))return;
        beforeDrag=connector.localPosition;gripOffset=new Vector2(beforeDrag.x,beforeDrag.y)-local;dragging=true;
    }
    private void Move(Vector2 p)
    {
        if(!dragging)return;
        if(OverUI(p)||!Point(p,out var local)){Cancel();return;}
        var desired=local+gripOffset;
        connector.localPosition=new Vector3(Mathf.Clamp(desired.x,-1.7f,1.7f),Mathf.Clamp(desired.y,-.6f,1.1f),home.z);
    }
    private void End(Vector2 p)
    {
        if(!dragging)return;Move(p);if(!dragging)return;dragging=false;
        var position=connector.localPosition;var destination=board.transform.InverseTransformPoint(target.position);
        if(Vector2.Distance(new Vector2(position.x,position.y),new Vector2(destination.x,destination.y))<=snapDistance)
        {stage=Stage.Seating;elapsed=0;seatFrom=position;targetGuide.SetActive(false);RefreshInstructions();}
        else if(board.instructionsText!=null)board.instructionsText.text="Move the connector closer to the outline at the wire ends, then release.";
    }
    private void ShowInspection()
    {
        for(int i=0;i<8;i++)
        {
            var wire=board.WireAtPin(i);
            if(wire==null){Debug.LogError("Inspection requires eight ordered wires.",this);return;}
            var renderer=wire.bendingTube.GetComponent<Renderer>();
            inspectionWires[i].sharedMaterial=renderer.sharedMaterials[0];
            inspectionStripes[i].SetActive(wire.bendingTube.striped);
        }
        inspectionView.SetActive(true);
    }
    private void Cancel(){if(dragging&&connector!=null)connector.localPosition=beforeDrag;dragging=false;}
    private void OnDisable(){Cancel();finger=-1;suppress=true;waitForRelease=true;}
    private void OnApplicationFocus(bool focused){if(!focused){Cancel();finger=-1;suppress=true;waitForRelease=true;}}
}
