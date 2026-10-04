#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RJ45OverlappingCableSetup
{
 [MenuItem("ARDENT/RJ45/Use Overlapping Cable Model")]
 public static void Setup()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode||PrefabStageUtility.GetCurrentPrefabStage()!=null){Debug.LogWarning("Exit Play Mode and Prefab Mode first.");return;}
  const string path="Assets/Models/RJ45TabletopWorkstation.prefab";
  var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
  var shader=Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard");
  if(asset==null||asset.GetComponentInChildren<RJ45CablePreparation>(true)==null||shader==null){Debug.LogError("Install the earlier cable preparation setup first.");return;}
  if(asset.GetComponentInChildren<RJ45CablePreparation>(true).cuttingPoint!=null){Debug.Log("Overlapping cable model is already configured.");return;}
  if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
  string backup="Library/ARDENTRJ45Backups/Overlap-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");Directory.CreateDirectory(backup);File.Copy(path,Path.Combine(backup,"RJ45TabletopWorkstation.prefab"));
  var root=PrefabUtility.LoadPrefabContents(path);
  try
  {
   var prep=root.GetComponentInChildren<RJ45CablePreparation>(true);var board=prep.board;
   // Work only on this workstation copy, preserving the earlier prototype prefab.
   if(PrefabUtility.IsAnyPrefabInstanceRoot(board.gameObject))PrefabUtility.UnpackPrefabInstance(board.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
   string folder=AssetDatabase.GenerateUniqueAssetPath("Assets/Models/RJ45OverlappingCableAssets");AssetDatabase.CreateFolder("Assets/Models",Path.GetFileName(folder));
   var material=new Material(shader);material.color=new Color(.16f,.23f,.34f);AssetDatabase.CreateAsset(material,folder+"/Jacket.mat");
   var guideMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Unlit/Color"));guideMaterial.color=new Color(1,.8f,.1f);AssetDatabase.CreateAsset(guideMaterial,folder+"/Guide.mat");
   if(prep.pairObjects!=null)foreach(var obj in prep.pairObjects)if(obj!=null)UnityEngine.Object.DestroyImmediate(obj);
   prep.pairObjects=new GameObject[0];prep.pairColliders=new Collider[0];
   if(prep.jacketCover!=null)UnityEngine.Object.DestroyImmediate(prep.jacketCover.gameObject);
   if(prep.dragGuide!=null)UnityEngine.Object.DestroyImmediate(prep.dragGuide);
   var originalJacket=board.transform.Find("CableJacket");if(originalJacket!=null)UnityEngine.Object.DestroyImmediate(originalJacket.gameObject);
   var parent=new GameObject("UnityCableBody").transform;parent.SetParent(board.transform,false);
   Pipe("Jacket_Main",parent,new Vector3(0,-.92f,-.12f),1.30f,material,folder);
   prep.jacketCover=Pipe("Jacket_Removable",parent,new Vector3(0,.07f,-.12f),.68f,material,folder);
   prep.stripY=-.27f;prep.crossingHalfWidth=.32f;prep.gestureRadius=.16f;
   var guide=new GameObject("SidewaysStripGuide").transform;guide.SetParent(board.transform,false);prep.dragGuide=guide.gameObject;
   var line=new GameObject("CrossingGuide").AddComponent<LineRenderer>();line.transform.SetParent(guide,false);line.useWorldSpace=false;line.positionCount=2;line.SetPosition(0,new Vector3(-.48f,-.27f,-.30f));line.SetPosition(1,new Vector3(.48f,-.27f,-.30f));line.widthMultiplier=.015f;line.sharedMaterial=guideMaterial;
   Label("Direction",guide,"<-- DRAG ACROSS -->",new Vector3(0,-.05f,-.35f),1.5f);
   prep.toolVisual.transform.localPosition=new Vector3(-1.05f,-.27f,-.36f);
   var point=new GameObject("CuttingPoint").transform;point.SetParent(prep.toolVisual.transform,false);prep.cuttingPoint=point;
   var dot=GameObject.CreatePrimitive(PrimitiveType.Sphere);dot.name="ContactMarker";dot.transform.SetParent(point,false);dot.transform.localPosition=new Vector3(0,0,-.16f);dot.transform.localScale=Vector3.one*.055f;
   UnityEngine.Object.DestroyImmediate(dot.GetComponent<Collider>());dot.GetComponent<Renderer>().sharedMaterial=guideMaterial;
   foreach(var label in prep.toolVisual.GetComponentsInChildren<TMP_Text>(true))label.text="WIRE STRIPPER\nDrag across cable";
   // Bases fit inside the cable mouth; shuffled tips produce crossing curves.
   int[] tips={5,2,7,0,3,6,1,4};
   for(int i=0;i<8;i++)
   {
    var wire=board.wires[i];wire.transform.localPosition=new Vector3((tips[i]-3.5f)*.35f,.12f,-.12f);
    wire.bendingTube.fixedBase=new Vector3((i%4-1.5f)*.044f,-.29f,-.12f+(i/4-.5f)*.044f);wire.bendingTube.radius=.020f;
    wire.pickCollider.transform.localScale=Vector3.one;
    board.pinSlots[i].localPosition=new Vector3((i-3.5f)*.43f,.65f,0);
    if(board.pinLabels[i]!=null)board.pinLabels[i].transform.localPosition=new Vector3((i-3.5f)*.43f,1.0f,-.05f);
   }
   PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();
   Debug.Log("Overlapping cable ready. Drag the actual stripper sideways, release beyond the opposite edge, then arrange eight wires. Your scene UI is unchanged. Backup: "+Path.GetFullPath(backup));
  }
  finally{PrefabUtility.UnloadPrefabContents(root);}
 }
 private static Transform Pipe(string name,Transform parent,Vector3 position,float length,Material material,string folder)
 {
  const int n=32;var vertices=new Vector3[n*4];var triangles=new System.Collections.Generic.List<int>();
  for(int ring=0;ring<4;ring++)for(int i=0;i<n;i++){float a=2*Mathf.PI*i/n;float radius=ring<2?.16f:.135f;vertices[ring*n+i]=new Vector3(Mathf.Cos(a)*radius,ring%2==0?-length/2:length/2,Mathf.Sin(a)*radius);}
  for(int i=0;i<n;i++){
   int j=(i+1)%n;
   Add(triangles,i,n+i,j);Add(triangles,n+i,n+j,j);
   Add(triangles,2*n+i,2*n+j,3*n+i);Add(triangles,3*n+i,2*n+j,3*n+j);
   Add(triangles,n+i,3*n+i,n+j);Add(triangles,n+j,3*n+i,3*n+j);
   Add(triangles,i,j,2*n+i);Add(triangles,j,2*n+j,2*n+i);
  }
  var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.triangles=triangles.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,folder+"/"+name+".asset");
  var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<Renderer>().sharedMaterial=material;return obj.transform;
 }
 private static void Add(System.Collections.Generic.List<int> list,int a,int b,int c){list.Add(a);list.Add(b);list.Add(c);}
 private static void Label(string name,Transform parent,string value,Vector3 position,float width)
 {
  var text=new GameObject(name).AddComponent<TextMeshPro>();text.transform.SetParent(parent,false);text.transform.localPosition=position;text.rectTransform.sizeDelta=new Vector2(width,.25f);text.font=TMP_Settings.defaultFontAsset;text.fontSize=1.1f;text.color=Color.white;text.alignment=TextAlignmentOptions.Center;text.text=value;text.raycastTarget=false;
 }
}
#endif
