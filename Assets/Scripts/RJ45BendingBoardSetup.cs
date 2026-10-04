#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RJ45BendingBoardSetup
{
    [MenuItem("ARDENT/RJ45/Create Eight Bendable Wires")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Exit Play Mode first."); return; }
        if (PrefabStageUtility.GetCurrentPrefabStage() != null) { Debug.LogWarning("Close Prefab Mode first."); return; }
        if (TMP_Settings.defaultFontAsset == null) { Debug.LogError("Import TMP Essential Resources first."); return; }
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null) { Debug.LogError("No supported unlit shader found."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "RJ45_EightBendableWires";
        if (!AssetDatabase.IsValidFolder("Assets/Models")) AssetDatabase.CreateFolder("Assets", "Models");
        string folder = AssetDatabase.GenerateUniqueAssetPath("Assets/Models/RJ45BendingBoardMaterials");
        AssetDatabase.CreateFolder("Assets/Models", System.IO.Path.GetFileName(folder));
        var dark = Material(shader, folder, "Slot", new Color(.14f, .2f, .28f));
        var white = Material(shader, folder, "White", Color.white);
        var camera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
        camera.tag = "MainCamera"; camera.transform.position = new Vector3(0, 0, -10);
        camera.orthographic = true; camera.orthographicSize = 2.1f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.035f, .055f, .085f);

        var light = new GameObject("Wire Light", typeof(Light)).GetComponent<Light>();
        light.type = LightType.Directional; light.intensity = 1.3f; light.transform.rotation = Quaternion.Euler(25,-25,0);
        RenderSettings.ambientLight = new Color(.4f,.4f,.4f);
        var root = new GameObject("RJ45BendingBoard");
        Cube("CableJacket", root.transform, new Vector3(0,-1.38f,.04f), new Vector3(.48f,.5f,.18f), dark);
        var board = root.AddComponent<RJ45WireArrangement>();
        board.interactionCamera = camera;
        board.wires = new RJ45DraggableWire[8]; board.pinSlots = new Transform[8]; board.pinLabels = new TMP_Text[8];
        Color[] colors = { new Color(1,.45f,.08f), new Color(1,.45f,.08f), new Color(.1f,.65f,.25f), new Color(.1f,.65f,.25f), new Color(.15f,.4f,1), new Color(.15f,.4f,1), new Color(.5f,.27f,.1f), new Color(.5f,.27f,.1f) };
        string[] labels = { "White/\nOrange", "Orange", "White/\nGreen", "Green", "White/\nBlue", "Blue", "White/\nBrown", "Brown" };
        int[] trayOrder = { 5, 2, 7, 0, 3, 6, 1, 4 };
        for (int pin = 0; pin < 8; pin++)
        {
            float x = (pin - 3.5f) * .43f;
            var slot = new GameObject("Pin" + (pin + 1)).transform;
            slot.SetParent(root.transform, false); slot.localPosition = new Vector3(x, .35f, 0);
            board.pinSlots[pin] = slot;
            Cube("SlotVisual", slot, new Vector3(0,0,.09f), new Vector3(.37f,.35f,.02f), dark);
            board.pinLabels[pin] = WorldText("PinLabel", root.transform, new Vector3(x,.96f,-.05f), "PIN " + (pin + 1), .46f, .32f, 1.35f);
            int identity = trayOrder[pin];
            var wire = new GameObject(((RJ45WireColor)identity).ToString()).AddComponent<RJ45DraggableWire>();
            wire.transform.SetParent(root.transform, false); wire.transform.localPosition = new Vector3(x,-.5f,0);
            wire.identity = (RJ45WireColor)identity;
            var collider = wire.gameObject.AddComponent<BoxCollider>(); collider.size = new Vector3(.32f,.26f,.16f); wire.pickCollider = collider;
            var material = Material(shader, folder, wire.name, colors[identity]);
            var tipVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tipVisual.name = "TipVisual"; tipVisual.transform.SetParent(wire.transform,false);
            tipVisual.transform.localScale = new Vector3(.13f,.13f,.13f);
            tipVisual.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(tipVisual.GetComponent<Collider>());
            if(identity % 2 == 0)
                Cube("WhiteMark",wire.transform,new Vector3(0,0,-.065f),new Vector3(.09f,.035f,.01f),white);
            WorldText("WireName", wire.transform, new Vector3(0,.20f,-.08f), labels[identity], .42f,.25f,1.1f);
            var tube = new GameObject(wire.name + "Tube",typeof(MeshFilter),typeof(MeshRenderer)).AddComponent<RJ45WireTube>();
            tube.transform.SetParent(root.transform,false);
            tube.tip = wire.transform; tube.fixedBase = new Vector3((pin-3.5f)*.045f,-1.14f,.015f*pin);
            tube.striped = identity % 2 == 0;
            tube.GetComponent<MeshRenderer>().sharedMaterials = new[] { material, white };
            wire.bendingTube = tube;
            board.wires[identity] = wire;
        }
        var canvas = new GameObject("PrototypeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280,720); scaler.matchWidthOrHeight = .5f;
        board.standardText = UIText("StandardText", canvas.transform, "", new Vector2(.04f,.91f),new Vector2(.96f,.99f),26);
        board.instructionsText = UIText("InstructionsText", canvas.transform, "",new Vector2(.04f,.77f),new Vector2(.96f,.90f),21);
        board.standardAButton = Button("T568AButton",canvas.transform,"T568A", .05f,.30f);
        board.standardBButton = Button("T568BButton",canvas.transform,"T568B", .375f,.625f);
        board.restartButton = Button("RestartButton",canvas.transform,"Restart", .70f,.95f);
        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        // Save only the board as a reusable placeholder prefab. Scene UI stays editable in the scene.
        var savedCamera = board.interactionCamera;
        var a = board.standardAButton; var b = board.standardBButton; var restart = board.restartButton;
        var instructions = board.instructionsText; var standard = board.standardText;
        board.interactionCamera = null; board.standardAButton = null; board.standardBButton = null; board.restartButton = null;
        board.instructionsText = null; board.standardText = null; board.fitOrthographicCamera = false;
        PrefabUtility.SaveAsPrefabAsset(root, AssetDatabase.GenerateUniqueAssetPath("Assets/Models/RJ45BendingBoard.prefab"));
        board.interactionCamera = savedCamera; board.standardAButton = a; board.standardBButton = b; board.restartButton = restart;
        board.instructionsText = instructions; board.standardText = standard; board.fitOrthographicCamera = true;
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        string scenePath = AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/RJ45_EightBendableWires.unity");
        EditorSceneManager.SaveScene(scene,scenePath);
        AssetDatabase.SaveAssets(); Selection.activeGameObject = root;
        Debug.Log("RJ45 prototype created: " + scenePath + ". Press Play and drag wires. No lesson completion is awarded by this prototype.");
    }
    private static Material Material(Shader shader, string folder, string name, Color color)
    {
        var material = new Material(shader); material.color = color;
        AssetDatabase.CreateAsset(material, folder + "/" + name + ".mat"); return material;
    }
    private static void Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name; obj.transform.SetParent(parent,false);
        obj.transform.localPosition = position; obj.transform.localScale = scale;
        Object.DestroyImmediate(obj.GetComponent<Collider>()); obj.GetComponent<Renderer>().sharedMaterial = material;
    }
    private static TMP_Text WorldText(string name, Transform parent, Vector3 position, string value, float width, float height, float size)
    {
        var text = new GameObject(name).AddComponent<TextMeshPro>(); text.transform.SetParent(parent,false); text.transform.localPosition = position;
        text.rectTransform.sizeDelta = new Vector2(width,height); text.font = TMP_Settings.defaultFontAsset; text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center; text.text = value; text.color = Color.white; text.raycastTarget = false; return text;
    }
    private static TextMeshProUGUI UIText(string name, Transform parent, string value, Vector2 min, Vector2 max, float size)
    {
        var text = new GameObject(name,typeof(RectTransform)).AddComponent<TextMeshProUGUI>(); text.transform.SetParent(parent,false);
        text.rectTransform.anchorMin = min; text.rectTransform.anchorMax = max; text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        text.font = TMP_Settings.defaultFontAsset; text.text = value; text.fontSize = size; text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white; text.raycastTarget = false; return text;
    }
    private static Button Button(string name, Transform parent, string label, float min, float max)
    {
        var obj = new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button)); obj.transform.SetParent(parent,false);
        var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(min,.025f); rect.anchorMax = new Vector2(max,.10f); rect.offsetMin = rect.offsetMax = Vector2.zero;
        obj.GetComponent<Image>().color = new Color(.22f,.32f,.5f);
        var button = obj.GetComponent<Button>(); button.targetGraphic = obj.GetComponent<Image>();
        UIText("Label",obj.transform,label,Vector2.zero,Vector2.one,22); return button;
    }
}
#endif

