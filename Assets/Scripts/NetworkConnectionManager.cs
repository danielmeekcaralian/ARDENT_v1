using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Owns connections for one AR activity. No topology or completion rules here.
public sealed partial class NetworkConnectionManager : MonoBehaviour
{
    [SerializeField] private ARPlacementManager placementManager;
    [SerializeField] private ARModeManager modeManager;
    [SerializeField] private ARInteractionManager interactionManager;
    [SerializeField] private Button connectButton;
    [SerializeField] private Button checkTopologyButton;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private NetworkConnection connectionPrefab;
    private readonly List<NetworkConnection> connections = new List<NetworkConnection>();
    private readonly Dictionary<NetworkNode, string> labels = new Dictionary<NetworkNode, string>();
    [SerializeField] private TMP_Dropdown topologyDropdown;
    private int topologyIndex; // 0 = Star, 1 = Ring, 2 = Bus.
    private NetworkNode firstNode;
    private int nextLabel = 1;
    private bool wasAvailable;
    private bool showingTopologySuccess;
    private readonly NetworkTopologyProgress topologyProgress = new NetworkTopologyProgress();
    private ARActivityData progressActivity;
    private bool completionRecorded;

    public bool Available => isActiveAndEnabled && placementManager != null &&
        placementManager.IsNetworkActivity && modeManager != null &&
        interactionManager != null && connectionPrefab != null && feedbackText != null;
    public IReadOnlyList<NetworkConnection> Connections => connections;

    private void OnEnable()
    {
        if (connectButton != null) connectButton.onClick.AddListener(BeginConnect);
        // Reuse the user's scene-authored button, including inactive UI children.
        if (checkTopologyButton == null)
            foreach (var root in gameObject.scene.GetRootGameObjects())
                foreach (var button in root.GetComponentsInChildren<Button>(true))
                    if (button.name == "CheckTopologyButton") checkTopologyButton = button;
        if (checkTopologyButton != null) checkTopologyButton.onClick.AddListener(CheckTopology);
        if (topologyDropdown == null)
            foreach (var root in gameObject.scene.GetRootGameObjects())
                foreach (var dropdown in root.GetComponentsInChildren<TMP_Dropdown>(true))
                    if (dropdown.name == "TopologyDropdown") topologyDropdown = dropdown;
        if (topologyDropdown != null)
        {
            // Make option ordering explicit so labels cannot select the wrong validator.
            topologyDropdown.ClearOptions();
            topologyDropdown.AddOptions(new List<string> { "Star", "Ring", "Bus" });
            topologyDropdown.SetValueWithoutNotify(topologyIndex);
            topologyDropdown.onValueChanged.AddListener(ChangeTopology);
        }
        UpdateTopologyControls(placementManager != null && placementManager.IsNetworkActivity, false);
    }

    private void Update()
    {
        bool available = Available;
        EnsureProgressActivity();
        bool canInteract = available && !ARCheckpointSession.BlocksInput && !UIManager.HasOpenPanel && !SandboxInventoryPanel.IsOpen;
        ARButtonAvailability.Set(connectButton, canInteract);
        UpdateTopologyControls(placementManager != null && placementManager.IsNetworkActivity, canInteract);

        if (!wasAvailable && available) Message(Objective());
        if (available && !ARCheckpointSession.BlocksInput && topologyProgress.IsComplete && !completionRecorded)
            TryRecordRestoredCompletion();
        connections.RemoveAll(c => c == null);
        RefreshBackboneAttachments();
        // Moving devices never changes the result; graph edits invalidate old success.
        if (available && showingTopologySuccess && !EvaluateTopology().passed)
            Message("Network changed. Press Check Topology to check the new connections.");
        if (!available || modeManager.CurrentMode != ARInteractionMode.Connect ||
            ARCheckpointSession.BlocksInput || UIManager.HasOpenPanel || SandboxInventoryPanel.IsOpen)
            firstNode = null;
        if (wasAvailable && !available)
        {
            ClearConnections();
            if (modeManager != null && modeManager.CurrentMode == ARInteractionMode.Connect)
                modeManager.SetEditMode();
        }
        wasAvailable = available;
    }

    private void UpdateTopologyControls(bool visible, bool canInteract)
    {
        if (checkTopologyButton != null)
        {
            if (visible) ARButtonAvailability.Set(checkTopologyButton, canInteract);
            else checkTopologyButton.gameObject.SetActive(false);
        }
        if (topologyDropdown != null)
        {
            if ((!visible || !canInteract) && topologyDropdown.IsExpanded) topologyDropdown.Hide();
            topologyDropdown.interactable = visible && canInteract;
            if (topologyDropdown.gameObject.activeSelf != visible) topologyDropdown.gameObject.SetActive(visible);
        }
    }
    private string Objective() => topologyIndex == 2
        ? "BUS: Use 4 PCs and 1 backbone, with no switch. Connect each PC to the shared backbone."
        : topologyIndex == 1
        ? "RING: Use 4 PCs, with no switch. Connect each PC to two other PCs to form one closed loop."
        : "STAR: Use 4 PCs and 1 switch. Connect every PC only to the switch.";

    private void ChangeTopology(int value)
    {
        if (!Available || value < 0 || value > 2 || ARCheckpointSession.BlocksInput ||
            UIManager.HasOpenPanel || SandboxInventoryPanel.IsOpen)
        {
            if (topologyDropdown != null) topologyDropdown.SetValueWithoutNotify(topologyIndex);
            return;
        }
        if (value == topologyIndex) return;
        placementManager.CancelPlacement();
        interactionManager.DeselectObject();
        modeManager.SetEditMode();
        ClearConnections(false);
        topologyIndex = value;
        Message(Objective() + "\nCables cleared; placed devices kept. Add or delete devices as needed.");
    }

    public void CheckTopology()
    {
        if (!Available || ARCheckpointSession.BlocksInput || UIManager.HasOpenPanel || SandboxInventoryPanel.IsOpen) return;
        placementManager.CancelPlacement();
        interactionManager.DeselectObject();
        firstNode = null;
        modeManager.SetEditMode();
        EnsureProgressActivity();
        var result = EvaluateTopology();
        topologyProgress.Record(topologyIndex, result.passed);
        string completionNotice = "";
        if (topologyProgress.IsComplete && !completionRecorded)
        {
            var progress = FindFirstObjectByType<ARActivityProgress>();
            completionRecorded = progress != null && progress.TryCompleteNetworkActivity(progressActivity);
            completionNotice = completionRecorded
                ? "\nAll three topologies passed. Network Design AR completed! Use the completion button to continue."
                : "\nAll three passed, but lesson completion could not be saved. Open this activity through its lesson and check the progress manager setup.";
        }
        Message(result.message + completionNotice);
        showingTopologySuccess = result.passed;
        ARCheckpointSession.SaveCurrent();
    }

    private void EnsureProgressActivity()
    {
        var activity = placementManager != null ? placementManager.CurrentActivity : null;
        if (progressActivity == activity) return;
        progressActivity = activity;
        topologyProgress.Reset();
        completionRecorded = false;
        hasWorkspaceOrigin = false;
    }

    private NetworkValidator.Result EvaluateTopology()
    {
        var nodes = new List<NetworkValidator.Node>();
        foreach (var node in FindObjectsByType<NetworkNode>(FindObjectsSortMode.InstanceID))
            if (node.gameObject.scene == gameObject.scene && node.isActiveAndEnabled)
                nodes.Add(new NetworkValidator.Node(node.GetInstanceID(), node.NodeType, Label(node)));
        var edges = new List<NetworkValidator.Edge>();
        foreach (var cable in connections)
        {
            // A destroyed device removes its cable in LateUpdate. Ignore it immediately.
            if (cable == null || cable.StartNode == null || cable.EndNode == null) continue;
            edges.Add(new NetworkValidator.Edge(cable.StartNode.GetInstanceID(), cable.EndNode.GetInstanceID()));
        }
        if (topologyIndex == 2) return NetworkValidator.CheckBus(nodes, edges);
        return topologyIndex == 1 ? NetworkValidator.CheckRing(nodes, edges) : NetworkValidator.CheckStar(nodes, edges);
    }

    public void BeginConnect()
    {
        if (!Available || ARCheckpointSession.BlocksInput || UIManager.HasOpenPanel || SandboxInventoryPanel.IsOpen) return;
        placementManager.CancelPlacement();
        interactionManager.DeselectObject();
        firstNode = null;
        modeManager.SetConnectMode();
        Message("Tap the first device, then a second device to connect them. Tap an existing pair again to disconnect.");
    }

    public void SelectNode(NetworkNode node)
    {
        if (!Available || modeManager.CurrentMode != ARInteractionMode.Connect) return;
        if (node == null || node.gameObject.scene != gameObject.scene)
        {
            firstNode = null;
            Message("Selection cleared. Tap a network device.");
            return;
        }
        if (firstNode == null)
        {
            firstNode = node;
            Message(Label(node) + " selected. Tap another device. Tap the same device to cancel.");
            return;
        }
        var start = firstNode;
        firstNode = null;
        if (start == node) { Message("Connection cancelled. Tap a device to start again."); return; }
        for (int i = connections.Count - 1; i >= 0; i--)
        {
            var existing = connections[i];
            if (existing == null || !existing.Joins(start, node)) continue;
            connections.RemoveAt(i);
            Destroy(existing.gameObject);
            Message(Label(start) + " disconnected from " + Label(node) + ".");
            return;
        }
        var cable = Instantiate(connectionPrefab, transform);
        cable.gameObject.SetActive(true);
        cable.Initialize(start, node);
        connections.Add(cable);
        Message(Label(start) + " connected to " + Label(node) + ". Select another pair, or use Select to move devices.");
    }

    private string Label(NetworkNode node)
    {
        if (!labels.TryGetValue(node, out string label))
        {
            label = node.DeviceName + " " + nextLabel++;
            labels.Add(node, label);
        }
        return label;
    }

    private void RefreshBackboneAttachments()
    {
        var groups = new Dictionary<NetworkNode, List<NetworkConnection>>();
        foreach (var cable in connections)
        {
            if (cable == null || cable.StartNode == null || cable.EndNode == null) continue;
            AddBackboneCable(groups, cable.StartNode, cable);
            AddBackboneCable(groups, cable.EndNode, cable);
        }
        foreach (var group in groups)
            for (int i = 0; i < group.Value.Count; i++)
                group.Value[i].SetBackboneFraction(group.Key, (i + 1f) / (group.Value.Count + 1f));
    }

    private static void AddBackboneCable(Dictionary<NetworkNode, List<NetworkConnection>> groups,
        NetworkNode node, NetworkConnection cable)
    {
        if (node.NodeType != NetworkNodeType.Backbone) return;
        if (!groups.TryGetValue(node, out var list))
            groups.Add(node, list = new List<NetworkConnection>());
        list.Add(cable);
    }

    private void Message(string text)
    {
        showingTopologySuccess = false;
        if (feedbackText != null) feedbackText.text = text + "\n" + topologyProgress.Summary;
        if (Available) ARCheckpointSession.SaveCurrent();
    }

    private void ClearConnections(bool resetLabels = true)
    {
        firstNode = null;
        showingTopologySuccess = false;
        foreach (var cable in connections) if (cable != null) Destroy(cable.gameObject);
        connections.Clear();
        if (resetLabels) { labels.Clear(); nextLabel = 1; }
    }

    private void OnDisable()
    {
        if (connectButton != null) connectButton.onClick.RemoveListener(BeginConnect);
        if (checkTopologyButton != null) checkTopologyButton.onClick.RemoveListener(CheckTopology);
        if (topologyDropdown != null)
        {
            topologyDropdown.onValueChanged.RemoveListener(ChangeTopology);
            topologyDropdown.Hide();
            topologyDropdown.interactable = false;
        }
        ARButtonAvailability.Set(connectButton, false);
        UpdateTopologyControls(false, false);
        if (modeManager != null && modeManager.CurrentMode == ARInteractionMode.Connect)
            modeManager.SetEditMode();
        ClearConnections();
    }
}

