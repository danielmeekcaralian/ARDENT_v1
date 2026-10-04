#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class RJ45FlexibleWireSetup
{
 [MenuItem("ARDENT/RJ45/Create Bendable Wire Test")]
 public static void Create()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage()!=null) { Debug.LogWarning("Exit Play Mode and Prefab Mode first."); return; }
  if(TMP_Settings.defaultFontAsset==null) { Debug.LogError("Import TMP Essential Resources first."); return; }
  var shader=Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
  if(shader==null) { Debug.LogError("A supported lit shader is required."); return; }
  if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  if(!AssetDatabase.IsValidFolder("Assets/Models")) AssetDatabase.CreateFolder("Assets","Models");
  var folder=AssetDatabase.GenerateUniqueAssetPath("Assets/Models/RJ45FlexibleWireMaterials");
  AssetDatabase.CreateFolder("Assets/Models",System.IO.Path.GetFileName(folder));
  var orange=Mat(shader,folder,"Orange",new Color(1,.38f,.04f));
  var white=Mat(shader,folder,"White",Color.white);
  var gray=Mat(shader,folder,"Jacket",new Color(.18f,.23f,.3f));
  var green=Mat(shader,folder,"Target",new Color(.15f,.8f,.5f));
  var camera=new GameObject("Main Camera",typeof(Camera)).GetComponent<Camera>();
  camera.tag="MainCamera"; camera.transform.position=new Vector3(0,0,-8); camera.orthographic=true; camera.orthographicSize=1.85f;
  camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.04f,.055f,.085f);
  var light=new GameObject("Wire Light",typeof(Light)).GetComponent<Light>(); light.type=LightType.Directional; light.intensity=1.3f; light.transform.rotation=Quaternion.Euler(25,-25,0);
  RenderSettings.ambientLight=new Color(.4f,.4f,.4f);
  var wire=new GameObject("BendableWire",typeof(MeshFilter),typeof(MeshRenderer)).AddComponent<RJ45FlexibleWire>();
  wire.interactionCamera=camera; wire.GetComponent<MeshRenderer>().sharedMaterials=new[]{orange,white};
  var tip=Primitive("DragTip",PrimitiveType.Sphere,wire.transform,wire.initialTip,new Vector3(.15f,.15f,.15f),orange);
  // Generous invisible pick area; the visible tip stays small.
  tip.GetComponent<SphereCollider>().radius=1.15f;
  wire.tipHandle=tip.transform; wire.tipCollider=tip.GetComponent<Collider>();
  var target=Primitive("SnapTarget",PrimitiveType.Cube,wire.transform,new Vector3(-.7f,.65f,.09f),new Vector3(.24f,.24f,.05f),green);
  Object.DestroyImmediate(target.GetComponent<Collider>());
  var snap=new GameObject("TargetCenter").transform; snap.SetParent(wire.transform,false); snap.localPosition=new Vector3(-.7f,.65f,0); wire.snapTarget=snap;
  var jacket=Primitive("CableJacket",PrimitiveType.Cylinder,wire.transform,new Vector3(0,-1.08f,0),new Vector3(.18f,.28f,.18f),gray);
  Object.DestroyImmediate(jacket.GetComponent<Collider>());
  var canvas=new GameObject("PrototypeCanvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;
  var scaler=canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1280,720); scaler.matchWidthOrHeight=.5f;
  Text("Title",canvas.transform,"BENDABLE WIRE TEST",new Vector2(.04f,.92f),new Vector2(.96f,.99f),28);
  wire.instructionsText=Text("InstructionsText",canvas.transform,"",new Vector2(.04f,.79f),new Vector2(.96f,.91f),22);
  var button=new GameObject("ResetWireButton",typeof(RectTransform),typeof(Image),typeof(Button)); button.transform.SetParent(canvas.transform,false);
  var rect=button.GetComponent<RectTransform>(); rect.anchorMin=new Vector2(.35f,.025f); rect.anchorMax=new Vector2(.65f,.10f); rect.offsetMin=rect.offsetMax=Vector2.zero;
  button.GetComponent<Image>().color=new Color(.22f,.32f,.5f); button.GetComponent<Button>().targetGraphic=button.GetComponent<Image>();
  Text("Label",button.transform,"Reset wire",Vector2.zero,Vector2.one,24);
  UnityEventTools.AddPersistentListener(button.GetComponent<Button>().onClick,wire.ResetWire);
  new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule)).GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
  var feedback=wire.instructionsText;
  wire.interactionCamera=null; wire.instructionsText=null; wire.fitPrototypeCamera=false;
  PrefabUtility.SaveAsPrefabAsset(wire.gameObject,AssetDatabase.GenerateUniqueAssetPath("Assets/Models/RJ45FlexibleWirePrototype.prefab"));
  wire.interactionCamera=camera; wire.instructionsText=feedback; wire.fitPrototypeCamera=true;
  if(!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets","Scenes");
  var path=AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/RJ45_BendableWireTest.unity");
  EditorSceneManager.SaveScene(scene,path); AssetDatabase.SaveAssets(); Selection.activeGameObject=wire.gameObject;
  Debug.Log("Bendable wire test created. Press Play, drag the orange tip to the green target, then detach it. This is a single-wire interaction test.");
 }
 private static Material Mat(Shader shader,string folder,string name,Color color)
 {
  var material=new Material(shader); material.color=color; if(material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness",.3f);
  AssetDatabase.CreateAsset(material,folder+"/"+name+".mat"); return material;
 }
 private static GameObject Primitive(string name,PrimitiveType type,Transform parent,Vector3 position,Vector3 scale,Material material)
 {
  var obj=GameObject.CreatePrimitive(type); obj.name=name; obj.transform.SetParent(parent,false); obj.transform.localPosition=position; obj.transform.localScale=scale; obj.GetComponent<Renderer>().sharedMaterial=material; return obj;
 }
 private static TMP_Text Text(string name,Transform parent,string content,Vector2 min,Vector2 max,float size)
 {
  var text=new GameObject(name,typeof(RectTransform)).AddComponent<TextMeshProUGUI>(); text.transform.SetParent(parent,false);
  text.rectTransform.anchorMin=min; text.rectTransform.anchorMax=max; text.rectTransform.offsetMin=text.rectTransform.offsetMax=Vector2.zero;
  text.font=TMP_Settings.defaultFontAsset; text.fontSize=size; text.text=content; text.alignment=TextAlignmentOptions.Center; text.raycastTarget=false; return text;
 }
}
#endif
