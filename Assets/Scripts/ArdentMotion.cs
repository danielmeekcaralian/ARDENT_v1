using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// App-wide, UI-only motion for ARDENT. This deliberately operates only on
/// RectTransforms and CanvasGroups; world-space AR content is never touched.
/// </summary>
[DefaultExecutionOrder(-950)]
public sealed class ArdentMotion : MonoBehaviour
{
    private static ArdentMotion instance;
    private readonly HashSet<int> animatedSceneRoots = new HashSet<int>();
    private Canvas fadeCanvas;
    private CanvasGroup fadeGroup;
    private GameObject loadingRoot;
    private RectTransform loadingFill;
    private RectTransform loadingChirbit;
    private TMP_Text loadingText;
    private Coroutine sceneFade;
    private bool changingScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var host = new GameObject("Ardent Motion");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<ArdentMotion>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        CreateFadeOverlay();
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(RefreshBindings());
        FadeFromBlack();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
        {
            changingScene = false;
            animatedSceneRoots.Clear();
        }
        StartCoroutine(BindAfterLayout());
        if (mode == LoadSceneMode.Single) FadeFromBlack();
    }

    private IEnumerator BindAfterLayout()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        BindSceneUI();
        yield return new WaitForSecondsRealtime(.25f);
        BindSceneUI();
    }

    private IEnumerator RefreshBindings()
    {
        while (true)
        {
            BindSceneUI();
            yield return new WaitForSecondsRealtime(.4f);
        }
    }

    private void BindSceneUI()
    {
        foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!IsInLoadedScene(button) || button.GetComponent<ArdentButtonMotion>() != null) continue;
            button.gameObject.AddComponent<ArdentButtonMotion>().Initialize();
        }

        foreach (var text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!IsInLoadedScene(text) || text.GetComponent<ArdentInstructionMotion>() != null) continue;
            string key = text.name.ToLowerInvariant();
            if (key.Contains("instruction") || key.Contains("guidance") || key.Contains("hint") || key.Contains("status"))
                text.gameObject.AddComponent<ArdentInstructionMotion>().Initialize();
        }

        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!IsInLoadedScene(canvas) || canvas == fadeCanvas) continue;
            foreach (Transform child in canvas.transform)
            {
                var rect = child as RectTransform;
                if (rect == null || !rect.gameObject.activeInHierarchy) continue;
                int id = rect.gameObject.GetInstanceID();
                if (animatedSceneRoots.Add(id)) ArdentEntranceMotion.Play(rect);
            }
        }

        foreach (var rect in FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!IsInLoadedScene(rect) || rect.GetComponent<ArdentPanelMotion>() != null) continue;
            string key = rect.name.ToLowerInvariant();
            if (key.Contains("popup") || key.Contains("modal") || key.EndsWith("dialogue") || key.Contains("dialoguepanel"))
                rect.gameObject.AddComponent<ArdentPanelMotion>().Initialize();
        }
    }

    private static bool IsInLoadedScene(Component component) =>
        component != null && component.gameObject.scene.IsValid() && component.gameObject.scene.isLoaded;

    private void CreateFadeOverlay()
    {
        var canvasObject = new GameObject("Scene Fade", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);
        fadeCanvas = canvasObject.GetComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = short.MaxValue;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = .5f;
        fadeGroup = canvasObject.GetComponent<CanvasGroup>();

        var imageObject = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);
        var rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        imageObject.GetComponent<Image>().color = new Color(.025f, .035f, .065f, 1f);
        CreateLoadingUI(canvasObject.transform);
        fadeGroup.alpha = 1f;
        fadeGroup.blocksRaycasts = true;
    }

    private void CreateLoadingUI(Transform parent)
    {
        loadingRoot = new GameObject("Loading Content", typeof(RectTransform));
        loadingRoot.transform.SetParent(parent, false);
        var rootRect = (RectTransform)loadingRoot.transform;
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(.5f, .5f);
        rootRect.pivot = new Vector2(.5f, .5f);
        rootRect.sizeDelta = new Vector2(560f, 420f);

        var chirbitObject = new GameObject("Chirbit", typeof(RectTransform), typeof(Image));
        chirbitObject.transform.SetParent(rootRect, false);
        loadingChirbit = chirbitObject.GetComponent<RectTransform>();
        loadingChirbit.anchorMin = loadingChirbit.anchorMax = new Vector2(.5f, .5f);
        loadingChirbit.pivot = new Vector2(.5f, .5f);
        loadingChirbit.anchoredPosition = new Vector2(0f, 72f);
        loadingChirbit.sizeDelta = new Vector2(150f, 165f);
        var chirbitImage = chirbitObject.GetComponent<Image>();
        chirbitImage.preserveAspect = true;
        var chirbitSprites = Resources.LoadAll<Sprite>("Sprite Assets/Chirbit_Solo");
        Sprite largest = null;
        foreach (var sprite in chirbitSprites)
            if (largest == null || sprite.rect.width * sprite.rect.height > largest.rect.width * largest.rect.height)
                largest = sprite;
        chirbitImage.sprite = largest;
        chirbitImage.enabled = largest != null;

        var titleObject = new GameObject("ARDENT", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObject.transform.SetParent(rootRect, false);
        var titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(.5f, .5f);
        titleRect.anchoredPosition = new Vector2(0f, -35f);
        titleRect.sizeDelta = new Vector2(520f, 58f);
        var title = titleObject.GetComponent<TextMeshProUGUI>();
        title.text = "ARDENT";
        title.alignment = TextAlignmentOptions.Center;
        title.fontSize = 36f;
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(.96f, .98f, 1f);
        title.raycastTarget = false;

        var textObject = new GameObject("Loading Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(rootRect, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = textRect.anchorMax = new Vector2(.5f, .5f);
        textRect.anchoredPosition = new Vector2(0f, -87f);
        textRect.sizeDelta = new Vector2(520f, 44f);
        loadingText = textObject.GetComponent<TextMeshProUGUI>();
        loadingText.alignment = TextAlignmentOptions.Center;
        loadingText.fontSize = 18f;
        loadingText.color = new Color(.76f, .82f, .92f);
        loadingText.raycastTarget = false;

        var trackObject = new GameObject("Progress Track", typeof(RectTransform), typeof(Image));
        trackObject.transform.SetParent(rootRect, false);
        var trackRect = trackObject.GetComponent<RectTransform>();
        trackRect.anchorMin = trackRect.anchorMax = new Vector2(.5f, .5f);
        trackRect.anchoredPosition = new Vector2(0f, -130f);
        trackRect.sizeDelta = new Vector2(360f, 8f);
        trackObject.GetComponent<Image>().color = new Color(.18f, .23f, .34f, 1f);

        var fillObject = new GameObject("Progress Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(trackRect, false);
        loadingFill = fillObject.GetComponent<RectTransform>();
        loadingFill.anchorMin = Vector2.zero;
        loadingFill.anchorMax = new Vector2(0f, 1f);
        loadingFill.pivot = new Vector2(0f, .5f);
        loadingFill.offsetMin = Vector2.zero;
        loadingFill.offsetMax = Vector2.zero;
        fillObject.GetComponent<Image>().color = new Color(.24f, .78f, .94f, 1f);

        loadingRoot.SetActive(false);
    }

    private void FadeFromBlack()
    {
        if (sceneFade != null) StopCoroutine(sceneFade);
        sceneFade = StartCoroutine(FadeOverlay(1f, 0f, .34f, false, null));
    }

    private IEnumerator FadeOverlay(float from, float to, float duration, bool block, Action completed)
    {
        fadeGroup.alpha = from;
        fadeGroup.blocksRaycasts = block;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Smooth(Mathf.Clamp01(elapsed / duration));
            fadeGroup.alpha = Mathf.LerpUnclamped(from, to, t);
            yield return null;
        }
        fadeGroup.alpha = to;
        fadeGroup.blocksRaycasts = to > .01f;
        sceneFade = null;
        completed?.Invoke();
    }

    private IEnumerator ChangeScene(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        yield return FadeOverlay(fadeGroup.alpha, 1f, .22f, true, null);

        // Fast transitions stay as a clean fade. The branded loader appears
        // only when Unity still has meaningful scene work left to do.
        bool showLoader = operation.progress < .9f;
        if (showLoader)
        {
            loadingRoot.SetActive(true);
            SetLoadingProgress(0f);
            string message = LoadingMessage(sceneName);
            float shownFor = 0f;
            float displayed = 0f;
            const float minimumDisplayTime = .55f;

            while (operation.progress < .9f || shownFor < minimumDisplayTime)
            {
                shownFor += Time.unscaledDeltaTime;
                float actual = Mathf.Clamp01(operation.progress / .9f);
                displayed = Mathf.MoveTowards(displayed, actual, Time.unscaledDeltaTime * 1.8f);
                SetLoadingProgress(displayed);

                int dots = 1 + Mathf.FloorToInt(shownFor * 2.5f) % 3;
                loadingText.text = message + new string('.', dots);
                float bob = Mathf.Sin(shownFor * 4.5f);
                loadingChirbit.anchoredPosition = new Vector2(0f, 72f + bob * 6f);
                loadingChirbit.localScale = Vector3.one * (1f + bob * .02f);
                yield return null;
            }

            SetLoadingProgress(1f);
            loadingText.text = "Ready!";
            yield return new WaitForSecondsRealtime(.08f);
            loadingChirbit.anchoredPosition = new Vector2(0f, 72f);
            loadingChirbit.localScale = Vector3.one;
            loadingRoot.SetActive(false);
        }

        operation.allowSceneActivation = true;
        while (!operation.isDone) yield return null;
    }

    private void SetLoadingProgress(float progress)
    {
        if (loadingFill == null) return;
        Vector2 max = loadingFill.anchorMax;
        max.x = Mathf.Clamp01(progress);
        loadingFill.anchorMax = max;
    }

    private static string LoadingMessage(string sceneName)
    {
        switch (sceneName)
        {
            case "ARScene": return "Preparing your AR workspace";
            case "LessonScene": return "Preparing your lesson";
            case "QuizScene": return "Preparing your quiz";
            case "Hardware_Library": return "Organizing the hardware library";
            case "ActivitySelectionScene": return "Preparing your activities";
            case "COCScene": return "Gathering your lessons";
            case "Start_Learning": return "Opening the learning hub";
            case "MainMenu": return "Returning to ARDENT";
            default: return "Loading your next activity";
        }
    }

    public static void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return;
        if (instance == null) { SceneManager.LoadScene(sceneName); return; }
        if (instance.changingScene) return;
        instance.changingScene = true;
        if (instance.sceneFade != null) instance.StopCoroutine(instance.sceneFade);
        instance.sceneFade = instance.StartCoroutine(instance.ChangeScene(sceneName));
    }

    public static void LoadScene(string sceneName, LoadSceneMode mode)
    {
        // Additive utility scenes (such as the shared UI scene) do not replace
        // the current view, so a full-screen transition would be distracting.
        if (mode == LoadSceneMode.Additive) SceneManager.LoadScene(sceneName, mode);
        else LoadScene(sceneName);
    }

    public static void SetPanelVisible(GameObject panel, bool visible)
    {
        if (panel == null) return;
        var motion = panel.GetComponent<ArdentPanelMotion>();
        if (motion == null) motion = panel.AddComponent<ArdentPanelMotion>();
        motion.Initialize();
        if (visible) motion.Show(); else motion.Hide();
    }

    public static void AnimateDialogue(RectTransform dialogue, TMP_Text message)
    {
        if (dialogue != null)
        {
            var motion = dialogue.GetComponent<ArdentPanelMotion>();
            if (motion == null) motion = dialogue.gameObject.AddComponent<ArdentPanelMotion>();
            motion.Initialize();
            motion.Show(true);
        }
        if (message != null)
        {
            var motion = message.GetComponent<ArdentInstructionMotion>();
            if (motion == null) motion = message.gameObject.AddComponent<ArdentInstructionMotion>();
            motion.Initialize();
            motion.Play();
        }
    }

    public static void HidePanel(GameObject panel, Action completed)
    {
        if (panel == null) { completed?.Invoke(); return; }
        SetPanelVisible(panel, false);
        if (instance == null) { completed?.Invoke(); return; }
        instance.StartCoroutine(CompleteAfterPanelExit(completed));
    }

    private static IEnumerator CompleteAfterPanelExit(Action completed)
    {
        yield return new WaitForSecondsRealtime(.15f);
        completed?.Invoke();
    }

    internal static Coroutine Run(IEnumerator routine) => instance != null ? instance.StartCoroutine(routine) : null;
    internal static void Stop(Coroutine routine) { if (instance != null && routine != null) instance.StopCoroutine(routine); }
    internal static float Smooth(float t) => t * t * (3f - 2f * t);

    private void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }
}

public sealed class ArdentButtonMotion : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    private RectTransform rect;
    private Vector3 restingScale;
    private Coroutine activeRoutine;
    private bool initialized;

    public void Initialize()
    {
        if (initialized) return;
        rect = transform as RectTransform;
        restingScale = rect != null ? rect.localScale : Vector3.one;
        initialized = true;
    }

    private void OnEnable() { if (!initialized) Initialize(); }
    public void OnPointerDown(PointerEventData eventData) => AnimateTo(restingScale * .94f, .075f, false);
    public void OnPointerUp(PointerEventData eventData) => Release();
    public void OnPointerExit(PointerEventData eventData) => Release();
    public void OnSelect(BaseEventData eventData) => AnimateTo(restingScale * 1.025f, .11f, false);
    public void OnDeselect(BaseEventData eventData) => AnimateTo(restingScale, .12f, false);

    private void Release()
    {
        if (!isActiveAndEnabled) return;
        if (activeRoutine != null) ArdentMotion.Stop(activeRoutine);
        activeRoutine = ArdentMotion.Run(ReleaseRoutine());
    }

    private IEnumerator ReleaseRoutine()
    {
        yield return ScaleTo(restingScale * 1.035f, .09f, true);
        yield return ScaleTo(restingScale, .11f, true);
        activeRoutine = null;
    }

    private void AnimateTo(Vector3 target, float duration, bool smooth)
    {
        if (rect == null || !isActiveAndEnabled) return;
        if (activeRoutine != null) ArdentMotion.Stop(activeRoutine);
        activeRoutine = ArdentMotion.Run(ScaleTo(target, duration, smooth));
    }

    private IEnumerator ScaleTo(Vector3 target, float duration, bool smooth)
    {
        Vector3 start = rect.localScale;
        float elapsed = 0f;
        while (elapsed < duration && rect != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rect.localScale = Vector3.LerpUnclamped(start, target, smooth ? ArdentMotion.Smooth(t) : t);
            yield return null;
        }
        if (rect != null) rect.localScale = target;
    }

    private void OnDisable()
    {
        if (activeRoutine != null) ArdentMotion.Stop(activeRoutine);
        activeRoutine = null;
        if (rect != null) rect.localScale = restingScale;
    }
}

public sealed class ArdentPanelMotion : MonoBehaviour
{
    private RectTransform rect;
    private CanvasGroup group;
    private Vector3 restingScale;
    private Coroutine activeRoutine;
    private bool initialized;

    public void Initialize()
    {
        if (initialized) return;
        rect = transform as RectTransform;
        if (rect == null) return;
        restingScale = rect.localScale;
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        initialized = true;
        if (gameObject.activeInHierarchy) Show();
    }

    private void OnEnable()
    {
        if (!initialized) Initialize();
        else Show();
    }

    public void Show(bool replay = false)
    {
        if (!initialized) Initialize();
        if (!initialized) { gameObject.SetActive(true); return; }
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (activeRoutine != null) ArdentMotion.Stop(activeRoutine);
        activeRoutine = ArdentMotion.Run(ShowRoutine(replay));
    }

    public void Hide()
    {
        if (!initialized) Initialize();
        if (!gameObject.activeSelf || !initialized) { gameObject.SetActive(false); return; }
        if (activeRoutine != null) ArdentMotion.Stop(activeRoutine);
        activeRoutine = ArdentMotion.Run(HideRoutine());
    }

    private IEnumerator ShowRoutine(bool replay)
    {
        group.blocksRaycasts = false;
        group.interactable = false;
        group.alpha = replay ? Mathf.Min(group.alpha, .2f) : 0f;
        rect.localScale = restingScale * .94f;
        float elapsed = 0f;
        const float duration = .22f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            group.alpha = t;
            float bounce = 1f + Mathf.Sin(t * Mathf.PI) * .025f;
            rect.localScale = Vector3.LerpUnclamped(restingScale * .94f, restingScale * bounce, ArdentMotion.Smooth(t));
            yield return null;
        }
        group.alpha = 1f;
        rect.localScale = restingScale;
        group.blocksRaycasts = true;
        group.interactable = true;
        activeRoutine = null;
    }

    private IEnumerator HideRoutine()
    {
        group.blocksRaycasts = false;
        group.interactable = false;
        float startAlpha = group.alpha;
        Vector3 startScale = rect.localScale;
        float elapsed = 0f;
        const float duration = .14f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = ArdentMotion.Smooth(Mathf.Clamp01(elapsed / duration));
            group.alpha = Mathf.Lerp(startAlpha, 0f, t);
            rect.localScale = Vector3.LerpUnclamped(startScale, restingScale * .96f, t);
            yield return null;
        }
        rect.localScale = restingScale;
        group.alpha = 1f;
        activeRoutine = null;
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (activeRoutine != null) ArdentMotion.Stop(activeRoutine);
        activeRoutine = null;
        if (rect != null) rect.localScale = restingScale;
        if (group != null) group.alpha = 1f;
    }
}

public sealed class ArdentInstructionMotion : MonoBehaviour
{
    private TMP_Text label;
    private CanvasGroup group;
    private RectTransform rect;
    private Vector2 restingPosition;
    private string lastText;
    private Coroutine activeRoutine;
    private bool initialized;

    public void Initialize()
    {
        if (initialized) return;
        label = GetComponent<TMP_Text>();
        rect = transform as RectTransform;
        if (label == null || rect == null) return;
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        restingPosition = rect.anchoredPosition;
        lastText = label.text;
        initialized = true;
    }

    private void LateUpdate()
    {
        if (!initialized) Initialize();
        if (!initialized || label.text == lastText) return;
        lastText = label.text;
        Play();
    }

    public void Play()
    {
        if (!initialized) Initialize();
        if (!initialized || !gameObject.activeInHierarchy) return;
        lastText = label.text;
        if (activeRoutine != null) ArdentMotion.Stop(activeRoutine);
        activeRoutine = ArdentMotion.Run(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        group.alpha = 0f;
        rect.anchoredPosition = restingPosition + Vector2.down * 8f;
        float elapsed = 0f;
        const float duration = .2f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = ArdentMotion.Smooth(Mathf.Clamp01(elapsed / duration));
            group.alpha = t;
            rect.anchoredPosition = Vector2.LerpUnclamped(restingPosition + Vector2.down * 8f, restingPosition, t);
            yield return null;
        }
        group.alpha = 1f;
        rect.anchoredPosition = restingPosition;
        activeRoutine = null;
    }

    private void OnDisable()
    {
        if (activeRoutine != null) ArdentMotion.Stop(activeRoutine);
        activeRoutine = null;
        if (rect != null) rect.anchoredPosition = restingPosition;
        if (group != null) group.alpha = 1f;
    }
}

internal static class ArdentEntranceMotion
{
    public static void Play(RectTransform rect)
    {
        if (rect == null || rect.GetComponent<ArdentPanelMotion>() != null) return;
        ArdentMotion.Run(Routine(rect));
    }

    private static IEnumerator Routine(RectTransform rect)
    {
        var group = rect.GetComponent<CanvasGroup>();
        if (group == null) group = rect.gameObject.AddComponent<CanvasGroup>();
        Vector2 destination = rect.anchoredPosition;
        group.alpha = 0f;
        rect.anchoredPosition = destination + Vector2.down * 14f;
        yield return null;
        float elapsed = 0f;
        const float duration = .28f;
        while (elapsed < duration && rect != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = ArdentMotion.Smooth(Mathf.Clamp01(elapsed / duration));
            group.alpha = t;
            rect.anchoredPosition = Vector2.LerpUnclamped(destination + Vector2.down * 14f, destination, t);
            yield return null;
        }
        if (rect != null) { group.alpha = 1f; rect.anchoredPosition = destination; }
    }
}
