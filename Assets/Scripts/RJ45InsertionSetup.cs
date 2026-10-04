#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class RJ45InsertionSetup
{
 [MenuItem("ARDENT/RJ45/Add Connector Insertion")]
 public static void Setup()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode||PrefabStageUtility.GetCurrentPrefabStage()!=null){Debug.LogWarning("Exit Play Mode and Prefab Mode first.");return;}
  const string path="Assets/Models/RJ45TabletopWorkstation.prefab";
  var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);var shader=Shader.Find("Universal Render Pipeline/Lit");
  if(asset==null||shader==null||TMP_Settings.defaultFontAsset==null||asset.GetComponentInChildren<RJ45WireTrimming>(true)==null){Debug.LogError("Requires the trimming workstation, URP Lit shader and TMP font.");return;}
  if(asset.GetComponentInChildren<RJ45ConnectorInsertion>(true)!=null){Debug.Log("Connector insertion already configured.");return;}
  if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
  string backup="Library/ARDENTRJ45Backups/Insert-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");Directory.CreateDirectory(backup);File.Copy(path,Path.Combine(backup,"RJ45TabletopWorkstation.prefab"));
  var root=PrefabUtility.LoadPrefabContents(path);
  try
  {
   var board=root.GetComponentInChildren<RJ45WireArrangement>(true);var trim=root.GetComponentInChildren<RJ45WireTrimming>(true);
   string folder=AssetDatabase.GenerateUniqueAssetPath("Assets/Models/RJ45ConnectorMaterials");AssetDatabase.CreateFolder("Assets/Models",Path.GetFileName(folder));
   var clear=Mat(shader,folder,"ClearShell",new Color(.7f,.9f,1,.22f));
   clear.SetFloat("_Surface",1);clear.SetFloat("_Blend",0);clear.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);clear.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);clear.SetFloat("_ZWrite",0);clear.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");clear.SetOverrideTag("RenderType","Transparent");clear.renderQueue=(int)RenderQueue.Transparent;EditorUtility.SetDirty(clear);
   var rim=Mat(shader,folder,"Rim",new Color(.5f,.75f,.85f));var gold=Mat(shader,folder,"Contacts",new Color(1,.65f,.15f));var white=Mat(shader,folder,"White",Color.white);var jacket=Mat(shader,folder,"Jacket",new Color(.16f,.23f,.34f));
   var green=Mat(Shader.Find("Universal Render Pipeline/Unlit"),folder,"Target",new Color(.2f,1,.65f));
   var plug=new GameObject("RJ45ConnectorPlaceholder");plug.transform.SetParent(board.transform,false);
   // Open rear at local Y=-.64, front wire stops at Y=0. Clip is on local +Z (underside).
   Cube("LeftWall",plug.transform,new Vector3(-.17f,-.29f,0),new Vector3(.02f,.70f,.32f),clear);
   Cube("RightWall",plug.transform,new Vector3(.17f,-.29f,0),new Vector3(.02f,.70f,.32f),clear);
   Cube("TopWindow",plug.transform,new Vector3(0,-.29f,-.16f),new Vector3(.34f,.70f,.015f),clear);
   Cube("Bottom",plug.transform,new Vector3(0,-.29f,.16f),new Vector3(.34f,.70f,.015f),clear);
   Cube("FrontStop",plug.transform,new Vector3(0,.035f,0),new Vector3(.34f,.02f,.32f),clear);
   Cube("Clip",plug.transform,new Vector3(0,-.28f,.20f),new Vector3(.075f,.34f,.03f),rim);
   for(int i=0;i<8;i++)Cube("Contact"+(i+1),plug.transform,new Vector3((i-3.5f)*.034f,.005f,-.04f),new Vector3(.020f,.065f,.025f),gold);
   var pick=plug.AddComponent<BoxCollider>();pick.center=new Vector3(0,-.29f,0);pick.size=new Vector3(.40f,.74f,.36f);
   PrefabUtility.SaveAsPrefabAsset(plug,AssetDatabase.GenerateUniqueAssetPath("Assets/Models/RJ45ConnectorPlaceholder.prefab"));
   plug.transform.localPosition=new Vector3(.95f,.55f,-.12f);
   var insertion=board.gameObject.AddComponent<RJ45ConnectorInsertion>();insertion.board=board;insertion.trimming=trim;insertion.connector=plug.transform;insertion.connectorCollider=pick;
   var target=new GameObject("ConnectorSeatTarget").transform;target.SetParent(board.transform,false);target.localPosition=new Vector3(0,trim.cutY,-.12f);insertion.target=target;
   var guide=new GameObject("ConnectorTargetGuide");guide.transform.SetParent(target,false);insertion.targetGuide=guide;
   var line=guide.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=5;line.widthMultiplier=.012f;line.sharedMaterial=green;
   line.SetPositions(new[]{new Vector3(-.19f,-.64f,-.18f),new Vector3(.19f,-.64f,-.18f),new Vector3(.19f,.06f,-.18f),new Vector3(-.19f,.06f,-.18f),new Vector3(-.19f,-.64f,-.18f)});
   var inspection=new GameObject("ConnectorInspectionView").transform;inspection.SetParent(board.transform,false);inspection.localPosition=new Vector3(-.95f,.46f,-.35f);inspection.localScale=Vector3.one*1.7f;insertion.inspectionView=inspection.gameObject;
   var copy=UnityEngine.Object.Instantiate(plug,inspection);copy.name="EnlargedConnector";copy.transform.localPosition=Vector3.zero;copy.transform.localRotation=Quaternion.identity;copy.transform.localScale=Vector3.one;UnityEngine.Object.DestroyImmediate(copy.GetComponent<Collider>());
   insertion.inspectionWires=new Renderer[8];insertion.inspectionStripes=new GameObject[8];
   for(int i=0;i<8;i++)
   {
    var conductor=new GameObject("Pin"+(i+1)+"Wire").transform;conductor.SetParent(inspection,false);conductor.localPosition=new Vector3((i-3.5f)*.034f,0,0);
    var wire=GameObject.CreatePrimitive(PrimitiveType.Cylinder);wire.name="Insulation";wire.transform.SetParent(conductor,false);wire.transform.localPosition=new Vector3(0,-.285f,0);wire.transform.localScale=new Vector3(.024f,.285f,.024f);UnityEngine.Object.DestroyImmediate(wire.GetComponent<Collider>());insertion.inspectionWires[i]=wire.GetComponent<Renderer>();
    var stripes=new GameObject("WhiteStripes").transform;stripes.SetParent(conductor,false);insertion.inspectionStripes[i]=stripes.gameObject;
    for(int n=0;n<6;n++)Cube("Stripe",stripes,new Vector3(0,-.05f-n*.085f,-.013f),new Vector3(.024f,.022f,.004f),white);
    Label("PinLabel",inspection,(i+1).ToString(),new Vector3((i-3.5f)*.034f,.12f,-.18f),.045f,.55f);
   }
   Cube("JacketInsideRear",inspection,new Vector3(0,-.68f,0),new Vector3(.31f,.20f,.30f),jacket);
   Label("InspectionTitle",inspection,"INSPECTION VIEW",new Vector3(0,.25f,-.20f),.85f,.9f);
   Label("Orientation",inspection,"1 to 8 | clip underneath",new Vector3(0,-.88f,-.20f),1.1f,.65f);
   plug.SetActive(false);guide.SetActive(false);inspection.gameObject.SetActive(false);
   PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();
   Debug.Log("Connector insertion added. Finish trimming, drag the connector onto the outline, then inspect the enlarged view. No completion is awarded. Backup: "+Path.GetFullPath(backup));
  }
  finally{PrefabUtility.UnloadPrefabContents(root);}
 }
 private static Material Mat(Shader shader,string folder,string name,Color color){var m=new Material(shader);m.color=color;AssetDatabase.CreateAsset(m,folder+"/"+name+".mat");return m;}
 private static GameObject Cube(string name,Transform parent,Vector3 position,Vector3 size,Material material)
 {
  var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=size;UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());obj.GetComponent<Renderer>().sharedMaterial=material;return obj;
 }
 private static void Label(string name,Transform parent,string value,Vector3 position,float width,float size)
 {
  var text=new GameObject(name).AddComponent<TextMeshPro>();text.transform.SetParent(parent,false);text.transform.localPosition=position;text.rectTransform.sizeDelta=new Vector2(width,.2f);text.font=TMP_Settings.defaultFontAsset;text.fontSize=size;text.color=Color.white;text.alignment=TextAlignmentOptions.Center;text.text=value;text.raycastTarget=false;
 }
}
#endif
