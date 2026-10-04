#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RJ45PreparationSetup
{
    [MenuItem("ARDENT/RJ45/Add Cable Preparation")]
    public static void Setup()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage()!=null)
        {Debug.LogWarning("Exit Play Mode and Prefab Mode first.");return;}
        const string path="Assets/Models/RJ45TabletopWorkstation.prefab";
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var tool=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/wirestripper.prefab");
        var shader=Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        if(asset==null || tool==null || shader==null || TMP_Settings.defaultFontAsset==null)
        {Debug.LogError("Need the tabletop workstation, Assets/Models/wirestripper.prefab, and TMP font.");return;}
        if(asset.GetComponentInChildren<RJ45CablePreparation>(true)!=null)
        {Debug.Log("Cable preparation already exists. Customize PreparationVisuals in the workstation prefab.");return;}
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        string backup="Library/ARDENTRJ45Backups/Preparation-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
        Directory.CreateDirectory(backup);File.Copy(path,Path.Combine(backup,"RJ45TabletopWorkstation.prefab"));
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var board=root.GetComponentInChildren<RJ45WireArrangement>(true);
            if(board==null)throw new InvalidOperationException("Workstation has no wire board.");
            string folder=AssetDatabase.GenerateUniqueAssetPath("Assets/Models/RJ45PreparationMaterials");
            AssetDatabase.CreateFolder("Assets/Models",Path.GetFileName(folder));
            var gray=Mat(shader,folder,"Jacket",new Color(.25f,.29f,.35f));
            var yellow=Mat(shader,folder,"Guide",new Color(1,.8f,.1f));
            var white=Mat(shader,folder,"White",Color.white);
            var prep=board.gameObject.AddComponent<RJ45CablePreparation>();prep.board=board;
            var visuals=new GameObject("PreparationVisuals").transform;visuals.SetParent(board.transform,false);
            var holder=new GameObject("WireStripperSelectable").transform;holder.SetParent(visuals,false);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(tool);
            PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            model.transform.SetParent(holder,false);model.transform.localPosition=Vector3.zero;
            foreach(var component in model.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(component);
            foreach(var collider in model.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
            foreach(var body in model.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(body);
            // Fit a visual COPY into the station; original model dimensions remain untouched.
            var bounds=LocalBounds(holder);
            float longest=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
            if(longest<.00001f)throw new InvalidOperationException("Stripper mesh bounds are empty.");
            model.transform.localScale*=.65f/longest;
            bounds=LocalBounds(holder);model.transform.localPosition-=bounds.center;
            var pick=holder.gameObject.AddComponent<BoxCollider>();pick.size=Vector3.Max(LocalBounds(holder).size,new Vector3(.35f,.35f,.15f));
            holder.localPosition=new Vector3(1.1f,-.95f,-.15f);
            prep.toolVisual=holder.gameObject;prep.toolCollider=pick;
            Label("ToolLabel",holder,"WIRE STRIPPER\nTap to select",new Vector3(0,-.46f,-.1f),.85f);
            prep.jacketCover=Cube("RemovableJacket",visuals,new Vector3(0,-.62f,-.03f),new Vector3(1.4f,.85f,.12f),gray).transform;
            var guide=new GameObject("StripDragGuide").transform;guide.SetParent(visuals,false);prep.dragGuide=guide.gameObject;
            Cube("DragStrip",guide,new Vector3(0,-.535f,-.11f),new Vector3(.10f,.63f,.02f),yellow);
            Label("Start",guide,"START",new Vector3(.35f,-.85f,-.13f),.6f);
            Label("End",guide,"END",new Vector3(.35f,-.22f,-.13f),.6f);
            prep.pairObjects=new GameObject[4];prep.pairColliders=new Collider[4];
            Color[] colors={new Color(1,.4f,.05f),new Color(.1f,.7f,.25f),new Color(.12f,.4f,1),new Color(.55f,.3f,.1f)};
            string[] names={"Orange","Green","Blue","Brown"};
            for(int i=0;i<4;i++)
            {
                var pair=new GameObject(names[i]+"Pair");pair.transform.SetParent(visuals,false);pair.transform.localPosition=new Vector3((i-1.5f)*.32f,-.94f,-.04f);
                var color=Mat(shader,folder,names[i],colors[i]);
                for(int strand=0;strand<2;strand++)
                {
                    var line=new GameObject("Conductor"+strand).AddComponent<LineRenderer>();line.transform.SetParent(pair.transform,false);
                    line.useWorldSpace=false;line.positionCount=49;line.widthMultiplier=.026f;line.sharedMaterial=strand==0?color:white;
                    for(int n=0;n<49;n++){float t=n/48f;float angle=t*Mathf.PI*6+strand*Mathf.PI;line.SetPosition(n,new Vector3(Mathf.Sin(angle)*.045f,t*.6f,Mathf.Cos(angle)*.025f));}
                }
                var collider=pair.AddComponent<BoxCollider>();collider.center=new Vector3(0,.3f,0);collider.size=new Vector3(.24f,.68f,.18f);
                prep.pairObjects[i]=pair;prep.pairColliders[i]=collider;
                Label("PairLabel",pair.transform,names[i],new Vector3(0,.76f,-.1f),.45f);
            }
            PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();
            Debug.Log("Cable preparation added. Test Splicing AR: select stripper, drag START to END, release, tap four pairs, then arrange wires. Backup: "+Path.GetFullPath(backup));
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    private static Bounds LocalBounds(Transform root)
    {
        bool found=false;var result=new Bounds();
        foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var b=renderer.localBounds;
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
            {
                var p=root.InverseTransformPoint(renderer.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));
                if(!found){result=new Bounds(p,Vector3.zero);found=true;}else result.Encapsulate(p);
            }
        }
        return result;
    }
    private static Material Mat(Shader shader,string folder,string name,Color color)
    {var m=new Material(shader);m.color=color;AssetDatabase.CreateAsset(m,folder+"/"+name+".mat");return m;}
    private static GameObject Cube(string name,Transform parent,Vector3 position,Vector3 scale,Material material)
    {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=scale;
        UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());obj.GetComponent<Renderer>().sharedMaterial=material;return obj;
    }
    private static void Label(string name,Transform parent,string value,Vector3 position,float width)
    {
        var text=new GameObject(name).AddComponent<TextMeshPro>();text.transform.SetParent(parent,false);text.transform.localPosition=position;
        text.rectTransform.sizeDelta=new Vector2(width,.3f);text.font=TMP_Settings.defaultFontAsset;text.fontSize=1.1f;text.color=Color.white;text.alignment=TextAlignmentOptions.Center;text.text=value;text.raycastTarget=false;
    }
}
#endif
