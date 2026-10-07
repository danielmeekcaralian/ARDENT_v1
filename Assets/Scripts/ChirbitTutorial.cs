using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChirbitTutorial : MonoBehaviour
{
    public enum Corner { Auto, TopLeft, TopRight, BottomLeft, BottomRight }
    [Serializable] public class Step { public RectTransform target; [TextArea(2,5)] public string message; public Corner preferredCorner; }
    public bool useCorners;
    public bool autoStart = true;
    public bool sandboxOnly;
    public bool IsReady => initialized;
    public bool IsShowing => showing;
    public event Action<bool> Completed;
    private static ChirbitTutorial activeAR;
    public static bool BlocksARInput => activeAR != null && activeAR.showing;
    private Corner chosenCorner;
    private Vector2 lastSafeSize;
    public RectTransform root, dialogue, highlight;
    public Image topShade, bottomShade, leftShade, rightShade;
    public TMP_Text messageText, stepText;
    public Button backButton, nextButton, skipButton;
    public Step[] steps;
    public float spotlightPadding = 12, dialogueMargin = 24;
    [Tooltip("Leave empty to use this scene's name. Each tutorial remembers completion separately.")]
    public string tutorialID;
    private string TutorialID => string.IsNullOrWhiteSpace(tutorialID) ? gameObject.scene.name : tutorialID.Trim();
    private string SeenKey => "ARDENT.Chirbit." + TutorialID + ".v1";
    private bool initialized;
    private static bool replayRequested;
    private int index;
    private bool showing;
    private GameObject previousSelection;
    private bool previousNavigation;
    private EventSystem events;
    private readonly Vector3[] corners = new Vector3[4];
    private float authoredWidth, authoredHeight;
    private float authoredCenterX;
    private Color borderColor;
    private Graphic border;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() { replayRequested = false; activeAR = null; }
    private IEnumerator Start()
    {
        if (!Valid()) { Debug.LogError("Chirbit tutorial references are incomplete.", this); yield break; }
        root.gameObject.SetActive(false);
        // Let the library populate its category rows and finish layout first.
        yield return null;
        Canvas.ForceUpdateCanvases();
        authoredWidth = dialogue.rect.width; authoredHeight = dialogue.rect.height;
        authoredCenterX = root.InverseTransformPoint(
            dialogue.TransformPoint(dialogue.rect.center)
        ).x;
        // The clear portion is still modal during this introductory, Next-driven tour.
        var blocker = root.GetComponent<Image>();
        if (blocker == null) blocker = root.gameObject.AddComponent<Image>();
        blocker.color = Color.clear; blocker.raycastTarget = true;
        border = highlight != null ? highlight.GetComponent<Graphic>() : null;
        if (border != null) { border.raycastTarget = false; borderColor = border.color; }
        backButton.onClick.AddListener(Back); nextButton.onClick.AddListener(Next); skipButton.onClick.AddListener(Skip);
        root.gameObject.SetActive(false);
        initialized = true;
        bool replayThisTour = replayRequested && TutorialID == "MainMenu";
        if (replayThisTour) replayRequested = false;
        if (autoStart && (replayThisTour || PlayerPrefs.GetInt(SeenKey, 0) == 0)) Begin();
    }
    private bool Valid() => root != null && dialogue != null && topShade != null && bottomShade != null && leftShade != null && rightShade != null && messageText != null && stepText != null && backButton != null && nextButton != null && skipButton != null && steps != null && steps.Length > 0;
    [ContextMenu("Replay This Tutorial (Play Mode)")]
    public void Begin()
    {
        if (!initialized || !Valid() || showing || (sandboxOnly && !ARSandboxSession.IsActive)) return;
        events = EventSystem.current;
        if (events != null) { previousSelection = events.currentSelectedGameObject; previousNavigation = events.sendNavigationEvents; events.sendNavigationEvents = false; events.SetSelectedGameObject(null); }
        if (gameObject.scene.name == "ARScene") activeAR = this;
        showing = true; index = 0; root.SetAsLastSibling(); root.gameObject.SetActive(true); Show();
    }
    private void Show()
    {
        chosenCorner = Corner.Auto;
        messageText.text = steps[index].message; stepText.text = (index + 1) + " / " + steps.Length;
        backButton.interactable = index > 0;
        var label = nextButton.GetComponentInChildren<TMP_Text>(); if (label != null) label.text = index == steps.Length - 1 ? "Done" : "Next";
        Canvas.ForceUpdateCanvases(); Layout();
        ArdentMotion.AnimateDialogue(dialogue, messageText);
    }
    private void Back() { if (showing && index > 0) { index--; Show(); } }
    private void Next() { if (!showing) return; if (index == steps.Length - 1) Finish(); else { index++; Show(); } }
    private void Finish() { End(false); }
    private void Skip() { End(true); }
    private void End(bool skipped) { PlayerPrefs.SetInt(SeenKey, 1); PlayerPrefs.Save(); Close(); Completed?.Invoke(skipped); }
    private void Close()
    {
        if (!showing) return; showing = false; if (activeAR == this) activeAR = null;
        if (events != null) { events.sendNavigationEvents = previousNavigation; if (previousSelection != null && previousSelection.activeInHierarchy) events.SetSelectedGameObject(previousSelection); }
        if (root != null)
        {
            GameObject overlay = root.gameObject;
            ArdentMotion.HidePanel(dialogue != null ? dialogue.gameObject : overlay,
                () => { if (overlay != null) overlay.SetActive(false); });
        }
    }
    private void LateUpdate() { if (showing) Layout(); }
    private void Layout()
    {
        Rect bounds = root.rect;
        Rect hole = new Rect(bounds.center, Vector2.zero);
        var target = steps[index].target;
        bool visible = target != null && target.gameObject.activeInHierarchy;
        if (visible)
        {
            target.GetWorldCorners(corners);
            Vector2 min = new Vector2(float.MaxValue,float.MaxValue), max = new Vector2(float.MinValue,float.MinValue);
            foreach (var corner in corners) { Vector2 p = root.InverseTransformPoint(corner); min = Vector2.Min(min,p); max = Vector2.Max(max,p); }
            min -= Vector2.one * spotlightPadding; max += Vector2.one * spotlightPadding;
            hole = Rect.MinMaxRect(Mathf.Clamp(min.x,bounds.xMin,bounds.xMax),Mathf.Clamp(min.y,bounds.yMin,bounds.yMax),Mathf.Clamp(max.x,bounds.xMin,bounds.xMax),Mathf.Clamp(max.y,bounds.yMin,bounds.yMax));
        }
        Place(topShade.rectTransform, bounds.xMin,hole.yMax,bounds.width,bounds.yMax-hole.yMax);
        Place(bottomShade.rectTransform,bounds.xMin,bounds.yMin,bounds.width,hole.yMin-bounds.yMin);
        Place(leftShade.rectTransform,bounds.xMin,hole.yMin,hole.xMin-bounds.xMin,hole.height);
        Place(rightShade.rectTransform,hole.xMax,hole.yMin,bounds.xMax-hole.xMax,hole.height);
        if (highlight != null && border != null && (border as Image)?.sprite != null)
        {
            highlight.gameObject.SetActive(visible);
            Place(highlight,hole.xMin,hole.yMin,hole.width,hole.height);
            var c = borderColor; c.a *= .75f + .25f * Mathf.Sin(Time.unscaledTime * 3f); border.color = c;
        }
        Rect safe = SafeBounds();
        if (useCorners) { PlaceInCorner(safe, hole); return; }

        // Dialogue Margin now affects only top and bottom.
        float margin = Mathf.Clamp(dialogueMargin, 0, safe.height * .25f);
        float height = Mathf.Min(authoredHeight, safe.height - margin * 2);

        bool above = hole.center.y < safe.center.y;
        float y = above
            ? safe.yMax - margin - height
            : safe.yMin + margin;

        // Preserve the horizontal position and width you set in the scene.
        Place(
            dialogue,
            authoredCenterX - authoredWidth / 2f,
            y,
            authoredWidth,
            height
        );

        // Keep vertically protruding Chirbit artwork inside the safe area.
        var content = RectTransformUtility.CalculateRelativeRectTransformBounds(
            root, dialogue
        );

        float dy = content.min.y < safe.yMin + margin
            ? safe.yMin + margin - content.min.y
            : content.max.y > safe.yMax - margin
                ? safe.yMax - margin - content.max.y
                : 0f;

        dialogue.anchoredPosition += new Vector2(0f, dy);
    }
    private void PlaceInCorner(Rect safe, Rect hole)
    {
        float margin = Mathf.Clamp(dialogueMargin,0,Mathf.Min(safe.width,safe.height)*.2f);
        // Measure the panel together with any artwork protruding beyond it.
        Place(dialogue,0,0,authoredWidth,authoredHeight);
        Bounds content = RectTransformUtility.CalculateRelativeRectTransformBounds(root,dialogue);
        Vector2 offset = content.min;
        Vector2 size = content.size;
        if (chosenCorner == Corner.Auto || lastSafeSize != safe.size)
        {
            chosenCorner = steps[index].preferredCorner;
            if (chosenCorner == Corner.Auto)
            {
                float best = float.MaxValue;
                for (int i=1;i<=4;i++)
                {
                    Rect candidate = CornerRect(safe,size,margin,(Corner)i);
                    float overlap = Mathf.Max(0,Mathf.Min(candidate.xMax,hole.xMax)-Mathf.Max(candidate.xMin,hole.xMin)) *
                        Mathf.Max(0,Mathf.Min(candidate.yMax,hole.yMax)-Mathf.Max(candidate.yMin,hole.yMin));
                    if (overlap < best) { best=overlap; chosenCorner=(Corner)i; }
                }
            }
            lastSafeSize=safe.size;
        }
        Rect dest=CornerRect(safe,size,margin,chosenCorner);
        Place(dialogue,dest.x-offset.x,dest.y-offset.y,authoredWidth,authoredHeight);
    }
    private static Rect CornerRect(Rect safe,Vector2 size,float margin,Corner corner)
    {
        bool left=corner==Corner.TopLeft||corner==Corner.BottomLeft;
        bool top=corner==Corner.TopLeft||corner==Corner.TopRight;
        float x=left?safe.xMin+margin:safe.xMax-margin-size.x;
        float y=top?safe.yMax-margin-size.y:safe.yMin+margin;
        return new Rect(x,y,size.x,size.y);
    }
    private Rect SafeBounds()
    {
        var canvas = root.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Rect pixels = Screen.safeArea;
        if (pixels.width <= 0 || pixels.height <= 0) return root.rect;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root,pixels.min,camera,out var min) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(root,pixels.max,camera,out var max)) return root.rect;
        Rect bounds = root.rect;
        float left = Mathf.Max(bounds.xMin,Mathf.Min(min.x,max.x));
        float right = Mathf.Min(bounds.xMax,Mathf.Max(min.x,max.x));
        float bottom = Mathf.Max(bounds.yMin,Mathf.Min(min.y,max.y));
        float top = Mathf.Min(bounds.yMax,Mathf.Max(min.y,max.y));
        return right > left && top > bottom ? Rect.MinMaxRect(left,bottom,right,top) : bounds;
    }
    private void Place(RectTransform rect,float x,float y,float width,float height)
    {
        // All overlay objects use the root's coordinate space; shade parent stretches to it.
        rect.anchorMin = rect.anchorMax = Vector2.zero; rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(x-root.rect.xMin,y-root.rect.yMin);
        rect.sizeDelta = new Vector2(Mathf.Max(0,width),Mathf.Max(0,height));
    }
    private void OnDisable() { Close(); }
    private void OnDestroy()
    {
        if (backButton != null) backButton.onClick.RemoveListener(Back);
        if (nextButton != null) nextButton.onClick.RemoveListener(Next);
        if (skipButton != null) skipButton.onClick.RemoveListener(Skip);
    }
    public static void ReplayMainMenu() { replayRequested = true; ArdentMotion.LoadScene("MainMenu"); }
}
