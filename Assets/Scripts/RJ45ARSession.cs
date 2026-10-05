using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed partial class RJ45ARSession : MonoBehaviour
{
    public GameObject toolbar;
    public TMP_Text instructionsText;
    public TMP_Text standardText;
    public Button standardAButton, standardBButton, restartButton, repositionButton;
    public Button[] ordinaryButtons;
    private ARPlacementManager placement;
    private GameObject workstation;
    private RJ45WireArrangement board;
    private RJ45CablePreparation preparation;
    private RJ45WireTrimming trimming;
    private RJ45ConnectorInsertion insertion;
    private RJ45Crimping crimping;
    private RJ45CableTester tester;
    private RJ45WiringStandard standard = RJ45WiringStandard.T568B;
    private bool initialized, placing, waitForRelease;
    private int placedFrame;
    public bool CanPlace => initialized && placing;

    public void Initialize(ARPlacementManager manager)
    {
        if (initialized) return;
        placement = manager;
        if (placement == null || toolbar == null || instructionsText == null || standardText == null ||
            standardAButton == null || standardBButton == null || restartButton == null || repositionButton == null)
        { Debug.LogError("Run ARDENT > RJ45 > Set Up Tabletop AR to connect the scene UI.", this); return; }
        var data = placement.CurrentActivity;
        if (data == null || data.availableObjects == null || data.availableObjects.Length != 1 ||
            data.availableObjects[0]?.prefab == null || data.availableObjects[0].prefab.GetComponentInChildren<RJ45WireArrangement>(true) == null)
        { Debug.LogError("RJ45 requires exactly one workstation prefab with a wire board.",this); return; }
        initialized = true;
        standardAButton.onClick.AddListener(SelectA); standardBButton.onClick.AddListener(SelectB);
        restartButton.onClick.AddListener(Restart); repositionButton.onClick.AddListener(Reposition);
        toolbar.SetActive(true); instructionsText.gameObject.SetActive(true);
        Reposition();
    }
    public void Reposition()
    {
        if (!initialized || ARCheckpointSession.BlocksInput) return;
        placing = true;
        if (board != null) board.inputEnabled = false;
        if (preparation != null) preparation.inputEnabled = false;
        if (trimming != null) trimming.inputEnabled = false;
        if (insertion != null) insertion.inputEnabled = false;
        if (crimping != null) crimping.inputEnabled = false;
        if (tester != null) tester.inputEnabled = false;
        if (workstation != null) workstation.SetActive(false);
        placement.SelectObject(placement.CurrentActivity.availableObjects[0]);
        instructionsText.text = "Scan a horizontal table, then tap to place the RJ45 workstation.\nYour wire arrangement is kept when repositioning.";
    }
    public GameObject PlaceWorkstation(GameObject prefab, Pose pose, Transform parent)
    {
        if (!CanPlace || Camera.main == null) return null;
        if (Vector3.Dot(pose.rotation * Vector3.up, Vector3.up) < .95f)
        { instructionsText.text = "Choose a horizontal tabletop or floor, rather than a wall."; return null; }
        var forward = Vector3.ProjectOnPlane(pose.position - Camera.main.transform.position, Vector3.up);
        if (forward.sqrMagnitude < .001f) forward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up);
        if (forward.sqrMagnitude < .001f) forward = Vector3.forward;
        var rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        if (workstation == null)
        {
            workstation = Instantiate(prefab, pose.position + Vector3.up * .012f, rotation, parent);
            board = workstation.GetComponentInChildren<RJ45WireArrangement>(true);
            board.interactionCamera = Camera.main; board.fitOrthographicCamera = false;
            board.instructionsText = instructionsText;
            // Standard buttons are owned by this session, including before placement.
            if (standard == RJ45WiringStandard.T568A) board.SelectA();
            else board.Restart();
            preparation = workstation.GetComponentInChildren<RJ45CablePreparation>(true);
            if (preparation != null) preparation.Initialize(instructionsText);
            trimming = workstation.GetComponentInChildren<RJ45WireTrimming>(true);
            insertion = workstation.GetComponentInChildren<RJ45ConnectorInsertion>(true);
            crimping = workstation.GetComponentInChildren<RJ45Crimping>(true);
            tester = workstation.GetComponentInChildren<RJ45CableTester>(true);
        }
        else
        {
            workstation.transform.SetPositionAndRotation(pose.position + Vector3.up * .012f, rotation);
            workstation.SetActive(true);
            instructionsText.text = "Workstation repositioned. Continue your activity.";
            preparation?.RefreshInstructions();
            trimming?.RefreshInstructions();
            insertion?.RefreshInstructions();
            crimping?.RefreshInstructions();
            tester?.RefreshInstructions();
        }
        placing = false; placedFrame = Time.frameCount; waitForRelease = true; board.inputEnabled = false;
        ARCheckpointSession.SaveCurrent();
        return workstation;
    }
    private void SelectA() { Select(RJ45WiringStandard.T568A); }
    private void SelectB() { Select(RJ45WiringStandard.T568B); }
    private void Select(RJ45WiringStandard value)
    {
        if (ARCheckpointSession.BlocksInput) return;
        if (standard == value || (trimming != null && trimming.HasStarted)) return;
        standard = value;
        if (board != null) { if (value == RJ45WiringStandard.T568A) board.SelectA(); else board.SelectB(); }
        preparation?.RefreshInstructions();
    }
    private void Restart()
    {
        if (board == null || placing || ARCheckpointSession.BlocksInput) return;
        changingCheckpoint = true;
        try
        {
        tester?.ResetTester();
        crimping?.ResetCrimping();
        insertion?.ResetInsertion();
        trimming?.ResetTrimming();
        if (preparation != null) preparation.ResetPreparation(); else board.Restart();
        }
        finally { changingCheckpoint = false; }
        ARCheckpointSession.SaveCurrent();
    }
    private void LateUpdate()
    {
        if (!initialized) return;
        if (ordinaryButtons != null) foreach (var button in ordinaryButtons) ARButtonAvailability.Set(button, false);
        standardText.text = "Standard: " + standard;
        standardAButton.interactable = standard != RJ45WiringStandard.T568A && !placing && !ARCheckpointSession.BlocksInput && (trimming == null || !trimming.HasStarted);
        standardBButton.interactable = standard != RJ45WiringStandard.T568B && !placing && !ARCheckpointSession.BlocksInput && (trimming == null || !trimming.HasStarted);
        restartButton.interactable = board != null && !placing && !ARCheckpointSession.BlocksInput;
        repositionButton.interactable = workstation != null && !placing && !ARCheckpointSession.BlocksInput;
        if (board == null || placing) return;
        if (waitForRelease)
        {
            bool held = Mouse.current != null && Mouse.current.leftButton.isPressed;
            if (Touchscreen.current != null) foreach (var touch in Touchscreen.current.touches) held |= touch.press.isPressed;
            if (Time.frameCount <= placedFrame || held) return;
            waitForRelease = false;
        }
        bool canInteract = !UIManager.HasOpenPanel && !SandboxInventoryPanel.IsOpen && !ARCheckpointSession.BlocksInput;
        if (preparation != null) preparation.inputEnabled = canInteract;
        if (trimming != null) trimming.inputEnabled = canInteract;
        if (insertion != null) insertion.inputEnabled = canInteract;
        if (crimping != null) crimping.inputEnabled = canInteract;
        if (tester != null) tester.inputEnabled = canInteract;
        board.inputEnabled = canInteract && (preparation == null || preparation.Ready) && (trimming == null || !trimming.HasStarted);
    }
    private void OnDestroy()
    {
        if (!initialized) return;
        standardAButton?.onClick.RemoveListener(SelectA); standardBButton?.onClick.RemoveListener(SelectB);
        restartButton?.onClick.RemoveListener(Restart); repositionButton?.onClick.RemoveListener(Reposition);
    }
}






