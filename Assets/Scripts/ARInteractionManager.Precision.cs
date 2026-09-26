using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class ARInteractionManager
{
    public enum PrecisionAction { Height, TiltX, TiltZ }

    [Header("Height and Tilt Controls")]
    [SerializeField] private bool enablePrecisionControls = true;
    [Tooltip("World units per second at the original assembly size.")]
    [SerializeField, Min(0.001f)] private float heightSpeed = 0.15f;
    [SerializeField, Min(1f)] private float tiltSpeed = 35f;
    [Tooltip("Optional parent for the generated controls. Defaults to the AR Canvas.")]
    [SerializeField] private RectTransform precisionControlsParent;
    [SerializeField] private Vector2 precisionControlsPosition = new Vector2(-24f, 0f);

    private GameObject precisionPanel;
    private TMP_Text precisionTitle;
    private Button[] precisionButtons;
    private Button alignRotationButton;
    private ARObjectManipulator precisionTarget;
    private PrecisionAction precisionAction;
    private float precisionDirection;
    private bool suppressWorldInputUntilRelease;

    private bool CanAdjustSelection => enablePrecisionControls && selectedObject != null &&
        !selectedObject.IsLocked && placementManager != null && placementManager.IsAssemblyActivity &&
        GetCurrentMode() == ARInteractionMode.Edit;

    private bool ActionAllowed(PrecisionAction action)
    {
        return placementManager != null && (action == PrecisionAction.Height
            ? placementManager.AllowMovement : placementManager.AllowRotation);
    }

    // Returns true when the button gesture owns input, including its release frame.
    private bool UpdatePrecisionControls()
    {
        bool visible = CanAdjustSelection;
        if (visible && precisionPanel == null) CreatePrecisionPanel();
        if (precisionPanel != null)
        {
            if (precisionPanel.activeSelf != visible) precisionPanel.SetActive(visible);
            if (visible)
            {
                var info = selectedObject.GetComponent<ARObjectInfo>();
                precisionTitle.text = "Adjust " + (info != null ? info.objectName : selectedObject.name);
                for (int i = 0; i < precisionButtons.Length; i++)
                    precisionButtons[i].interactable = ActionAllowed((PrecisionAction)(i / 2));
                alignRotationButton.interactable = placementManager.AllowRotation && assemblyManager != null &&
                    assemblyManager.Phase == ARAssemblyManager.ActivityPhase.Assembly &&
                    assemblyManager.IsCurrentStepComponent(selectedObject.gameObject);
            }
        }

        if (precisionTarget != null)
        {
            if (!visible || selectedObject != precisionTarget || !ActionAllowed(precisionAction))
                CancelPrecisionAdjustment();
            else
                ApplyPrecisionStep(Mathf.Min(Time.unscaledDeltaTime, 0.05f));
        }

        if (!suppressWorldInputUntilRelease) return false;
#if !UNITY_EDITOR
        twoFingerGestureActive = false;
#endif
        if (!AnyAdjustmentPointerPressed())
        {
            // PointerUp usually ends the gesture. Also handle a lost UI release.
            if (precisionTarget != null) EndPrecisionAdjustment();
            suppressWorldInputUntilRelease = false;
        }
        return true;
    }

    private bool AnyAdjustmentPointerPressed()
    {
#if UNITY_EDITOR
        return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
        if (Touchscreen.current != null)
            foreach (var touch in Touchscreen.current.touches)
                if (touch.press.isPressed) return true;
        return false;
#endif
    }

    public bool BeginPrecisionAdjustment(PrecisionAction action, float direction)
    {
        if (!CanAdjustSelection || !ActionAllowed(action) || precisionTarget != null)
            return false;
        precisionTarget = selectedObject;
        precisionAction = action;
        precisionDirection = Mathf.Sign(direction);
        suppressWorldInputUntilRelease = true;
#if !UNITY_EDITOR
        twoFingerGestureActive = false;
#endif
        // A short tap nudges; a hold continues smoothly in Update.
        ApplyPrecisionStep(1f / 60f);
        return true;
    }

    private void ApplyPrecisionStep(float seconds)
    {
        if (precisionTarget == null || precisionTarget.IsLocked) return;
        if (precisionAction == PrecisionAction.Height)
        {
            float scale = placementManager.AssemblyRoot != null
                ? placementManager.AssemblyRoot.TransformVector(Vector3.up).magnitude : 1f;
            precisionTarget.MoveVertical(Mathf.Max(0.001f, heightSpeed) * scale * seconds * precisionDirection);
        }
        else
        {
            Vector3 axis = precisionAction == PrecisionAction.TiltX ? Vector3.right : Vector3.forward;
            precisionTarget.TiltLocal(axis, Mathf.Max(1f, tiltSpeed) * seconds * precisionDirection);
        }
    }

    public void EndPrecisionAdjustment()
    {
        var target = precisionTarget;
        precisionTarget = null;
        if (target != null && target == selectedObject && !target.IsLocked && CanAdjustSelection)
            CheckAssemblyStep(target.gameObject);
    }

    public void CancelPrecisionAdjustment()
    {
        // Keep world input suppressed until the initiating finger/mouse is released.
        precisionTarget = null;
    }

    private void OnDisable() { CancelPrecisionAdjustment(); if (precisionPanel != null) precisionPanel.SetActive(false); }
    private void OnApplicationFocus(bool focused) { if (!focused) CancelPrecisionAdjustment(); }
    private void OnApplicationPause(bool paused) { if (paused) CancelPrecisionAdjustment(); }
    private void OnDestroy() { if (precisionPanel != null) Destroy(precisionPanel); }

    private void CreatePrecisionPanel()
    {
        Transform parent = precisionControlsParent;
        if (parent == null)
        {
            Canvas fallback = null;
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace) continue;
                fallback = canvas;
                if (canvas.name == "AR Canvas") break;
            }
            if (fallback == null) return;
            parent = fallback.transform;
        }
        precisionPanel = new GameObject("HeightAndTiltControls", typeof(RectTransform), typeof(Image));
        var panelRect = precisionPanel.GetComponent<RectTransform>();
        panelRect.SetParent(parent, false);
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.anchoredPosition = precisionControlsPosition;
        panelRect.sizeDelta = new Vector2(360f, 318f);
        precisionPanel.GetComponent<Image>().color = new Color(0.04f, 0.09f, 0.12f, 0.94f);
        TMP_FontAsset font = null;
        var existingText = parent.GetComponentInChildren<TMP_Text>();
        if (existingText != null) font = existingText.font;
        precisionTitle = MakePrecisionLabel(panelRect, "", font, 23f);
        var titleRect = precisionTitle.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = Vector2.one;
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(12f, -48f);
        titleRect.offsetMax = new Vector2(-12f, -6f);
        precisionTitle.overflowMode = TextOverflowModes.Ellipsis;

        precisionButtons = new Button[6];
        string[] labels = { "Lower", "Raise", "Tilt X -", "Tilt X +", "Tilt Z -", "Tilt Z +" };
        for (int i = 0; i < labels.Length; i++)
        {
            var buttonObject = new GameObject(labels[i], typeof(RectTransform), typeof(Image), typeof(Button), typeof(ARPrecisionHoldButton));
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(panelRect, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(12f + (i % 2) * 172f, -54f - (i / 2) * 64f);
            rect.sizeDelta = new Vector2(164f, 56f);
            buttonObject.GetComponent<Image>().color = new Color(0.06f, 0.40f, 0.48f, 1f);
            var button = buttonObject.GetComponent<Button>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            precisionButtons[i] = button;
            var label = MakePrecisionLabel(rect, labels[i], font, 24f);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            buttonObject.GetComponent<ARPrecisionHoldButton>().Configure(this, (PrecisionAction)(i / 2), i % 2 == 0 ? -1f : 1f);
        }
        var alignObject = new GameObject("AlignRotationButton", typeof(RectTransform), typeof(Image), typeof(Button));
        var alignRect = alignObject.GetComponent<RectTransform>();
        alignRect.SetParent(panelRect, false);
        alignRect.anchorMin = alignRect.anchorMax = alignRect.pivot = new Vector2(0f, 1f);
        alignRect.anchoredPosition = new Vector2(12f, -246f);
        alignRect.sizeDelta = new Vector2(336f, 56f);
        alignObject.GetComponent<Image>().color = new Color(0.10f, 0.30f, 0.44f, 1f);
        alignRotationButton = alignObject.GetComponent<Button>();
        alignRotationButton.navigation = new Navigation { mode = Navigation.Mode.None };
        alignRotationButton.onClick.AddListener(AlignSelectedRotation);
        var alignLabel = MakePrecisionLabel(alignRect, "Align Rotation", font, 24f);
        alignLabel.rectTransform.anchorMin = Vector2.zero;
        alignLabel.rectTransform.anchorMax = Vector2.one;
        alignLabel.rectTransform.offsetMin = alignLabel.rectTransform.offsetMax = Vector2.zero;
    }

    public void AlignSelectedRotation()
    {
        if (!CanAdjustSelection || !placementManager.AllowRotation || assemblyManager == null) return;
        CancelWorldDrag();
        CancelPrecisionAdjustment();
        assemblyManager.TryAlignCurrentComponentRotation(selectedObject.gameObject);
        // No automatic position jump or completion: the next deliberate drag/adjustment
        // release still runs the normal distance and angle checks.
    }

    private TMP_Text MakePrecisionLabel(Transform parent, string text, TMP_FontAsset font, float fontSize)
    {
        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);
        var label = labelObject.GetComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }
}
