using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

public sealed class RJ45CablePreparation : MonoBehaviour
{
    public RJ45WireArrangement board;
    public GameObject toolVisual;
    public Collider toolCollider;
    public Transform cuttingPoint;
    public Transform jacketCover;
    public GameObject dragGuide;
    // Retained so the earlier setup script still compiles. Upgrade removes these visuals.
    [HideInInspector] public GameObject[] pairObjects;
    [HideInInspector] public Collider[] pairColliders;
    [HideInInspector] public Vector2 dragStart,dragEnd;
    public float stripY=-.27f;
    public float crossingHalfWidth=.32f;
    public float gestureRadius=.16f;
    [HideInInspector] public bool inputEnabled;
    public bool Ready => phase==Phase.Arrange;
    private enum Phase { Strip, RemoveSleeve, Reveal, Arrange }
    private Phase phase;
    private readonly RJ45CrossStripGesture gesture=new RJ45CrossStripGesture();
    private TMP_Text feedback;
    private bool initialized,dragging,suppress;
    private int finger=-1;
    private float animationTime;
    private Vector3 coverPosition,toolHome;
    private Vector2 gripOffset;
    private Vector3[] homes,bundled;
    private readonly List<RaycastResult> uiHits=new List<RaycastResult>();
    public void Initialize(TMP_Text text)
    {
        if(initialized)return;
        feedback=text;
        if(board==null||toolVisual==null||toolCollider==null||cuttingPoint==null||jacketCover==null||dragGuide==null)
        { Debug.LogError("Run ARDENT > RJ45 > Use Overlapping Cable Model before testing.",this);enabled=false;return; }
        coverPosition=jacketCover.localPosition;toolHome=toolVisual.transform.localPosition;
        homes=new Vector3[8];bundled=new Vector3[8];
        for(int i=0;i<8;i++)
        {
            homes[i]=board.wires[i].homePosition;
            bundled[i]=new Vector3((i%4-1.5f)*.044f,.31f+(i/4)*.025f,-.12f+(i/4-.5f)*.044f);
        }
        initialized=true;ResetPreparation();
    }
    public void ResetPreparation()
    {
        if(!initialized)return;
        Cancel();phase=Phase.Strip;animationTime=0;
        toolVisual.transform.localPosition=toolHome;jacketCover.localPosition=coverPosition;
        board.Restart();SetVisible(false);RefreshInstructions();
    }
    public void RefreshInstructions()
    {
        if(!initialized||Ready||feedback==null)return;
        feedback.text=phase==Phase.Strip
            ? "Grab the wire stripper and drag it SIDEWAYS across the highlighted cable section.\nLeft to right or right to left, then release on the other side."
            : phase==Phase.RemoveSleeve ? "Removing the jacket sleeve..." : "Eight conductors exposed. Preparing the wire tips for dragging...";
    }
    private void SetVisible(bool conductors)
    {
        toolVisual.SetActive(phase==Phase.Strip);
        dragGuide.SetActive(phase==Phase.Strip);
        jacketCover.gameObject.SetActive(phase==Phase.Strip||phase==Phase.RemoveSleeve);
        if(pairObjects!=null)foreach(var pair in pairObjects)if(pair!=null)pair.SetActive(false);
        for(int i=0;i<8;i++)
        {
            var wire=board.wires[i];wire.gameObject.SetActive(conductors);
            if(wire.bendingTube!=null)wire.bendingTube.gameObject.SetActive(conductors);
            // Wire names stay hidden; numbered pin/status labels are separate below.
            foreach(var label in wire.GetComponentsInChildren<TMP_Text>(true))label.gameObject.SetActive(false);
            board.pinSlots[i].gameObject.SetActive(Ready);
            if(board.pinLabels[i]!=null)board.pinLabels[i].gameObject.SetActive(Ready);
        }
        board.inputEnabled=false;
    }
    private void Update()
    {
        if(!initialized||Ready)return;
        if(!inputEnabled||UIManager.HasOpenPanel||SandboxInventoryPanel.IsOpen||ARCheckpointSession.BlocksInput){Cancel();return;}
        if(phase==Phase.RemoveSleeve||phase==Phase.Reveal)
        {
            animationTime+=Time.deltaTime;
            if(phase==Phase.RemoveSleeve)
            {
                float t=Mathf.Clamp01(animationTime/.8f);
                jacketCover.localPosition=coverPosition+Vector3.up*(t*.95f);
                for(int i=0;i<8;i++)board.wires[i].transform.localPosition=bundled[i];
                if(t>=1){phase=Phase.Reveal;animationTime=0;SetVisible(true);RefreshInstructions();}
            }
            else
            {
                // Briefly show the bundle, then spread only the tips for touch selection.
                float t=Mathf.SmoothStep(0,1,Mathf.Clamp01((animationTime-.6f)/.8f));
                for(int i=0;i<8;i++)board.wires[i].transform.localPosition=Vector3.Lerp(bundled[i],homes[i],t);
                if(t>=1){phase=Phase.Arrange;SetVisible(true);board.Restart();feedback.text+="\nSimplified view: conductors are shown untwisted.";}
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
        if(phase!=Phase.Strip||OverUI(p)||!Point(p,out var local))return;
        if(!toolCollider.Raycast(board.interactionCamera.ScreenPointToRay(p),out _,1000))return;
        var contact=board.transform.InverseTransformPoint(cuttingPoint.position);
        gripOffset=new Vector2(contact.x,contact.y)-local;
        dragging=true;gesture.Reset();Move(p);
    }
    private void Move(Vector2 p)
    {
        if(!dragging)return;
        if(OverUI(p)||!Point(p,out var local)){Cancel();return;}
        var desired=local+gripOffset;
        desired.x=Mathf.Clamp(desired.x,-1.7f,1.7f);desired.y=Mathf.Clamp(desired.y,-1.4f,.7f);
        var contact=board.transform.InverseTransformPoint(cuttingPoint.position);
        toolVisual.transform.position+=board.transform.TransformVector(new Vector3(desired.x-contact.x,desired.y-contact.y,0));
        gesture.Sample(desired.x,desired.y,stripY,crossingHalfWidth,gestureRadius);
    }
    private void End(Vector2 p)
    {
        if(!dragging)return;Move(p);
        bool success=dragging&&gesture.CanFinish;
        dragging=false;gesture.Reset();
        if(success)
        {
            phase=Phase.RemoveSleeve;animationTime=0;
            for(int i=0;i<8;i++)board.wires[i].transform.localPosition=bundled[i];
            SetVisible(true);RefreshInstructions();
        }
    }
    private void Cancel()
    {
        if(dragging&&toolVisual!=null)toolVisual.transform.localPosition=toolHome;
        dragging=false;gesture.Reset();
    }
    private void OnDisable(){Cancel();finger=-1;suppress=true;}
    private void OnApplicationFocus(bool focused){if(!focused){Cancel();finger=-1;suppress=true;}}
}


