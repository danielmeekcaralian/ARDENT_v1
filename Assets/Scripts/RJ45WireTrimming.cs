using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

public sealed class RJ45WireTrimming : MonoBehaviour
{
    public RJ45WireArrangement board;
    public GameObject cutterVisual;
    public Collider cutterCollider;
    public Transform cuttingPoint;
    public GameObject cuttingGuide;
    public float cutY=.30f;
    public float crossingHalfWidth=.34f;
    public float gestureRadius=.14f;
    [HideInInspector] public bool inputEnabled;
    public bool HasStarted => stage!=Stage.Idle;
    public bool IsTrimmed => stage==Stage.Done;
    private enum Stage { Idle, Aligning, AwaitCut, Cutting, Done }
    private Stage stage;
    private Vector3[] originalBases,fromTips,fromBases;
    private RJ45DraggableWire[] ordered;
    private float[] originalRadii;
    private readonly List<GameObject> tipVisuals=new List<GameObject>();
    private readonly List<bool> tipVisibility=new List<bool>();
    private Vector3 toolHome;
    private float elapsed;
    private bool valid,dragging,suppress;
    private int finger=-1;
    private Vector2 gripOffset;
    private readonly RJ45CrossStripGesture gesture=new RJ45CrossStripGesture();
    private readonly List<RaycastResult> uiHits=new List<RaycastResult>();
    private void Awake()
    {
        valid=board!=null&&cutterVisual!=null&&cutterCollider!=null&&cuttingPoint!=null&&cuttingGuide!=null&&board.wires.Length==8;
        if(valid)foreach(var wire in board.wires)valid &= wire!=null&&wire.bendingTube!=null;
        if(!valid){Debug.LogError("Run Add Wire Trimming to configure the cutter.",this);enabled=false;return;}
        originalBases=new Vector3[8];for(int i=0;i<8;i++)originalBases[i]=board.wires[i].bendingTube.fixedBase;
        originalRadii=new float[8];
        for(int i=0;i<8;i++)
        {
            originalRadii[i]=board.wires[i].bendingTube.radius;
            foreach(string name in new[]{"TipVisual","WhiteMark"})
            {
                var child=board.wires[i].transform.Find(name);
                if(child!=null){tipVisuals.Add(child.gameObject);tipVisibility.Add(child.gameObject.activeSelf);}
            }
        }
        fromTips=new Vector3[8];fromBases=new Vector3[8];ordered=new RJ45DraggableWire[8];
        toolHome=cutterVisual.transform.localPosition;
        cutterVisual.SetActive(false);cuttingGuide.SetActive(false);
        board.onOrderCorrect.AddListener(BeginAlignment);
    }
    private void BeginAlignment()
    {
        if(!valid||HasStarted||!board.IsOrderCorrect)return;
        for(int i=0;i<8;i++)
        {
            ordered[i]=board.WireAtPin(i);
            if(ordered[i]==null)return;
            fromTips[i]=ordered[i].transform.localPosition;fromBases[i]=ordered[i].bendingTube.fixedBase;
        }
        foreach(var visual in tipVisuals)visual.SetActive(false);
        foreach(var wire in board.wires)wire.bendingTube.radius=.012f;
        stage=Stage.Aligning;elapsed=0;board.inputEnabled=false;
        foreach(var slot in board.pinSlots)slot.gameObject.SetActive(false);
        foreach(var label in board.pinLabels)if(label!=null)label.gameObject.SetActive(false);
        RefreshInstructions();
    }
    public void ResetTrimming()
    {
        if(!valid)return;
        Cancel();stage=Stage.Idle;elapsed=0;
        for(int i=0;i<8;i++)board.wires[i].bendingTube.fixedBase=originalBases[i];
        for(int i=0;i<8;i++)board.wires[i].bendingTube.radius=originalRadii[i];
        for(int i=0;i<tipVisuals.Count;i++)tipVisuals[i].SetActive(tipVisibility[i]);
        cutterVisual.transform.localPosition=toolHome;cutterVisual.SetActive(false);cuttingGuide.SetActive(false);
    }
    public void RefreshInstructions()
    {
        if(!HasStarted||board.instructionsText==null)return;
        board.instructionsText.text=stage==Stage.Aligning ? "Wire order correct. Straightening the conductors while keeping their order..."
            :stage==Stage.AwaitCut ? "Drag the WIRE CUTTER across the marked line, then release on the opposite side.\nEither sideways direction works."
            :stage==Stage.Cutting ? "Trimming the conductor ends evenly..."
            :"Conductors trimmed evenly. Ready for the RJ45 connector.";
    }
    private Vector3 AlignedTip(int pin) => new Vector3((pin-3.5f)*.034f,cutY+.08f+(pin%3)*.035f,-.12f);
    private void Update()
    {
        if(!valid||!HasStarted||stage==Stage.Done)return;
        if(!inputEnabled||UIManager.HasOpenPanel||SandboxInventoryPanel.IsOpen||ARCheckpointSession.BlocksInput){Cancel();return;}
        if(stage==Stage.Aligning||stage==Stage.Cutting)
        {
            elapsed+=Time.deltaTime;float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.65f));
            for(int i=0;i<8;i++)
            {
                var end=AlignedTip(i);
                if(stage==Stage.Aligning)
                {
                    ordered[i].transform.localPosition=Vector3.Lerp(fromTips[i],end,t);
                    ordered[i].bendingTube.fixedBase=Vector3.Lerp(fromBases[i],new Vector3(end.x,-.29f,-.12f),t);
                }
                else ordered[i].transform.localPosition=Vector3.Lerp(end,new Vector3(end.x,cutY,end.z),t);
            }
            if(t>=1)
            {
                if(stage==Stage.Aligning){stage=Stage.AwaitCut;cutterVisual.SetActive(true);cuttingGuide.SetActive(true);}
                else {stage=Stage.Done;cutterVisual.SetActive(false);cuttingGuide.SetActive(false);}
                RefreshInstructions();
            }
            return;
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
        if(stage!=Stage.AwaitCut||OverUI(p)||!Point(p,out var local))return;
        if(!cutterCollider.Raycast(board.interactionCamera.ScreenPointToRay(p),out _,1000))return;
        var contact=board.transform.InverseTransformPoint(cuttingPoint.position);
        gripOffset=new Vector2(contact.x,contact.y)-local;
        dragging=true;gesture.Reset();Move(p);
    }
    private void Move(Vector2 p)
    {
        if(!dragging)return;
        if(OverUI(p)||!Point(p,out var local)){Cancel();return;}
        var desired=local+gripOffset;desired.x=Mathf.Clamp(desired.x,-1.7f,1.7f);desired.y=Mathf.Clamp(desired.y,-1.4f,1f);
        var contact=board.transform.InverseTransformPoint(cuttingPoint.position);
        cutterVisual.transform.position+=board.transform.TransformVector(new Vector3(desired.x-contact.x,desired.y-contact.y,0));
        gesture.Sample(desired.x,desired.y,cutY,crossingHalfWidth,gestureRadius);
    }
    private void End(Vector2 p)
    {
        if(!dragging)return;Move(p);bool success=dragging&&gesture.CanFinish;
        dragging=false;gesture.Reset();
        if(success){stage=Stage.Cutting;elapsed=0;cuttingGuide.SetActive(false);RefreshInstructions();}
    }
    private void Cancel(){if(dragging&&cutterVisual!=null)cutterVisual.transform.localPosition=toolHome;dragging=false;gesture.Reset();}
    private void OnDisable(){Cancel();finger=-1;suppress=true;}
    private void OnApplicationFocus(bool focused){if(!focused){Cancel();finger=-1;suppress=true;}}
    private void OnDestroy(){if(board!=null)board.onOrderCorrect.RemoveListener(BeginAlignment);}
}


