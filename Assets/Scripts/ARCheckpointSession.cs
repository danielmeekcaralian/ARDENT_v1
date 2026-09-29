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
    public static bool BlocksInput => Instance != null && Instance.pending != null;
    private LessonData lesson;
    private ARActivityData activity;
    private ARActivityProgress progress;
    private ARPlacementManager placement;
    private ARAssemblyManager assembly;
    private ARCheckpointData pending;
    private string signature;
    private bool waitingForSurface, restoring, finished;
    private GameObject prompt;
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    public void Initialize(LessonData currentLesson, ARActivityProgress currentProgress, ARPlacementManager currentPlacement)
    {
        Instance = this;
        lesson = currentLesson;
        activity = lesson.arActivity;
        progress = currentProgress;
        placement = currentPlacement;
        assembly = FindFirstObjectByType<ARAssemblyManager>();
        signature = ARCheckpointStore.Signature(activity);
        bool isAssembly = activity.activityType == ARActivityType.Assembly;
        if (!isAssembly && activity.activityType != ARActivityType.ToolIdentification &&
            activity.activityType != ARActivityType.HardwareIdentification) return;
        pending = ARCheckpointStore.Load(lesson.lessonID, signature, isAssembly,
            activity.assemblyActivity?.steps?.Length ?? 0, activity.assemblyActivity != null && activity.assemblyActivity.includeDisassembly);
        if (pending != null) ShowPrompt("Continue your saved activity?", false);
    }

    public static void SaveCurrent()
    {
        if (Instance != null) Instance.SaveCheckpoint();
    }

    private void SaveCheckpoint()
    {
        if (lesson == null || pending != null || restoring || finished) return;
        var data = new ARCheckpointData { lessonId = lesson.lessonID, signature = signature };
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

    public void Resume()
    {
        if (pending == null) return;
        if (activity.activityType != ARActivityType.Assembly)
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
        pending = null;
        waitingForSurface = false;
        ClosePrompt();
    }

    private void Update()
    {
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
        restoring = true;
        try
        {
            if (assembly == null) throw new InvalidOperationException("Assembly manager is missing.");
            assembly.RestoreCheckpoint(pending, activity, placement, pose);
            waitingForSurface = false;
            pending = null;
            ClosePrompt();
            var mode = FindFirstObjectByType<ARModeManager>();
            if (mode != null) mode.SetEditMode();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Could not restore AR checkpoint: " + ex.Message);
            placement.ClearCheckpointWorkspace();
            if (assembly != null) assembly.SetActivity(activity.assemblyActivity);
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
        if (Instance == this) Instance = null;
    }

    private void ClosePrompt()
    {
        if (prompt != null) { prompt.SetActive(false); Destroy(prompt); }
    }

    private void ShowPrompt(string message, bool surface)
    {
        ClosePrompt();
        prompt = new GameObject("ARResumePrompt", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = prompt.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        var scaler = prompt.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        if (!surface)
        {
            var shade = new GameObject("Background", typeof(RectTransform), typeof(Image));
            var rect = shade.GetComponent<RectTransform>();
            rect.SetParent(prompt.transform, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            shade.GetComponent<Image>().color = new Color(0, 0, 0, 0.85f);
        }
        var label = MakeLabel(prompt.transform, message, new Vector2(0, surface ? 245 : 70), new Vector2(850, 110));
        label.fontSize = 30;
        if (!surface) MakeButton("Resume", new Vector2(-160, -60), Resume);
        MakeButton("Start Over", new Vector2(surface ? 0 : 160, surface ? -250 : -60), StartOver);
    }

    private TMP_Text MakeLabel(Transform parent, string text, Vector2 position, Vector2 size)
    {
        var obj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = obj.GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        var label = obj.GetComponent<TextMeshProUGUI>();
        label.text = text; label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 26; label.raycastTarget = false;
        return label;
    }

    private void MakeButton(string text, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        var obj = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = obj.GetComponent<RectTransform>(); rect.SetParent(prompt.transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = new Vector2(280, 72);
        obj.GetComponent<Image>().color = new Color(0.04f, 0.36f, 0.44f, 1);
        obj.GetComponent<Button>().onClick.AddListener(action);
        MakeLabel(rect, text, Vector2.zero, rect.sizeDelta);
    }
}
