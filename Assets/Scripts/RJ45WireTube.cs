using System.Collections.Generic;
using UnityEngine;

// Visual only. RJ45WireArrangement remains the sole input and order controller.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class RJ45WireTube : MonoBehaviour
{
    public Transform tip;
    public Vector3 fixedBase = new Vector3(0,-1.15f,0);
    public bool striped;
    public float radius = .025f;
    public float maximumReach = 2.8f;
    private Mesh mesh;
    private Vector3[] vertices, normals;
    private Vector3 previousTip;
    private bool built;
    private const int Rings=40, Sides=8;

    // All positions are in board-local coordinates; tube root must be at board origin.
    public Vector3 ConstrainTip(Vector3 position)
    {
        position.x=Mathf.Clamp(position.x,-1.72f,1.72f);
        position.y=Mathf.Clamp(position.y,-.8f,.65f);
        var delta=position-fixedBase;
        return fixedBase+Vector3.ClampMagnitude(delta,Mathf.Max(.5f,maximumReach));
    }
    private void Awake()
    {
        if(tip==null) { enabled=false; Debug.LogError("Wire tube needs its draggable tip.",this); return; }
        vertices=new Vector3[(Rings+1)*(Sides+1)]; normals=new Vector3[vertices.Length];
        mesh=new Mesh { name="Runtime RJ45 wire tube" }; mesh.MarkDynamic();
        GetComponent<MeshFilter>().sharedMesh=mesh; mesh.vertices=vertices; mesh.subMeshCount=2;
        var color=new List<int>(); var white=new List<int>();
        for(int r=0;r<Rings;r++) for(int s=0;s<Sides;s++)
        {
            int a=r*(Sides+1)+s,b=a+Sides+1;
            var list=striped && r%8<2 ? white : color;
            list.Add(a);list.Add(b);list.Add(a+1);list.Add(b);list.Add(b+1);list.Add(a+1);
        }
        mesh.SetTriangles(color,0);mesh.SetTriangles(white,1);
    }
    private void LateUpdate()
    {
        if(mesh==null || tip==null) return;
        var end=transform.InverseTransformPoint(tip.position);
        if(built && (end-previousTip).sqrMagnitude<.00000001f) return;
        previousTip=end;built=true;
        float length=Vector3.Distance(fixedBase,end);
        var p1=fixedBase+Vector3.up*length*.45f;
        var p2=end-Vector3.up*length*.30f;
        for(int r=0;r<=Rings;r++)
        {
            float t=(float)r/Rings,u=1-t;
            var center=u*u*u*fixedBase+3*u*u*t*p1+3*u*t*t*p2+t*t*t*end;
            var tangent=(3*u*u*(p1-fixedBase)+6*u*t*(p2-p1)+3*t*t*(end-p2)).normalized;
            if(tangent.sqrMagnitude<.001f) tangent=Vector3.up;
            var right=Vector3.Cross(tangent,Vector3.forward).normalized;
            var binormal=Vector3.Cross(right,tangent).normalized;
            for(int s=0;s<=Sides;s++)
            {
                float angle=2*Mathf.PI*s/Sides;
                var normal=right*Mathf.Cos(angle)+binormal*Mathf.Sin(angle);
                int i=r*(Sides+1)+s;vertices[i]=center+normal*Mathf.Max(.001f,radius);normals[i]=normal;
            }
        }
        mesh.vertices=vertices;mesh.normals=normals;mesh.RecalculateBounds();
    }
    private void OnDestroy() { if(mesh!=null) Destroy(mesh); }
}
