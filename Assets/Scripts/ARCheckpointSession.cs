using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ARCheckpointSession : MonoBehaviour
{
    public static ARCheckpointSession Instance { get; private set; }
    public static bool BlocksInput => Instance != null &&
        (Instance.pending != null || Instance.networkResumeFrame == Time.frameCount);
    private int networkResumeFrame = -1;
    private LessonData lesson;
    private ARActivityData activity;
    private ARActivityProgress progress;
    private ARPlacementManager placement;
    private ARAssemblyManager assembly;
    private NetworkConnectionManager network;
    private RJ45ARSession rj45;
    private NetworkCableARSession networkCable;
    private string lastRJ45Json;
    private bool IsRJ45 => activity != null && activity.activityType == ARActivityType.RJ45Termination;
    private bool IsNetwork => activity != null && activity.activityType == ARActivityType.NetworkDesign;
    private bool IsNetworkCable => activity != null && activity.activityType == ARActivityType.NetworkCables;
    private float nextNetworkSave;
    private string lastNetworkJson;
    private ARCheckpointData pending;
    private string signature;
    private bool waitingForSurface, restoring, finished;
    [Header("Scene Checkpoint UI (auto-connected by name when empty)")]
    [SerializeField] private GameObject resumePanel;
    [SerializeField] private GameObject surfacePlacementPanel;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button resumeStartOverButton;
    [SerializeField] private Button surfaceStartOverButton;
    private bool sceneUIReady;
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    public void Initialize(LessonData currentLesson, ARActivityProgress currentProgress, ARPlacementManager currentPlacement)
    {
        Instance = this;
        sceneUIReady = BindSceneUI();
        lesson = currentLesson;
        activity = lesson.arActivity;
        progress = currentProgress;
        placement = currentPlacement;
        assembly = FindFirstObjectByType<ARAssemblyManager>();
        network = FindFirstObjectByType<NetworkConnectionManager>();
        rj45 = FindFirstObjectByType<RJ45ARSession>();
        networkCable = FindFirstObjectByType<NetworkCableARSession>();
        signature = ARCheckpointStore.Signature(activity);
        bool isAssembly = activity.activityType == ARActivityType.Assembly;
        if (!isAssembly && activity.activityType != ARActivityType.ToolIdentification &&
            activity.activityType != ARActivityType.HardwareIdentification && !IsNetwork && !IsRJ45 && !IsNetworkCable) return;
        if (!sceneUIReady) return;
        pending = ARCheckpointStore.Load(lesson.lessonID, signature, isAssembly,
            activity.assemblyActivity?.steps?.Length ?? 0, activity.assemblyActivity != null && activity.assemblyActivity.includeDisassembly,
            IsNetwork ? activity.availableObjects?.Length ?? 0 : -1, IsRJ45, IsNetworkCable);
        if (pending != null) ShowPrompt("Continue your saved activity?", false);
    }

    public static void SaveCurrent()
    {
        if (Instance != null) Instance.SaveCheckpoint();
    }

    private void SaveCheckpoint()
    {
        if (!sceneUIReady || lesson == null || pending != null || restoring || finished) return;
        var data = new ARCheckpointData { lessonId = lesson.lessonID, signature = signature };
        if (IsNetworkCable)
        {
            if (networkCable == null) return;
            try
            {
                data.networkCable = networkCable.CaptureCheckpoint();
                if (data.networkCable == null) return;
                if (!ARCheckpointStore.IsValid(data, lesson.lessonID, signature, false, 0, false, -1, false, true))
                    throw new InvalidOperationException("Network Cables snapshot failed validation; previous save preserved.");
                ARCheckpointStore.Save(data);
            }
            catch (Exception ex) { Debug.LogWarning("Network Cables checkpoint could not be saved: " + ex.Message); }
            return;
        }
        if (IsRJ45)
        {
            if (rj45 == null) return;
            try
            {
                data.rj45 = rj45.CaptureCheckpoint();
                if (data.rj45 == null) return;
                if (!ARCheckpointStore.IsValid(data, lesson.lessonID, signature, false, 0, false, -1, true))
                    throw new InvalidOperationException("RJ45 snapshot failed validation; previous save preserved.");
                string json = JsonUtility.ToJson(data.rj45);
                if (json != lastRJ45Json && ARCheckpointStore.Save(data)) lastRJ45Json = json;
            }
            catch (Exception ex) { Debug.LogWarning("RJ45 checkpoint could not be saved: " + ex.Message); }
            return;
        }
        if (IsNetwork)
        {
            if (network == null || !network.Available) return;
            try
            {
                data.network = network.CaptureNetworkCheckpoint();
                if (!ARCheckpointStore.IsValid(data, lesson.lessonID, signature, false, 0, false, activity.availableObjects?.Length ?? 0))
                    throw new InvalidOperationException("Network snapshot failed validation; previous save preserved.");
                string json = JsonUtility.ToJson(data.network);
                if (json == lastNetworkJson) return;
                if (data.network.nodes.Length == 0 && data.network.passedMask == 0 && data.network.topology == 0)
                {
                    ARCheckpointStore.Clear(lesson.lessonID);
                    lastNetworkJson = json;
                }
                else if (ARCheckpointStore.Save(data)) lastNetworkJson = json;
            }
            catch (Exception ex) { Debug.LogWarning("Network checkpoint could not be saved: " + ex.Message); }
            return;
        }
        if (activity.activityType == ARActivityType.Assembly)
        {
            if (assembly == null) return;
            assembly.CaptureCheckpoint(data);
        }
        else if (progress != null) data.inspected = progress.CaptureInspectedObjects();
        if (ARCheckpointStore.IsValid(data, lesson.lessonID, signature, activity.activityType == ARActivityType.Assembly,
            activity.assemblyActivity?.steps?.Length ?? 0, activity.assemblyActivity != null && activity.assemblyActivity.includeDisassembly))
            ARCheckpointStore.Save(data);
    }

    public static void CompleteCurrent()
    {
        if (Instance == null || Instance.restoring || Instance.lesson == null) return;
        Instance.finished = true;
        ARCheckpointStore.Clear(Instance.lesson.lessonID);
    }

    public static void ClearSavedCurrent()
    {
        if (Instance == null || Instance.lesson == null) return;
        ARCheckpointStore.Clear(Instance.lesson.lessonID);
    }

    public void Resume()
    {
        if (pending == null) return;
        if (activity.activityType != ARActivityType.Assembly && !IsNetwork && !IsRJ45 && !IsNetworkCable)
        {
            var saved = pending;
            pending = null;
            ClosePrompt();
            progress.RestoreInspectedObjects(saved.inspected);
            return;
        }
        waitingForSurface = true;
        ShowPrompt("Scan a flat surface, then tap it to restore your workspace.", true);
    }

    public void StartOver()
    {
        if (lesson == null) return;
        ARCheckpointStore.Clear(lesson.lessonID);
        if (IsNetwork && network != null)
        {
            restoring = true;
            try { network.StartNetworkOver(); }
            finally { restoring = false; }
            lastNetworkJson = null;
        }
        pending = null;
        waitingForSurface = false;
        ClosePrompt();
        if (IsRJ45 && rj45 != null)
        {
            restoring = true;
            try { rj45.ClearCheckpointWorkspace(true); }
            finally { restoring = false; }
            lastRJ45Json = null;
        }
        if (IsNetworkCable && networkCable != null)
        {
            restoring = true;
            try { networkCable.ClearCheckpointWorkspace(true); }
            finally { restoring = false; }
        }
    }

    private void Update()
    {
        if ((IsNetwork || IsRJ45) && Time.unscaledTime >= nextNetworkSave)
        {
            nextNetworkSave = Time.unscaledTime + 1f;
            SaveCheckpoint();
        }
        if (!waitingForSurface || pending == null || placement == null) return;
        Vector2 point;
#if UNITY_EDITOR
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        point = Mouse.current.position.ReadValue();
#else
        if (Touchscreen.current == null || !Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return;
        point = Touchscreen.current.primaryTouch.position.ReadValue();
#endif
        if (EventSystem.current != null)
        {
            uiHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, uiHits);
            if (uiHits.Count > 0) return;
        }
        if (!placement.TryCheckpointSurface(point, out var pose)) return;
        if ((IsRJ45 || IsNetworkCable) && Vector3.Dot(pose.rotation * Vector3.up, Vector3.up) < .95f) return;
        restoring = true;
        try
        {
            if (IsNetworkCable)
            {
                if (networkCable == null) throw new InvalidOperationException("Network Cables session is missing.");
                networkCable.RestoreCheckpoint(pending.networkCable, pose);
            }
            else if (IsRJ45)
            {
                if (rj45 == null) throw new InvalidOperationException("RJ45 session is missing.");
                rj45.RestoreCheckpoint(pending.rj45, pose);
                lastRJ45Json = null;
            }
            else if (IsNetwork)
            {
                if (network == null) throw new InvalidOperationException("Network manager is missing.");
                network.RestoreNetworkCheckpoint(pending.network, pose);
                lastNetworkJson = null;
            }
            else
            {
                if (assembly == null) throw new InvalidOperationException("Assembly manager is missing.");
                assembly.RestoreCheckpoint(pending, activity, placement, pose);
            }
            waitingForSurface = false;
            pending = null;
            if (IsNetwork || IsRJ45 || IsNetworkCable) networkResumeFrame = Time.frameCount;
            ClosePrompt();
            var mode = FindFirstObjectByType<ARModeManager>();
            if (mode != null) mode.SetEditMode();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Could not restore AR checkpoint: " + ex.Message);
            if (IsNetworkCable) { if (networkCable != null) networkCable.ClearCheckpointWorkspace(false); }
            else if (IsRJ45) { if (rj45 != null) rj45.ClearCheckpointWorkspace(false); }
            else if (IsNetwork) { if (network != null) network.StartNetworkOver(); }
            else
            {
                placement.ClearCheckpointWorkspace();
                if (assembly != null) assembly.SetActivity(activity.assemblyActivity);
            }
            waitingForSurface = false;
            ShowPrompt("Couldn't restore this workspace. Retry Resume or choose Start Over.", false);
        }
        finally { restoring = false; }
    }

    private void OnApplicationPause(bool paused) { if (paused) SaveCheckpoint(); }
    private void OnApplicationFocus(bool focused) { if (!focused) SaveCheckpoint(); }
    private void OnApplicationQuit() { SaveCheckpoint(); }
    private void OnDestroy()
    {
        // Successful steps save immediately; don't inspect objects during scene teardown.
        ClosePrompt();
        if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
        if (resumeStartOverButton != null) resumeStartOverButton.onClick.RemoveListener(StartOver);
        if (surfaceStartOverButton != null) surfaceStartOverButton.onClick.RemoveListener(StartOver);
        if (Instance == this) Instance = null;
    }

    private bool BindSceneUI()
    {
        Transform uiRoot = null;
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            uiRoot = FindNamed<Transform>(root.transform, "ARCheckpointUI");
            if (uiRoot != null) break;
        }
        if (uiRoot != null)
        {
            // The checkpoint panels are hidden individually. Keep their shared root
            // active so a valid Resume prompt can actually become visible.
            uiRoot.gameObject.SetActive(true);
            if (resumePanel == null) resumePanel = FindNamed<Transform>(uiRoot, "ResumePanel")?.gameObject;
            if (surfacePlacementPanel == null) surfacePlacementPanel = FindNamed<Transform>(uiRoot, "SurfacePlacementPanel")?.gameObject;
        }
        if (resumePanel != null)
        {
            if (messageText == null) messageText = FindNamed<TMP_Text>(resumePanel.transform, "MessageText");
            if (resumeButton == null) resumeButton = FindNamed<Button>(resumePanel.transform, "ResumeButton");
            if (resumeStartOverButton == null) resumeStartOverButton = FindNamed<Button>(resumePanel.transform, "StartOverButton");
        }
        if (surfacePlacementPanel != null)
        {
            if (instructionText == null) instructionText = FindNamed<TMP_Text>(surfacePlacementPanel.transform, "InstructionText");
            if (surfaceStartOverButton == null) surfaceStartOverButton = FindNamed<Button>(surfacePlacementPanel.transform, "StartOverButton");
        }
        ClosePrompt();
        if (resumePanel == null || surfacePlacementPanel == null || messageText == null || instructionText == null ||
            resumeButton == null || resumeStartOverButton == null || surfaceStartOverButton == null)
        {
            Debug.LogError("Checkpoint UI references are incomplete. Check ARCheckpointUI and its panel/button names. Existing checkpoints will be preserved.", this);
            return false;
        }
        // These are scene-owned objects; only their visibility/text and listeners change.
        messageText.raycastTarget = false;
        instructionText.raycastTarget = false;
        var surfaceBackground = surfacePlacementPanel.GetComponent<Graphic>();
        if (surfaceBackground != null) surfaceBackground.raycastTarget = false;
        if (uiRoot != null) uiRoot.SetAsLastSibling();
        // Remove our own callbacks before adding them, keeping initialization idempotent.
        resumeButton.onClick.RemoveListener(Resume);
        resumeButton.onClick.AddListener(Resume);
        resumeStartOverButton.onClick.RemoveListener(StartOver);
        resumeStartOverButton.onClick.AddListener(StartOver);
        surfaceStartOverButton.onClick.RemoveListener(StartOver);
        surfaceStartOverButton.onClick.AddListener(StartOver);
        return true;
    }

    private static T FindNamed<T>(Transform root, string name) where T : Component
    {
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child.GetComponent<T>();
        return null;
    }

    private void ClosePrompt()
    {
        ArdentMotion.SetPanelVisible(resumePanel, false);
        ArdentMotion.SetPanelVisible(surfacePlacementPanel, false);
    }

    private void ShowPrompt(string message, bool surface)
    {
        ClosePrompt();
        if (!sceneUIReady) return;
        if (surface)
        {
            instructionText.text = message;
            ArdentMotion.SetPanelVisible(surfacePlacementPanel, true);
        }
        else
        {
            messageText.text = message;
            ArdentMotion.SetPanelVisible(resumePanel, true);
        }
    }
}
