#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RJ45CrimpingSetup
{
 [MenuItem("ARDENT/RJ45/Add Crimping Step")]
 public static void Setup()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode||PrefabStageUtility.GetCurrentPrefabStage()!=null){Debug.LogWarning("Exit Play Mode and Prefab Mode first.");return;}
  const string path="Assets/Models/RJ45TabletopWorkstation.prefab";
  var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);var cutter=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/crimping_tool.prefab");
  var shader=Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Unlit/Color");
  if(asset==null||cutter==null||shader==null){Debug.LogError("Missing workstation or Assets/Models/crimping_tool.prefab.");return;}
  if(asset.GetComponentInChildren<RJ45Crimping>(true)!=null){Debug.Log("Crimping already configured.");return;}
  if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
  string backup="Library/ARDENTRJ45Backups/Crimp-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");Directory.CreateDirectory(backup);File.Copy(path,Path.Combine(backup,"RJ45TabletopWorkstation.prefab"));
  var root=PrefabUtility.LoadPrefabContents(path);
  try
  {
   var board=root.GetComponentInChildren<RJ45WireArrangement>(true);
   var prep=root.GetComponentInChildren<RJ45CablePreparation>(true);
   if(board==null||prep==null||prep.cuttingPoint==null)throw new InvalidOperationException("Set up the overlapping cable first.");
   var insertion=root.GetComponentInChildren<RJ45ConnectorInsertion>(true);
   if(insertion==null)throw new InvalidOperationException("Add connector insertion first.");
   var trim=board.gameObject.AddComponent<RJ45Crimping>();trim.board=board;trim.insertion=insertion;
   var holder=new GameObject("CrimperSelectable").transform;holder.SetParent(board.transform,false);
   var model=(GameObject)PrefabUtility.InstantiatePrefab(cutter);PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);model.transform.SetParent(holder,false);model.transform.localPosition=Vector3.zero;
   foreach(var script in model.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(script);
   foreach(var collider in model.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
   foreach(var body in model.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(body);
   var initialBounds=BoundsIn(holder);
   if(initialBounds.size.z>initialBounds.size.x && initialBounds.size.z>initialBounds.size.y)model.transform.localRotation=Quaternion.Euler(90,0,0)*model.transform.localRotation;
   var bounds=BoundsIn(holder);float longest=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));if(longest<.00001f)throw new InvalidOperationException("Cutter mesh is empty.");
   model.transform.localScale*=.65f/longest;bounds=BoundsIn(holder);model.transform.localPosition-=bounds.center;
   var pick=holder.gameObject.AddComponent<BoxCollider>();pick.size=Vector3.Max(BoundsIn(holder).size,new Vector3(.30f,.30f,.15f));
   holder.localPosition=new Vector3(.95f,.20f,-.40f);trim.tool=holder;trim.toolCollider=pick;
   var point=new GameObject("CrimpPoint").transform;point.SetParent(holder,false);trim.crimpPoint=point;
   string folder=AssetDatabase.GenerateUniqueAssetPath("Assets/Models/RJ45CrimpingMaterials");AssetDatabase.CreateFolder("Assets/Models",Path.GetFileName(folder));
   var yellow=new Material(shader);yellow.color=new Color(1,.8f,.1f);AssetDatabase.CreateAsset(yellow,folder+"/CrimpGuide.mat");
   var dot=GameObject.CreatePrimitive(PrimitiveType.Sphere);dot.name="ContactMarker";dot.transform.SetParent(point,false);dot.transform.localPosition=new Vector3(0,0,-.16f);dot.transform.localScale=Vector3.one*.055f;UnityEngine.Object.DestroyImmediate(dot.GetComponent<Collider>());dot.GetComponent<Renderer>().sharedMaterial=yellow;
   Label("ToolLabel",holder,"CRIMPER\nDrag onto connector",new Vector3(0,-.45f,-.15f),.85f);
   var guide=new GameObject("CrimpTargetGuide").transform;guide.SetParent(insertion.connector,false);trim.targetGuide=guide.gameObject;
   var line=new GameObject("CrimpOutline").AddComponent<LineRenderer>();line.transform.SetParent(guide,false);line.useWorldSpace=false;line.positionCount=5;line.widthMultiplier=.012f;line.sharedMaterial=yellow;
   line.SetPositions(new[]{new Vector3(-.21f,-.64f,-.19f),new Vector3(.21f,-.64f,-.19f),new Vector3(.21f,.08f,-.19f),new Vector3(-.21f,.08f,-.19f),new Vector3(-.21f,-.64f,-.19f)});
   var preview=insertion.inspectionView.transform.Find("EnlargedConnector");
   if(preview==null)throw new InvalidOperationException("Inspection connector is missing.");
   trim.contactsAndClamps=new Transform[18];
   var clampMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));clampMaterial.color=new Color(.45f,.65f,.75f);AssetDatabase.CreateAsset(clampMaterial,folder+"/JacketClamp.mat");
   int index=0;
   foreach(var connector in new[]{insertion.connector,preview})
   {
    for(int i=1;i<=8;i++)
    {
     var contact=connector.Find("Contact"+i);if(contact==null)throw new InvalidOperationException("Missing contact "+i);
     trim.contactsAndClamps[index++]=contact;
    }
    var clamp=GameObject.CreatePrimitive(PrimitiveType.Cube);clamp.name="JacketClamp";clamp.transform.SetParent(connector,false);clamp.transform.localPosition=new Vector3(0,-.58f,-.17f);clamp.transform.localScale=new Vector3(.26f,.07f,.02f);
    UnityEngine.Object.DestroyImmediate(clamp.GetComponent<Collider>());clamp.GetComponent<Renderer>().sharedMaterial=clampMaterial;
    trim.contactsAndClamps[index++]=clamp.transform;
   }
   holder.gameObject.SetActive(false);guide.gameObject.SetActive(false);
   PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();
   Debug.Log("Crimping ready. After insertion, inspect the wires, then drag the crimper marker onto the connector and release. The one-piece tool uses a pressing motion; contacts and clamps move in both views. Backup: "+Path.GetFullPath(backup));
  }
  finally{PrefabUtility.UnloadPrefabContents(root);}
 }
 private static Bounds BoundsIn(Transform root)
 {
  bool any=false;var result=new Bounds();foreach(var renderer in root.GetComponentsInChildren<Renderer>(true)){
   var b=renderer.localBounds;for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2){var p=root.InverseTransformPoint(renderer.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));if(!any){result=new Bounds(p,Vector3.zero);any=true;}else result.Encapsulate(p);}
  }return result;
 }
 private static void Label(string name,Transform parent,string value,Vector3 position,float width)
 {
  var text=new GameObject(name).AddComponent<TextMeshPro>();text.transform.SetParent(parent,false);text.transform.localPosition=position;text.rectTransform.sizeDelta=new Vector2(width,.3f);text.font=TMP_Settings.defaultFontAsset;text.fontSize=1.1f;text.color=Color.white;text.alignment=TextAlignmentOptions.Center;text.text=value;text.raycastTarget=false;
 }
}
#endif

