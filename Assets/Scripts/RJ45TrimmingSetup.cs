#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RJ45TrimmingSetup
{
 [MenuItem("ARDENT/RJ45/Add Wire Trimming")]
 public static void Setup()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode||PrefabStageUtility.GetCurrentPrefabStage()!=null){Debug.LogWarning("Exit Play Mode and Prefab Mode first.");return;}
  const string path="Assets/Models/RJ45TabletopWorkstation.prefab";
  var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);var cutter=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/pliers_wire_cutter.prefab");
  var shader=Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Unlit/Color");
  if(asset==null||cutter==null||shader==null){Debug.LogError("Missing workstation or Assets/Models/pliers_wire_cutter.prefab.");return;}
  if(asset.GetComponentInChildren<RJ45WireTrimming>(true)!=null){Debug.Log("Wire trimming already configured.");return;}
  if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
  string backup="Library/ARDENTRJ45Backups/Trim-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");Directory.CreateDirectory(backup);File.Copy(path,Path.Combine(backup,"RJ45TabletopWorkstation.prefab"));
  var root=PrefabUtility.LoadPrefabContents(path);
  try
  {
   var board=root.GetComponentInChildren<RJ45WireArrangement>(true);
   var prep=root.GetComponentInChildren<RJ45CablePreparation>(true);
   if(board==null||prep==null||prep.cuttingPoint==null)throw new InvalidOperationException("Set up the overlapping cable first.");
   var trim=board.gameObject.AddComponent<RJ45WireTrimming>();trim.board=board;
   var holder=new GameObject("WireCutterSelectable").transform;holder.SetParent(board.transform,false);
   var model=(GameObject)PrefabUtility.InstantiatePrefab(cutter);PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);model.transform.SetParent(holder,false);model.transform.localPosition=Vector3.zero;
   foreach(var script in model.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(script);
   foreach(var collider in model.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
   foreach(var body in model.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(body);
   var bounds=BoundsIn(holder);float longest=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));if(longest<.00001f)throw new InvalidOperationException("Cutter mesh is empty.");
   model.transform.localScale*=.65f/longest;bounds=BoundsIn(holder);model.transform.localPosition-=bounds.center;
   var pick=holder.gameObject.AddComponent<BoxCollider>();pick.size=Vector3.Max(BoundsIn(holder).size,new Vector3(.30f,.30f,.15f));
   holder.localPosition=new Vector3(-.95f,.05f,-.36f);trim.cutterVisual=holder.gameObject;trim.cutterCollider=pick;
   var point=new GameObject("CuttingPoint").transform;point.SetParent(holder,false);trim.cuttingPoint=point;
   string folder=AssetDatabase.GenerateUniqueAssetPath("Assets/Models/RJ45TrimmingMaterials");AssetDatabase.CreateFolder("Assets/Models",Path.GetFileName(folder));
   var yellow=new Material(shader);yellow.color=new Color(1,.8f,.1f);AssetDatabase.CreateAsset(yellow,folder+"/CutGuide.mat");
   var dot=GameObject.CreatePrimitive(PrimitiveType.Sphere);dot.name="ContactMarker";dot.transform.SetParent(point,false);dot.transform.localPosition=new Vector3(0,0,-.16f);dot.transform.localScale=Vector3.one*.055f;UnityEngine.Object.DestroyImmediate(dot.GetComponent<Collider>());dot.GetComponent<Renderer>().sharedMaterial=yellow;
   Label("ToolLabel",holder,"WIRE CUTTER\nDrag across line",new Vector3(0,-.45f,-.15f),.85f);
   var guide=new GameObject("WireTrimmingGuide").transform;guide.SetParent(board.transform,false);trim.cuttingGuide=guide.gameObject;
   var line=new GameObject("CutLine").AddComponent<LineRenderer>();line.transform.SetParent(guide,false);line.useWorldSpace=false;line.positionCount=2;line.SetPosition(0,new Vector3(-.48f,.30f,-.18f));line.SetPosition(1,new Vector3(.48f,.30f,-.18f));line.widthMultiplier=.012f;line.sharedMaterial=yellow;
   Label("CutLabel",guide,"<-- TRIM HERE -->",new Vector3(0,.78f,-.15f),1.4f);
   holder.gameObject.SetActive(false);guide.gameObject.SetActive(false);
   PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();
   Debug.Log("Wire trimming ready. Correct the order, wait for alignment, then drag the cutter across the line and release. Backup: "+Path.GetFullPath(backup));
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
