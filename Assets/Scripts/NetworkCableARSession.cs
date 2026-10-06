using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed partial class NetworkCableARSession : MonoBehaviour
{
    public GameObject toolbar;
    public TMP_Text instructionsText;
    public TMP_Text stageText;
    public Button restartButton;
    public Button repositionButton;
    public Button[] ordinaryButtons;

    private ARPlacementManager placement;
    private ARActivityData activity;
    private GameObject workstationObject;
    private NetworkCableWorkstation workstation;
    private bool initialized;
    private bool placing;
    private bool waitForRelease;
    private int placedFrame;

    public bool CanPlace => initialized && placing;

    public void Initialize(ARPlacementManager manager)
    {
        if (initialized) return;
        placement = manager;
        activity = placement != null ? placement.CurrentActivity : null;
        if (placement == null || toolbar == null || instructionsText == null || stageText == null ||
            restartButton == null || repositionButton == null)
        {
            Debug.LogError("Run ARDENT > Network Cables > Set Up Complete AR Activity to connect the scene UI.", this);
            return;
        }
        if (activity == null || activity.activityType != ARActivityType.NetworkCables ||
            activity.availableObjects == null || activity.availableObjects.Length != 1 ||
            activity.availableObjects[0]?.prefab == null ||
            activity.availableObjects[0].prefab.GetComponent<NetworkCableWorkstation>() == null)
        {
            Debug.LogError("Network Cables requires exactly one configured workstation prefab.", this);
            return;
        }

        initialized = true;
        restartButton.onClick.AddListener(RestartActivity);
        repositionButton.onClick.AddListener(Reposition);
        toolbar.SetActive(true);
        instructionsText.gameObject.SetActive(true);
        Reposition();
    }

    public void Reposition()
    {
        if (!initialized || ARCheckpointSession.BlocksInput) return;
        placing = true;
        if (workstation != null) workstation.inputEnabled = false;
        if (workstationObject != null) workstationObject.SetActive(false);
        placement.SelectObject(activity.availableObjects[0]);
        stageText.text = ProgressLabel(workstation != null ? workstation.CompletedSteps : 0);
        instructionsText.text = "Scan a horizontal table, then tap it to place the Network Cables workstation.";
    }

    public GameObject PlaceWorkstation(GameObject prefab, Pose pose, Transform parent)
    {
        if (!CanPlace || Camera.main == null) return null;
        if (Vector3.Dot(pose.rotation * Vector3.up, Vector3.up) < .95f)
        {
            instructionsText.text = "Choose a horizontal tabletop or floor rather than a wall.";
            return null;
        }

        Vector3 forward = Vector3.ProjectOnPlane(pose.position - Camera.main.transform.position, Vector3.up);
        if (forward.sqrMagnitude < .001f) forward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up);
        if (forward.sqrMagnitude < .001f) forward = Vector3.forward;
        Quaternion rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);

        if (workstationObject == null)
        {
            workstationObject = Instantiate(prefab, pose.position + Vector3.up * .012f, rotation, parent);
            workstation = workstationObject.GetComponent<NetworkCableWorkstation>();
            if (workstation == null) { Destroy(workstationObject); workstationObject = null; return null; }
            workstation.Initialize(this, Camera.main);
        }
        else
        {
            workstationObject.transform.SetPositionAndRotation(pose.position + Vector3.up * .012f, rotation);
            workstationObject.SetActive(true);
        }

        placing = false;
        placedFrame = Time.frameCount;
        waitForRelease = true;
        workstation.inputEnabled = false;
        stageText.text = ProgressLabel(workstation.CompletedSteps);
        if (workstation.CompletedSteps == 0) ShowCurrentInstruction();
        else instructionsText.text = "Workstation repositioned. " + CurrentPrompt(workstation.CompletedSteps);
        return workstationObject;
    }

    public void ReportMiss(string message)
    {
        instructionsText.text = message + " " + CurrentPrompt(workstation.CompletedSteps);
    }

    public void ReportWrongCable(NetworkCableKind selected, NetworkCableKind expected)
    {
        ArdentAudioManager.Play(ArdentSound.Error);
        instructionsText.text = "That is " + DisplayName(selected) + ". This step needs " +
            DisplayName(expected) + ". " + RecognitionHint(expected);
    }

    public void ReportCorrectCable(NetworkCableKind kind, int completed)
    {
        ArdentAudioManager.Play(ArdentSound.CorrectPlacement);
        stageText.text = ProgressLabel(completed);
        if (completed >= NetworkCableCheckpointRules.TotalSteps)
        {
            instructionsText.text = "All cable types matched correctly: UTP, STP, coaxial/BNC, and fiber/LC. Network Cables AR complete!";
            var progress = FindFirstObjectByType<ARActivityProgress>();
            progress?.TryCompleteNetworkCableActivity(activity);
            return;
        }

        instructionsText.text = SuccessExplanation(kind) + " Next: " + CurrentPrompt(completed);
        ARCheckpointSession.SaveCurrent();
    }

    private void RestartActivity()
    {
        if (workstation == null || placing || ARCheckpointSession.BlocksInput) return;
        changingCheckpoint = true;
        try { workstation.ResetProgress(); }
        finally { changingCheckpoint = false; }
        ARCheckpointSession.ClearSavedCurrent();
        stageText.text = ProgressLabel(0);
        ShowCurrentInstruction();
    }

    private void ShowCurrentInstruction()
    {
        if (workstation == null) return;
        instructionsText.text = CurrentPrompt(workstation.CompletedSteps);
    }

    private void LateUpdate()
    {
        if (!initialized) return;
        if (ordinaryButtons != null)
            foreach (var button in ordinaryButtons) ARButtonAvailability.Set(button, false);
        restartButton.interactable = workstation != null && !placing && !ARCheckpointSession.BlocksInput;
        repositionButton.interactable = workstationObject != null && !placing && !ARCheckpointSession.BlocksInput;
        if (workstation == null || placing) return;

        if (waitForRelease)
        {
            bool held = Mouse.current != null && Mouse.current.leftButton.isPressed;
            if (Touchscreen.current != null)
                foreach (var touch in Touchscreen.current.touches) held |= touch.press.isPressed;
            if (Time.frameCount <= placedFrame || held) return;
            waitForRelease = false;
        }
        workstation.inputEnabled = !UIManager.HasOpenPanel && !SandboxInventoryPanel.IsOpen && !ARCheckpointSession.BlocksInput;
    }

    private static string ProgressLabel(int completed) => "Cable Match: " + completed + " / " + NetworkCableCheckpointRules.TotalSteps;

    private static string CurrentPrompt(int step)
    {
        switch (step)
        {
            case 0: return "Step 1: Find the blue unshielded twisted-pair cable and drag its RJ45 connector into the glowing port.";
            case 1: return "Step 2: Find the silver shielded twisted-pair cable and connect its shielded RJ45 end.";
            case 2: return "Step 3: Find the black coaxial cable and connect its round BNC connector.";
            case 3: return "Step 4: Find the yellow fiber-optic cable and connect its blue LC duplex end.";
            default: return "All network cables are connected.";
        }
    }

    private static string DisplayName(NetworkCableKind kind)
    {
        switch (kind)
        {
            case NetworkCableKind.UTP: return "UTP with RJ45";
            case NetworkCableKind.STP: return "STP with shielded RJ45";
            case NetworkCableKind.Coaxial: return "coaxial cable with BNC";
            default: return "fiber-optic cable with LC duplex";
        }
    }

    private static string RecognitionHint(NetworkCableKind kind)
    {
        switch (kind)
        {
            case NetworkCableKind.UTP: return "Look for the blue jacket and clear rectangular plug.";
            case NetworkCableKind.STP: return "Look for the silver jacket and metal-shielded rectangular plug.";
            case NetworkCableKind.Coaxial: return "Look for the thick black cable and round metal connector.";
            default: return "Look for the thin yellow cable and paired blue connector.";
        }
    }

    private static string SuccessExplanation(NetworkCableKind kind)
    {
        switch (kind)
        {
            case NetworkCableKind.UTP: return "Correct. UTP carries data through four unshielded twisted pairs and commonly uses RJ45.";
            case NetworkCableKind.STP: return "Correct. STP adds metallic shielding to reduce electromagnetic interference.";
            case NetworkCableKind.Coaxial: return "Correct. Coaxial cable surrounds one central conductor with insulation and a conductive shield; this example uses BNC.";
            default: return "Correct. Fiber optic cable carries light through glass or plastic cores; this example uses an LC duplex connector.";
        }
    }

    private void OnDestroy()
    {
        restartButton?.onClick.RemoveListener(RestartActivity);
        repositionButton?.onClick.RemoveListener(Reposition);
    }
}
