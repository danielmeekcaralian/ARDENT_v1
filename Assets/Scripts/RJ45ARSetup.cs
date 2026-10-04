#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class RJ45ARSetup
{
    [MenuItem("ARDENT/RJ45/Set Up Tabletop AR")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage()!=null)
        { Debug.LogWarning("Exit Play Mode and Prefab Mode first."); return; }
        const string lessonPath="Assets/Scripts/COC2_L5.asset", activityPath="Assets/Scripts/COC2_L5_AR.asset";
        const string scenePath="Assets/Scenes/ARScene.unity", prefabPath="Assets/Models/RJ45TabletopWorkstation.prefab";
        var lesson=AssetDatabase.LoadAssetAtPath<LessonData>(lessonPath);
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/RJ45BendingBoard.prefab");
        if(lesson==null || source==null || source.GetComponent<RJ45WireArrangement>()==null || TMP_Settings.defaultFontAsset==null)
        { Debug.LogError("Missing Splicing lesson, RJ45BendingBoard prefab, or TMP font. Create the eight-wire prototype first."); return; }
        var activity=AssetDatabase.LoadAssetAtPath<ARActivityData>(activityPath);
        foreach(var guid in AssetDatabase.FindAssets("t:ARActivityData"))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);
            var other=AssetDatabase.LoadAssetAtPath<ARActivityData>(path);
            if(other!=null && other.activityID=="9" && path!=activityPath)
            { Debug.LogError("Activity ID 9 is already used by "+path+". No changes made."); return; }
        }
        if(lesson.arActivity!=null && lesson.arActivity!=activity)
        { Debug.LogError("Splicing already references another AR activity. Review that assignment first."); return; }
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string backup=Path.Combine("Library/ARDENTRJ45Backups",DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));
        Directory.CreateDirectory(backup);
        foreach(var path in new[]{lessonPath,activityPath,scenePath,prefabPath})
            if(File.Exists(path)) File.Copy(path,Path.Combine(backup,Path.GetFileName(path)),false);
        var scene=EditorSceneManager.OpenScene(scenePath);
        var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        var controller=all.Select(t=>t.GetComponent<ARSceneController>()).FirstOrDefault(c=>c!=null);
        var instructions=all.Where(t=>t.name=="InstructionsText").Select(t=>t.GetComponent<TMP_Text>()).FirstOrDefault(t=>t!=null);
        if(controller==null || instructions==null || instructions.canvas==null)
        { Debug.LogError("ARScene needs ARSceneController and InstructionsText on a Canvas. No assets updated."); return; }
        var workstation=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if(workstation==null)
        {
            var root=new GameObject("RJ45TabletopWorkstation");
            try
            {
                var child=(GameObject)PrefabUtility.InstantiatePrefab(source);
                child.transform.SetParent(root.transform,false);
                child.transform.localPosition=Vector3.zero; child.transform.localRotation=Quaternion.Euler(90,0,0);
                child.transform.localScale=Vector3.one*.15f;
                var board=child.GetComponent<RJ45WireArrangement>();
                board.fitOrthographicCamera=false; board.interactionCamera=null; board.instructionsText=null; board.standardText=null;
                board.standardAButton=null;board.standardBButton=null;board.restartButton=null;
                workstation=PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        if(workstation.GetComponentInChildren<RJ45WireArrangement>(true)==null)
        { Debug.LogError("Existing workstation lacks a wire board."); return; }
        if(activity==null) { activity=ScriptableObject.CreateInstance<ARActivityData>(); AssetDatabase.CreateAsset(activity,activityPath); }
        Undo.RecordObject(activity,"Configure RJ45 AR");
        activity.activityID="9"; activity.activityTitle="RJ45 Cable Termination"; activity.activityType=ARActivityType.RJ45Termination;
        activity.instruction="Scan a table and tap to place the workstation. Drag wire tips into the numbered slots.";
        activity.requirePlanePlacement=true;activity.allowMovement=false;activity.allowRotation=false;activity.allowScaling=false;
        activity.availableObjects=new[]{new ARObjectData{prefab=workstation}};
        activity.availableObjects[0].RefreshLibraryIdentity(); EditorUtility.SetDirty(activity);
        Undo.RecordObject(lesson,"Connect Splicing AR"); lesson.hasARActivity=true;lesson.arActivity=activity; EditorUtility.SetDirty(lesson);
        var session=controller.GetComponent<RJ45ARSession>();if(session==null) session=Undo.AddComponent<RJ45ARSession>(controller.gameObject);
        Undo.RecordObject(session,"Wire RJ45 controls");session.instructionsText=instructions;
        if(session.toolbar==null)
        {
            var panel=new GameObject("RJ45ProcedurePanel",typeof(RectTransform));panel.transform.SetParent(instructions.canvas.rootCanvas.transform,false);
            var rect=panel.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.12f,.70f);rect.anchorMax=new Vector2(.88f,.82f);rect.offsetMin=rect.offsetMax=Vector2.zero;
            session.toolbar=panel;
            session.standardText=Text("StandardText",panel.transform,"Standard: T568B",new Vector2(0,.60f),Vector2.one,20);
            session.standardAButton=Button("T568AButton",panel.transform,"T568A",0,.23f);
            session.standardBButton=Button("T568BButton",panel.transform,"T568B",.255f,.485f);
            session.restartButton=Button("RestartWiresButton",panel.transform,"Restart",.51f,.74f);
            session.repositionButton=Button("RepositionButton",panel.transform,"Reposition",.765f,1);
            panel.SetActive(false);
        }
        string[] disabled={"EditButton","DeleteButton","ResetButton","AdjustButton","InventoryButton"};
        session.ordinaryButtons=all.Where(t=>disabled.Contains(t.name)).Select(t=>t.GetComponent<Button>()).Where(b=>b!=null).ToArray();
        EditorUtility.SetDirty(session);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Selection.activeGameObject=session.toolbar;
        Debug.Log("RJ45 tabletop AR ready: activity ID 9, COC2_L5_AR, linked to Splicing. Enter through the lesson to test. This prototype does not award completion or save progress. Backup: "+Path.GetFullPath(backup));
    }
    private static TMP_Text Text(string name,Transform parent,string value,Vector2 min,Vector2 max,float size)
    {
        var text=new GameObject(name,typeof(RectTransform)).AddComponent<TextMeshProUGUI>();text.transform.SetParent(parent,false);
        text.rectTransform.anchorMin=min;text.rectTransform.anchorMax=max;text.rectTransform.offsetMin=text.rectTransform.offsetMax=Vector2.zero;
        text.font=TMP_Settings.defaultFontAsset;text.text=value;text.fontSize=size;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;return text;
    }
    private static Button Button(string name,Transform parent,string value,float min,float max)
    {
        var obj=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));obj.transform.SetParent(parent,false);
        var rect=obj.GetComponent<RectTransform>();rect.anchorMin=new Vector2(min,0);rect.anchorMax=new Vector2(max,.58f);rect.offsetMin=rect.offsetMax=Vector2.zero;
        obj.GetComponent<Image>().color=new Color(.22f,.32f,.5f);var button=obj.GetComponent<Button>();button.targetGraphic=obj.GetComponent<Image>();
        Text("Label",obj.transform,value,Vector2.zero,Vector2.one,20);return button;
    }
}
#endif
