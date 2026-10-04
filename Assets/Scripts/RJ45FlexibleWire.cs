using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// A controlled visual bend, not a rope/constant-length physics simulation.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class RJ45FlexibleWire : MonoBehaviour
{
    public Camera interactionCamera;
    public Transform tipHandle;
    public Collider tipCollider;
    public Transform snapTarget;
    public TMP_Text instructionsText;
    public Vector3 basePosition = new Vector3(0, -.8f, 0);
    public Vector3 initialTip = new Vector3(.7f, .6f, 0);
    [Min(.3f)] public float maximumReach = 1.85f;
    [Min(.01f)] public float radius = .035f;
    [Min(.01f)] public float snapDistance = .23f;
    [Range(12, 96)] public int curveSegments = 48;
    [Range(6, 16)] public int radialSegments = 10;
    public bool fitPrototypeCamera = true;
    public bool IsSnapped { get; private set; }

    private Mesh mesh;
    private Vector3[] vertices, normals;
    private Vector2[] uvs;
    private Vector3 tip, beforeDrag, offset;
    private bool dragging, wasSnapped, suppressTouch;
    private int finger = -1, rings, sides;
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    private void Awake()
    {
        if (interactionCamera == null) interactionCamera = Camera.main;
        if (interactionCamera == null || tipHandle == null || tipCollider == null || snapTarget == null)
        { Debug.LogError("Flexible wire requires a camera, tip handle/collider, and snap target.", this); enabled = false; return; }
        rings = Mathf.Clamp(curveSegments,12,96); sides = Mathf.Clamp(radialSegments,6,16);
        vertices = new Vector3[(rings+1)*(sides+1)]; normals = new Vector3[vertices.Length]; uvs = new Vector2[vertices.Length];
        mesh = new Mesh { name = "Runtime bendable wire" }; mesh.MarkDynamic();
        GetComponent<MeshFilter>().sharedMesh = mesh;
        mesh.subMeshCount = 2;
        var colored = new List<int>(); var white = new List<int>();
        for (int r=0; r<rings; r++) for(int s=0;s<sides;s++)
        {
            int a=r*(sides+1)+s, b=a+sides+1;
            var list = r%8<2 ? white : colored;
            list.Add(a); list.Add(b); list.Add(a+1);
            list.Add(b); list.Add(b+1); list.Add(a+1);
        }
        // Vertices must exist before assigning triangle indices.
        mesh.vertices = vertices; mesh.SetTriangles(colored,0); mesh.SetTriangles(white,1);
        ResetWire();
    }
    public void ResetWire()
    {
        if (mesh == null) return;
        dragging=false; finger=-1; IsSnapped=false; tip=Constrain(initialTip); Rebuild(); Feedback();
    }
    private Vector3 Constrain(Vector3 point)
    {
        point.z=basePosition.z;
        point.y=Mathf.Max(point.y,basePosition.y+.25f);
        return basePosition+Vector3.ClampMagnitude(point-basePosition,Mathf.Max(.3f,maximumReach));
    }
    private void Update()
    {
        if(mesh==null || interactionCamera==null) return;
        var screen=Touchscreen.current; int count=0;
        if(screen!=null) foreach(var t in screen.touches) if(t.press.isPressed) count++;
        if(count>1) { Cancel(); finger=-1; suppressTouch=true; return; }
        if(suppressTouch) { if(count==0) suppressTouch=false; return; }
        if(finger>=0 && screen!=null)
        {
            foreach(var t in screen.touches) if(t.touchId.ReadValue()==finger)
            {
                var p=t.position.ReadValue();
                if(t.phase.ReadValue()==UnityEngine.InputSystem.TouchPhase.Canceled) Cancel();
                else if(t.press.wasReleasedThisFrame) End(p);
                else if(t.press.isPressed) { Move(p); return; }
                else Cancel();
                finger=-1; return;
            }
            Cancel(); finger=-1; return;
        }
        if(count>0)
        {
            foreach(var t in screen.touches) if(t.press.wasPressedThisFrame)
            { Cancel(); finger=t.touchId.ReadValue(); Begin(t.position.ReadValue()); break; }
            return;
        }
        var mouse=Mouse.current;
        if(mouse==null) return;
        var position=mouse.position.ReadValue();
        if(mouse.leftButton.wasPressedThisFrame) Begin(position);
        if(mouse.leftButton.isPressed) Move(position);
        if(mouse.leftButton.wasReleasedThisFrame) End(position);
    }
    private void LateUpdate()
    {
        if(fitPrototypeCamera && interactionCamera!=null && interactionCamera.orthographic)
            interactionCamera.orthographicSize=Mathf.Max(1.85f,2.05f/Mathf.Max(.1f,interactionCamera.aspect));
    }
    private bool OverUI(Vector2 p)
    {
        if(EventSystem.current==null) return false;
        uiHits.Clear(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=p},uiHits);
        foreach(var hit in uiHits) if(hit.module is GraphicRaycaster) return true;
        return false;
    }
    private bool Point(Vector2 p,out Vector3 local)
    {
        var ray=interactionCamera.ScreenPointToRay(p);
        if(new Plane(transform.forward,transform.TransformPoint(basePosition)).Raycast(ray,out float distance))
        { local=transform.InverseTransformPoint(ray.GetPoint(distance)); return true; }
        local=default; return false;
    }
    private void Begin(Vector2 p)
    {
        if(OverUI(p) || !Point(p,out var local) || !tipCollider.Raycast(interactionCamera.ScreenPointToRay(p),out _,1000)) return;
        beforeDrag=tip; wasSnapped=IsSnapped; offset=tip-local; dragging=true; IsSnapped=false; Feedback();
    }
    private void Move(Vector2 p)
    {
        if(!dragging || !Point(p,out var local)) return;
        tip=Constrain(local+offset); Rebuild();
    }
    private void End(Vector2 p)
    {
        if(!dragging) return;
        if(OverUI(p)) { Cancel(); return; }
        Move(p); dragging=false;
        var target=transform.InverseTransformPoint(snapTarget.position);
        // Reject targets outside the drag plane/reach instead of stretching to them.
        if(Vector3.Distance(target,Constrain(target))<.001f && Vector3.Distance(tip,target)<=snapDistance)
        { tip=target; IsSnapped=true; }
        Rebuild(); Feedback();
    }
    private void Cancel()
    {
        if(!dragging) return;
        dragging=false; tip=beforeDrag; IsSnapped=wasSnapped; Rebuild(); Feedback();
    }
    private void OnApplicationFocus(bool focus) { if(!focus) { Cancel(); finger=-1; suppressTouch=true; } }
    private void OnDisable() { Cancel(); finger=-1; suppressTouch=false; }
    private void OnDestroy() { if(mesh!=null) Destroy(mesh); }
    private void Feedback()
    {
        if(instructionsText!=null) instructionsText.text=IsSnapped
            ? "Snapped! Drag the tip again to detach and bend the wire."
            : "Drag the orange tip. The cable end stays fixed.\nRelease near the target to snap. Pull farther to test the reach limit.";
    }
    private void Rebuild()
    {
        float distance=Vector3.Distance(basePosition,tip);
        var p1=basePosition+Vector3.up*distance*.45f;
        var p2=tip-Vector3.up*distance*.30f;
        for(int r=0;r<=rings;r++)
        {
            float t=(float)r/rings,u=1-t;
            var center=u*u*u*basePosition+3*u*u*t*p1+3*u*t*t*p2+t*t*t*tip;
            var tangent=(3*u*u*(p1-basePosition)+6*u*t*(p2-p1)+3*t*t*(tip-p2)).normalized;
            if(tangent.sqrMagnitude<.001f) tangent=Vector3.up;
            var right=Vector3.Cross(tangent,Vector3.forward).normalized;
            for(int s=0;s<=sides;s++)
            {
                float angle=2*Mathf.PI*s/sides;
                var normal=right*Mathf.Cos(angle)+Vector3.forward*Mathf.Sin(angle);
                int i=r*(sides+1)+s;
                vertices[i]=center+normal*Mathf.Max(.001f,radius); normals[i]=normal; uvs[i]=new Vector2((float)s/sides,t);
            }
        }
        mesh.vertices=vertices; mesh.normals=normals; mesh.uv=uvs; mesh.RecalculateBounds();
        tipHandle.position=transform.TransformPoint(tip);
    }
}
