using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class NetworkConnectionManager
{
    private bool hasWorkspaceOrigin;
    private Pose workspaceOrigin;
    private float nextCompletionRetry;

    public void RegisterPlacedNode(GameObject instance, GameObject prefab, Pose surface)
    {
        EnsureProgressActivity();
        var node = instance.GetComponent<NetworkNode>();
        if (node == null) return;
        node.InitializeRuntime(prefab);
        if (!hasWorkspaceOrigin) { workspaceOrigin = surface; hasWorkspaceOrigin = true; }
        Label(node);
        ARCheckpointSession.SaveCurrent();
    }

    private List<NetworkNode> WorkspaceNodes()
    {
        var result = new List<NetworkNode>();
        foreach (var node in FindObjectsByType<NetworkNode>(FindObjectsSortMode.None))
            if (node.gameObject.scene == gameObject.scene && node.isActiveAndEnabled) result.Add(node);
        result.Sort((a, b) => string.CompareOrdinal(a.RuntimeID, b.RuntimeID));
        return result;
    }

    public NetworkCheckpointData CaptureNetworkCheckpoint()
    {
        EnsureProgressActivity();
        if (progressActivity == null || progressActivity.activityType != ARActivityType.NetworkDesign)
            throw new InvalidOperationException("No active network activity.");
        var saved = new NetworkCheckpointData { topology = topologyIndex, passedMask = topologyProgress.PassedMask };
        var nodes = new List<NetworkSavedNode>();
        var live = new HashSet<NetworkNode>();
        Quaternion inverse = Quaternion.Inverse(hasWorkspaceOrigin ? workspaceOrigin.rotation : Quaternion.identity);
        Vector3 origin = hasWorkspaceOrigin ? workspaceOrigin.position : Vector3.zero;
        foreach (var node in WorkspaceNodes())
        {
            int index = Array.FindIndex(progressActivity.availableObjects,
                item => item != null && item.prefab != null && item.prefab == node.SourcePrefab);
            var manipulator = node.GetComponent<ARObjectManipulator>();
            if (index < 0 || string.IsNullOrEmpty(node.RuntimeID) || manipulator == null)
                throw new InvalidOperationException("A placed network device has no saved prefab identity.");
            nodes.Add(new NetworkSavedNode {
                id = node.RuntimeID, prefabIndex = index, label = Label(node),
                position = inverse * (node.transform.position - origin),
                rotation = inverse * node.transform.rotation, multiplier = manipulator.ScaleMultiplier
            });
            live.Add(node);
        }
        var edges = new List<NetworkSavedEdge>();
        foreach (var cable in connections)
            if (cable != null && cable.StartNode != null && cable.EndNode != null &&
                live.Contains(cable.StartNode) && live.Contains(cable.EndNode))
                edges.Add(new NetworkSavedEdge { a = cable.StartNode.RuntimeID, b = cable.EndNode.RuntimeID });
        saved.nodes = nodes.ToArray();
        saved.edges = edges.ToArray();
        saved.nextLabel = nextLabel;
        return saved;
    }

    public void RestoreNetworkCheckpoint(NetworkCheckpointData saved, Pose surface)
    {
        EnsureProgressActivity();
        if (!Available || !NetworkCheckpointRules.IsValid(saved, progressActivity.availableObjects.Length))
            throw new InvalidOperationException("Network checkpoint or scene references are invalid.");
        foreach (var n in saved.nodes)
        {
            var prefab = progressActivity.availableObjects[n.prefabIndex]?.prefab;
            if (prefab == null || prefab.GetComponent<NetworkNode>() == null || prefab.GetComponent<ARObjectManipulator>() == null)
                throw new InvalidOperationException("A saved network prefab is unavailable.");
        }
        StartNetworkOver();
        workspaceOrigin = surface;
        hasWorkspaceOrigin = true;
        var restored = new Dictionary<string, NetworkNode>();
        try
        {
            foreach (var n in saved.nodes)
            {
                var prefab = progressActivity.availableObjects[n.prefabIndex].prefab;
                var instance = placementManager.SpawnNetworkCheckpointObject(prefab,
                    new Pose(surface.position + surface.rotation * n.position, surface.rotation * n.rotation));
                var node = instance.GetComponent<NetworkNode>();
                node.InitializeRuntime(prefab, n.id);
                restored.Add(n.id, node);
                instance.GetComponent<ARObjectManipulator>().SetScaleMultiplier(n.multiplier);
                labels[node] = n.label;
            }
            foreach (var edge in saved.edges)
            {
                var cable = Instantiate(connectionPrefab, transform);
                cable.gameObject.SetActive(true);
                connections.Add(cable);
                cable.Initialize(restored[edge.a], restored[edge.b]);
            }
            topologyProgress.Restore(saved.passedMask);
            topologyIndex = saved.topology;
            nextLabel = saved.nextLabel;
            if (topologyDropdown != null) topologyDropdown.SetValueWithoutNotify(topologyIndex);
            RefreshBackboneAttachments();
            Message("Saved network restored. " + Objective());
        }
        catch
        {
            StartNetworkOver();
            throw;
        }
    }

    public void StartNetworkOver()
    {
        placementManager?.CancelPlacement();
        interactionManager?.DeselectObject();
        ClearConnections();
        foreach (var node in WorkspaceNodes())
        {
            node.gameObject.SetActive(false);
            Destroy(node.gameObject);
        }
        topologyProgress.Reset();
        completionRecorded = false;
        hasWorkspaceOrigin = false;
        topologyIndex = 0;
        if (topologyDropdown != null) topologyDropdown.SetValueWithoutNotify(0);
        if (modeManager != null) modeManager.SetEditMode();
        Message(Objective());
    }

    private void TryRecordRestoredCompletion()
    {
        if (Time.unscaledTime < nextCompletionRetry) return;
        nextCompletionRetry = Time.unscaledTime + 2f;
        var progress = FindFirstObjectByType<ARActivityProgress>();
        completionRecorded = progress != null && progress.TryCompleteNetworkActivity(progressActivity);
        if (completionRecorded) Message("All three topologies passed. Network Design AR completed! Use the completion button to continue.");
    }
}
