#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class NetworkCableARSetup
{
    private const string LessonPath = "Assets/Scripts/COC2_L6.asset";
    private const string ActivityPath = "Assets/Scripts/COC2_L6_AR.asset";
    private const string ScenePath = "Assets/Scenes/ARScene.unity";
    private const string PrefabPath = "Assets/Models/NetworkCableWorkstation.prefab";
    private const string MaterialFolder = "Assets/Models/NetworkCableMaterials";

    [MenuItem("ARDENT/Network Cables/Set Up Complete AR Activity")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            Debug.LogWarning("Exit Play Mode and Prefab Mode before setting up Network Cables AR.");
            return;
        }

        var lesson = AssetDatabase.LoadAssetAtPath<LessonData>(LessonPath);
        var activity = AssetDatabase.LoadAssetAtPath<ARActivityData>(ActivityPath);
        if (lesson == null || activity == null || TMP_Settings.defaultFontAsset == null)
        {
            Debug.LogError("Missing COC2_L6, COC2_L6_AR, or the default TMP font. No assets were changed.");
            return;
        }
        foreach (string guid in AssetDatabase.FindAssets("t:ARActivityData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var other = AssetDatabase.LoadAssetAtPath<ARActivityData>(path);
            if (other != null && other.activityID == "10" && path != ActivityPath)
            {
                Debug.LogError("Activity ID 10 is already used by " + path + ". No assets were changed.");
                return;
            }
        }
        if (lesson.arActivity != null && lesson.arActivity != activity)
        {
            Debug.LogError("COC2_L6 references a different AR activity. Review that assignment first.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string backup = Path.Combine("Library/ARDENTNetworkCableBackups", DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));
        Directory.CreateDirectory(backup);
        foreach (string path in new[] { LessonPath, ActivityPath, ScenePath, PrefabPath })
            if (File.Exists(path)) File.Copy(path, Path.Combine(backup, Path.GetFileName(path)), false);

        EnsureFolder(MaterialFolder);
        var materials = CreateMaterials();
        var workstationPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (workstationPrefab == null)
            workstationPrefab = BuildWorkstation(materials);
        if (workstationPrefab == null || workstationPrefab.GetComponent<NetworkCableWorkstation>() == null)
        {
            Debug.LogError("Could not create a valid Network Cable workstation.");
            return;
        }

        // Creating/importing the generated prefab can invalidate cached ScriptableObject
        // references in some Unity editor versions. Reload them before editing and save
        // them before opening another scene.
        activity = AssetDatabase.LoadAssetAtPath<ARActivityData>(ActivityPath);
        lesson = AssetDatabase.LoadAssetAtPath<LessonData>(LessonPath);
        if (activity == null || lesson == null)
        {
            Debug.LogError("COC2_L6 or COC2_L6_AR could not be reloaded after creating the workstation.");
            return;
        }

        Undo.RecordObject(activity, "Configure Network Cables AR");
        activity.activityID = "10";
        activity.activityTitle = "Network Cables";
        activity.activityType = ARActivityType.NetworkCables;
        activity.instruction = "Place the workstation, identify UTP, STP, coaxial, and fiber optic cables, then drag each connector to its matching port.";
        activity.requirePlanePlacement = true;
        activity.allowMovement = false;
        activity.allowRotation = false;
        activity.allowScaling = false;
        activity.availableObjects = new[] { new ARObjectData { prefab = workstationPrefab } };
        activity.availableObjects[0].RefreshLibraryIdentity();
        EditorUtility.SetDirty(activity);

        Undo.RecordObject(lesson, "Connect Network Cables AR");
        lesson.hasARActivity = true;
        lesson.arActivity = activity;
        EditorUtility.SetDirty(lesson);
        AssetDatabase.SaveAssets();

        var scene = EditorSceneManager.OpenScene(ScenePath);
        var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        var controller = all.Select(t => t.GetComponent<ARSceneController>()).FirstOrDefault(c => c != null);
        var instructions = all.Where(t => t.name == "InstructionsText")
            .Select(t => t.GetComponent<TMP_Text>()).FirstOrDefault(t => t != null);
        if (controller == null || instructions == null || instructions.canvas == null)
        {
            Debug.LogError("ARScene needs ARSceneController and InstructionsText on a Canvas. No scene changes were saved.");
            return;
        }

        var session = controller.GetComponent<NetworkCableARSession>();
        if (session == null) session = Undo.AddComponent<NetworkCableARSession>(controller.gameObject);
        Undo.RecordObject(session, "Connect Network Cables UI");
        session.instructionsText = instructions;
        if (session.toolbar == null) CreateToolbar(session, instructions.canvas.rootCanvas.transform);

        string[] disabled = { "SelectButton", "EditButton", "DeleteButton", "ResetButton", "AdjustButton", "InventoryButton" };
        session.ordinaryButtons = all.Where(t => disabled.Contains(t.name))
            .Select(t => t.GetComponent<Button>()).Where(b => b != null).ToArray();
        EditorUtility.SetDirty(session);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = session.toolbar;
        Debug.Log("Network Cables AR is ready: activity ID 10, four drag-and-snap cable steps, completion, and checkpoint saving. Backup: " + Path.GetFullPath(backup));
    }

    private static GameObject BuildWorkstation(MaterialSet mat)
    {
        var root = new GameObject("NetworkCableWorkstation");
        try
        {
            var workstation = root.AddComponent<NetworkCableWorkstation>();
            workstation.snapDistance = .075f;
            Primitive("WorkSurface", root.transform, PrimitiveType.Cube, new Vector3(0, .006f, 0),
                new Vector3(.58f, .012f, .38f), mat.surface, false);

            var connectors = new NetworkCableConnector[4];
            var targets = new NetworkCableTarget[4];
            float[] x = { -.21f, -.07f, .07f, .21f };
            var kinds = new[] { NetworkCableKind.UTP, NetworkCableKind.STP, NetworkCableKind.Coaxial, NetworkCableKind.FiberOptic };
            Material[] cableMaterials = { mat.utp, mat.stp, mat.coax, mat.fiber };

            for (int i = 0; i < kinds.Length; i++)
            {
                var fixedEnd = new GameObject(kinds[i] + "FixedEnd").transform;
                fixedEnd.SetParent(root.transform, false);
                fixedEnd.localPosition = new Vector3(x[i], .035f, -.145f);

                var lineObject = new GameObject(kinds[i] + "CableBody");
                lineObject.transform.SetParent(root.transform, false);
                var line = lineObject.AddComponent<LineRenderer>();
                line.sharedMaterial = cableMaterials[i];
                line.startWidth = line.endWidth = kinds[i] == NetworkCableKind.FiberOptic ? .007f :
                    kinds[i] == NetworkCableKind.Coaxial ? .014f : .010f;
                line.numCapVertices = 8;
                line.numCornerVertices = 5;

                var connectorObject = new GameObject(kinds[i] + "Connector");
                connectorObject.transform.SetParent(root.transform, false);
                connectorObject.transform.localPosition = new Vector3(x[i], .038f, -.055f);
                var collider = connectorObject.AddComponent<BoxCollider>();
                collider.size = new Vector3(.055f, .045f, .065f);
                var connector = connectorObject.AddComponent<NetworkCableConnector>();
                connector.kind = kinds[i]; connector.fixedEnd = fixedEnd; connector.cableLine = line;
                CreateConnectorVisual(connectorObject.transform, kinds[i], mat);
                connectors[i] = connector;

                var targetObject = new GameObject(kinds[i] + "Target");
                targetObject.transform.SetParent(root.transform, false);
                targetObject.transform.localPosition = new Vector3(x[i], .022f, .125f);
                var target = targetObject.AddComponent<NetworkCableTarget>();
                target.kind = kinds[i];
                var indicator = Primitive("PortGlow", targetObject.transform, PrimitiveType.Cube, Vector3.zero,
                    new Vector3(.075f, .025f, .065f), mat.port, false);
                target.indicator = indicator.GetComponent<Renderer>();
                var snap = new GameObject("SnapPoint").transform;
                snap.SetParent(targetObject.transform, false);
                snap.localPosition = new Vector3(0, .022f, 0);
                target.snapPoint = snap;
                CreatePortVisual(targetObject.transform, kinds[i], mat);
                targets[i] = target;

                CreateCrossSection(root.transform, kinds[i], new Vector3(x[i], .022f, -.155f), mat);
            }
            workstation.connectors = connectors;
            workstation.targets = targets;
            return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void CreateConnectorVisual(Transform parent, NetworkCableKind kind, MaterialSet mat)
    {
        if (kind == NetworkCableKind.UTP || kind == NetworkCableKind.STP)
        {
            Material shell = kind == NetworkCableKind.UTP ? mat.clear : mat.metal;
            Primitive("RJ45Shell", parent, PrimitiveType.Cube, Vector3.zero, new Vector3(.030f, .020f, .043f), shell, false);
            for (int i = 0; i < 4; i++)
                Primitive("Contact" + i, parent, PrimitiveType.Cube, new Vector3(-.0105f + i * .007f, .011f, .010f),
                    new Vector3(.004f, .002f, .016f), mat.copper, false);
        }
        else if (kind == NetworkCableKind.Coaxial)
        {
            Primitive("BNCBody", parent, PrimitiveType.Cylinder, Vector3.zero, new Vector3(.014f, .022f, .014f),
                mat.metal, false, Quaternion.Euler(90, 0, 0));
            Primitive("BNCPin", parent, PrimitiveType.Cylinder, new Vector3(0, 0, .030f), new Vector3(.003f, .010f, .003f),
                mat.copper, false, Quaternion.Euler(90, 0, 0));
        }
        else
        {
            Primitive("LCLeft", parent, PrimitiveType.Cube, new Vector3(-.009f, 0, 0), new Vector3(.014f, .016f, .035f), mat.lc, false);
            Primitive("LCRight", parent, PrimitiveType.Cube, new Vector3(.009f, 0, 0), new Vector3(.014f, .016f, .035f), mat.lc, false);
            Primitive("Clip", parent, PrimitiveType.Cube, new Vector3(0, .010f, -.004f), new Vector3(.030f, .004f, .018f), mat.clear, false);
        }
    }

    private static void CreatePortVisual(Transform parent, NetworkCableKind kind, MaterialSet mat)
    {
        if (kind == NetworkCableKind.UTP || kind == NetworkCableKind.STP)
            Primitive("EthernetPort", parent, PrimitiveType.Cube, new Vector3(0, .018f, 0), new Vector3(.038f, .028f, .030f),
                kind == NetworkCableKind.STP ? mat.metal : mat.dark, false);
        else if (kind == NetworkCableKind.Coaxial)
            Primitive("BNCPort", parent, PrimitiveType.Cylinder, new Vector3(0, .020f, 0), new Vector3(.020f, .012f, .020f), mat.metal, false);
        else
        {
            Primitive("LCSlotLeft", parent, PrimitiveType.Cube, new Vector3(-.010f, .018f, 0), new Vector3(.016f, .025f, .030f), mat.lc, false);
            Primitive("LCSlotRight", parent, PrimitiveType.Cube, new Vector3(.010f, .018f, 0), new Vector3(.016f, .025f, .030f), mat.lc, false);
        }
    }

    private static void CreateCrossSection(Transform parent, NetworkCableKind kind, Vector3 position, MaterialSet mat)
    {
        var group = new GameObject(kind + "Cutaway").transform;
        group.SetParent(parent, false); group.localPosition = position;
        if (kind == NetworkCableKind.UTP || kind == NetworkCableKind.STP)
        {
            if (kind == NetworkCableKind.STP)
                Primitive("Shield", group, PrimitiveType.Cylinder, Vector3.zero, new Vector3(.026f, .003f, .026f), mat.metal, false);
            Color[] colors = { Color.white, new Color(1f, .45f, .05f), Color.white, new Color(.1f, .45f, 1f), Color.white, Color.green, Color.white, new Color(.45f, .2f, .08f) };
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                var material = TemporaryMaterial(kind + "Pair" + i, colors[i]);
                Primitive("Conductor" + i, group, PrimitiveType.Cylinder,
                    new Vector3(Mathf.Cos(a) * .014f, .006f, Mathf.Sin(a) * .014f), new Vector3(.004f, .006f, .004f), material, false);
            }
        }
        else if (kind == NetworkCableKind.Coaxial)
        {
            Primitive("Jacket", group, PrimitiveType.Cylinder, Vector3.zero, new Vector3(.027f, .003f, .027f), mat.coax, false);
            Primitive("Shield", group, PrimitiveType.Cylinder, new Vector3(0, .004f, 0), new Vector3(.021f, .003f, .021f), mat.metal, false);
            Primitive("Insulator", group, PrimitiveType.Cylinder, new Vector3(0, .008f, 0), new Vector3(.014f, .003f, .014f), mat.clear, false);
            Primitive("Core", group, PrimitiveType.Cylinder, new Vector3(0, .012f, 0), new Vector3(.004f, .003f, .004f), mat.copper, false);
        }
        else
        {
            Primitive("FiberJacket", group, PrimitiveType.Cylinder, Vector3.zero, new Vector3(.025f, .003f, .025f), mat.fiber, false);
            Primitive("CoreLeft", group, PrimitiveType.Cylinder, new Vector3(-.007f, .006f, 0), new Vector3(.004f, .006f, .004f), mat.clear, false);
            Primitive("CoreRight", group, PrimitiveType.Cylinder, new Vector3(.007f, .006f, 0), new Vector3(.004f, .006f, .004f), mat.clear, false);
        }
    }

    private static void CreateToolbar(NetworkCableARSession session, Transform canvas)
    {
        var panel = new GameObject("NetworkCablePanel", typeof(RectTransform));
        panel.transform.SetParent(canvas, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(.20f, .70f); rect.anchorMax = new Vector2(.80f, .82f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        session.toolbar = panel;
        session.stageText = Text("StageText", panel.transform, "Cable Match: 0 / 4", new Vector2(0, .58f), Vector2.one, 20);
        session.restartButton = Button("RestartCablesButton", panel.transform, "Restart", 0, .48f);
        session.repositionButton = Button("RepositionCablesButton", panel.transform, "Reposition", .52f, 1f);
        panel.SetActive(false);
    }

    private static TMP_Text Text(string name, Transform parent, string value, Vector2 min, Vector2 max, float size)
    {
        var text = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false);
        text.rectTransform.anchorMin = min; text.rectTransform.anchorMax = max;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        text.font = TMP_Settings.defaultFontAsset; text.text = value; text.fontSize = size;
        text.color = Color.white; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        return text;
    }

    private static Button Button(string name, Transform parent, string value, float min, float max)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(min, 0); rect.anchorMax = new Vector2(max, .54f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        obj.GetComponent<Image>().color = new Color(.48f, .26f, .62f, .96f);
        var button = obj.GetComponent<Button>(); button.targetGraphic = obj.GetComponent<Image>();
        Text("Label", obj.transform, value, Vector2.zero, Vector2.one, 19);
        return button;
    }

    private static GameObject Primitive(string name, Transform parent, PrimitiveType type, Vector3 position,
        Vector3 scale, Material material, bool keepCollider, Quaternion? rotation = null)
    {
        var obj = GameObject.CreatePrimitive(type); obj.name = name; obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position; obj.transform.localRotation = rotation ?? Quaternion.identity;
        obj.transform.localScale = scale;
        var renderer = obj.GetComponent<Renderer>(); if (renderer != null) renderer.sharedMaterial = material;
        if (!keepCollider)
        {
            var collider = obj.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        }
        return obj;
    }

    private sealed class MaterialSet
    {
        public Material surface, utp, stp, coax, fiber, metal, copper, clear, lc, dark, port;
    }

    private static MaterialSet CreateMaterials()
    {
        return new MaterialSet
        {
            surface = Material("WorkSurface", new Color(.12f, .14f, .18f)),
            utp = Material("UTPBlue", new Color(.08f, .32f, .85f)),
            stp = Material("STPSilver", new Color(.58f, .63f, .68f)),
            coax = Material("CoaxBlack", new Color(.025f, .028f, .035f)),
            fiber = Material("FiberYellow", new Color(1f, .72f, .04f)),
            metal = Material("ConnectorMetal", new Color(.62f, .68f, .74f), .8f),
            copper = Material("Copper", new Color(.78f, .34f, .10f), .5f),
            clear = Material("ConnectorClear", new Color(.78f, .88f, .92f)),
            lc = Material("LCBlue", new Color(.08f, .42f, .86f)),
            dark = Material("PortDark", new Color(.035f, .045f, .06f)),
            port = Material("PortIndicator", new Color(.22f, .27f, .34f))
        };
    }

    private static Material Material(string name, Color color, float metallic = 0f)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        material = new Material(shader) { name = name, color = color };
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .5f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Material TemporaryMaterial(string name, Color color)
    {
        // These small conductor colors are embedded as prefab sub-assets by Unity.
        string path = MaterialFolder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        material = new Material(shader) { name = name, color = color };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
