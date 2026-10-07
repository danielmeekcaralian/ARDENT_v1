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
    [Header("Scene Adjustment UI")]
    [SerializeField] private Button adjustButton;
    [SerializeField] private GameObject precisionPanel;
    [SerializeField] private TMP_Text precisionTitle;
    [SerializeField] private Button lowerButton;
    [SerializeField] private Button raiseButton;
    [SerializeField] private Button tiltXMinusButton;
    [SerializeField] private Button tiltXPlusButton;
    [SerializeField] private Button tiltZMinusButton;
    [SerializeField] private Button tiltZPlusButton;
    [SerializeField] private Button alignRotationButton;
    [SerializeField] private Button closeAdjustmentButton;

    private Button[] precisionButtons;
    private bool precisionUIReady;
    private bool precisionUIAttempted;
    private bool precisionPanelOpen;
    private ARObjectManipulator precisionSelection;
    private ARObjectManipulator precisionTarget;
    private PrecisionAction precisionAction;
    private float precisionDirection;
    private bool suppressWorldInputUntilRelease;

    private bool CanAdjustSelection => enablePrecisionControls && selectedObject != null &&
        !selectedObject.IsLocked && placementManager != null && (placementManager.IsAssemblyActivity || placementManager.IsSandboxActivity || placementManager.IsNetworkActivity) &&
        GetCurrentMode() == ARInteractionMode.Edit;

    private bool ActionAllowed(PrecisionAction action)
    {
        return placementManager != null && (action == PrecisionAction.Height
            ? placementManager.AllowMovement : placementManager.AllowRotation);
    }

    // Returns true when the button gesture owns input, including its release frame.
    private bool UpdatePrecisionControls()
    {
        InitializePrecisionUI();
        bool visible = precisionUIReady && CanAdjustSelection;
        if (!visible || precisionSelection != selectedObject) ClosePrecisionPanel();
        precisionSelection = selectedObject;
        ARButtonAvailability.Set(adjustButton, visible);
        if (precisionUIReady)
        {
            bool showPanel = visible && precisionPanelOpen;
            if (precisionPanel.activeSelf != showPanel) precisionPanel.SetActive(showPanel);
            if (visible)
            {
                precisionTitle.text = "Adjust " + HardwareProfileCatalog.InstanceDisplayName(selectedObject.gameObject);
                for (int i = 0; i < precisionButtons.Length; i++)
                    precisionButtons[i].interactable = ActionAllowed((PrecisionAction)(i / 2));
                alignRotationButton.interactable = placementManager.AllowRotation && assemblyManager != null &&
                    ((placementManager.IsSandboxActivity && assemblyManager.CanAlignSandboxComponent(selectedObject.gameObject)) ||
                     (assemblyManager.Phase == ARAssemblyManager.ActivityPhase.Assembly &&
                      assemblyManager.IsCurrentStepComponent(selectedObject.gameObject)));
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
        if (!precisionUIReady || !precisionPanelOpen || !precisionPanel.activeInHierarchy ||
            !CanAdjustSelection || !ActionAllowed(action) || precisionTarget != null)
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

    private void OnEnable() { InitializePrecisionUI(); }
    private void OnDisable()
    {
        ClosePrecisionPanel();
        if (adjustButton != null) adjustButton.gameObject.SetActive(false);
    }
    private void OnApplicationFocus(bool focused) { if (!focused) CancelPrecisionAdjustment(); }
    private void OnApplicationPause(bool paused) { if (paused) CancelPrecisionAdjustment(); }
    private void OnDestroy()
    {
        // These objects belong to the scene, not to this component.
        if (adjustButton != null) adjustButton.onClick.RemoveListener(TogglePrecisionPanel);
        if (closeAdjustmentButton != null) closeAdjustmentButton.onClick.RemoveListener(ClosePrecisionPanel);
        if (alignRotationButton != null) alignRotationButton.onClick.RemoveListener(AlignSelectedRotation);
    }

    private void InitializePrecisionUI()
    {
        if (precisionUIAttempted) return;
        precisionUIAttempted = true;
        precisionButtons = new[] { lowerButton, raiseButton, tiltXMinusButton,
            tiltXPlusButton, tiltZMinusButton, tiltZPlusButton };
        bool complete = adjustButton != null && precisionPanel != null && precisionTitle != null &&
            closeAdjustmentButton != null && alignRotationButton != null;
        foreach (var button in precisionButtons) complete &= button != null;
        if (precisionPanel != null) precisionPanel.SetActive(false);
        if (adjustButton != null) adjustButton.gameObject.SetActive(false);
        if (!complete)
        {
            Debug.LogWarning("Assign all Scene Adjustment UI fields on ARInteractionManager. No panel will be generated.", this);
            return;
        }
        for (int i = 0; i < precisionButtons.Length; i++)
        {
            var button = precisionButtons[i];
            var hold = button.GetComponent<ARPrecisionHoldButton>();
            if (hold == null) hold = button.gameObject.AddComponent<ARPrecisionHoldButton>();
            hold.Configure(this, (PrecisionAction)(i / 2), i % 2 == 0 ? -1f : 1f);
        }
        adjustButton.onClick.AddListener(TogglePrecisionPanel);
        closeAdjustmentButton.onClick.AddListener(ClosePrecisionPanel);
        alignRotationButton.onClick.AddListener(AlignSelectedRotation);
        precisionUIReady = true;
    }

    public void TogglePrecisionPanel()
    {
        if (!precisionUIReady || !CanAdjustSelection) return;
        if (precisionPanelOpen) { ClosePrecisionPanel(); return; }
        CancelWorldDrag();
        CancelPrecisionAdjustment();
        precisionSelection = selectedObject;
        precisionPanelOpen = true;
        ArdentMotion.SetPanelVisible(precisionPanel, true);
    }

    public void ClosePrecisionPanel()
    {
        CancelPrecisionAdjustment();
        precisionPanelOpen = false;
        if (precisionPanel != null && precisionPanel.activeSelf) ArdentMotion.SetPanelVisible(precisionPanel, false);
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

}
