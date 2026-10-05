#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RJ45TesterSetup
{
 [MenuItem("ARDENT/RJ45/Add Tester Demonstration")]
 public static void Setup()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode||PrefabStageUtility.GetCurrentPrefabStage()!=null){Debug.LogWarning("Exit Play Mode and Prefab Mode first.");return;}
  const string path="Assets/Models/RJ45TabletopWorkstation.prefab";
  var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);var modelAsset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/lan_tester.prefab");
  var shader=Shader.Find("Universal Render Pipeline/Unlit");
  if(asset==null||modelAsset==null||shader==null||asset.GetComponentInChildren<RJ45Crimping>(true)==null){Debug.LogError("Need the crimping workstation and Assets/Models/lan_tester.prefab.");return;}
  if(asset.GetComponentInChildren<RJ45CableTester>(true)!=null){Debug.Log("Tester demonstration already configured.");return;}
  if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
  string backup="Library/ARDENTRJ45Backups/Tester-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");Directory.CreateDirectory(backup);File.Copy(path,Path.Combine(backup,"RJ45TabletopWorkstation.prefab"));
  var root=PrefabUtility.LoadPrefabContents(path);
  try
  {
   var board=root.GetComponentInChildren<RJ45WireArrangement>(true);
   var demo=board.gameObject.AddComponent<RJ45CableTester>();demo.board=board;demo.crimping=root.GetComponentInChildren<RJ45Crimping>(true);
   var holder=new GameObject("LANTesterDemonstration").transform;holder.SetParent(board.transform,false);
   var modelRoot=new GameObject("TesterModel").transform;modelRoot.SetParent(holder,false);
   var model=(GameObject)PrefabUtility.InstantiatePrefab(modelAsset);PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);model.transform.SetParent(modelRoot,false);model.transform.localPosition=Vector3.zero;
   foreach(var script in model.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(script);
   foreach(var col in model.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(col);
   foreach(var body in model.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(body);
   var b=BoundsIn(modelRoot);
   if(b.size.z>b.size.x&&b.size.z>b.size.y)model.transform.localRotation=Quaternion.Euler(90,0,0)*model.transform.localRotation;
   b=BoundsIn(modelRoot);float longest=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));if(longest<.00001f)throw new InvalidOperationException("Tester mesh is empty.");
   model.transform.localScale*=.70f/longest;b=BoundsIn(modelRoot);model.transform.localPosition-=b.center;
   var pick=modelRoot.gameObject.AddComponent<BoxCollider>();pick.size=Vector3.Max(BoundsIn(modelRoot).size,new Vector3(.4f,.4f,.18f));demo.testerCollider=pick;
   modelRoot.localPosition=new Vector3(0,.48f,0);
   string folder=AssetDatabase.GenerateUniqueAssetPath("Assets/Models/RJ45TesterMaterials");AssetDatabase.CreateFolder("Assets/Models",Path.GetFileName(folder));
   var lightMaterial=new Material(shader);lightMaterial.color=new Color(.08f,.14f,.10f);AssetDatabase.CreateAsset(lightMaterial,folder+"/Indicator.mat");
   demo.mainLights=new Renderer[8];demo.remoteLights=new Renderer[8];
   for(int i=0;i<8;i++)for(int row=0;row<2;row++)
   {
    var lamp=GameObject.CreatePrimitive(PrimitiveType.Sphere);lamp.name=(row==0?"Main":"Remote")+(i+1);lamp.transform.SetParent(holder,false);
    lamp.transform.localPosition=new Vector3((i-3.5f)*.085f,-.12f-row*.22f,-.09f);lamp.transform.localScale=Vector3.one*.055f;
    UnityEngine.Object.DestroyImmediate(lamp.GetComponent<Collider>());var renderer=lamp.GetComponent<Renderer>();renderer.sharedMaterial=lightMaterial;
    if(row==0)demo.mainLights[i]=renderer;else demo.remoteLights[i]=renderer;
    Label("Pin"+(i+1),holder,(i+1).ToString(),new Vector3((i-3.5f)*.085f,-.04f-row*.22f,-.09f),.085f);
   }
   Label("MainLabel",holder,"MAIN",new Vector3(-.53f,-.12f,-.09f),.36f);
   Label("RemoteLabel",holder,"REMOTE",new Vector3(-.53f,-.34f,-.09f),.45f);
   Label("TestTitle",holder,"SIMULATED TEST\nTap tester to start",new Vector3(0,.99f,-.09f),1.2f);
   holder.localPosition=new Vector3(.95f,-.15f,-.35f);demo.testerVisual=holder.gameObject;holder.gameObject.SetActive(false);
   PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();
   Debug.Log("Tester demonstration added. After crimping, tap the tester, watch pins 1-8, then use the existing completion button. Backup: "+Path.GetFullPath(backup));
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

