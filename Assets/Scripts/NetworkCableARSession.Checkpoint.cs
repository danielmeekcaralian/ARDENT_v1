using System;
using UnityEngine;

public sealed partial class NetworkCableARSession
{
    private bool changingCheckpoint;

    public NetworkCableCheckpointData CaptureCheckpoint()
    {
        if (!initialized || changingCheckpoint || workstation == null) return null;
        var data = new NetworkCableCheckpointData { completedSteps = workstation.CompletedSteps };
        return NetworkCableCheckpointRules.IsValid(data) ? data : null;
    }

    public void ClearCheckpointWorkspace(bool beginPlacement)
    {
        changingCheckpoint = true;
        try
        {
            placement.CancelPlacement();
            if (workstationObject != null) { workstationObject.SetActive(false); Destroy(workstationObject); }
            workstationObject = null;
            workstation = null;
            placing = true;
            waitForRelease = true;
        }
        finally { changingCheckpoint = false; }
        if (beginPlacement) Reposition();
    }

    public void RestoreCheckpoint(NetworkCableCheckpointData data, Pose pose)
    {
        if (!initialized || !NetworkCableCheckpointRules.IsValid(data))
            throw new InvalidOperationException("Invalid Network Cables checkpoint or missing session UI.");
        if (Vector3.Dot(pose.rotation * Vector3.up, Vector3.up) < .95f)
            throw new InvalidOperationException("Choose a horizontal surface.");

        ClearCheckpointWorkspace(false);
        changingCheckpoint = true;
        try
        {
            placement.RestoreNetworkCableCheckpointPlacement(this, pose);
            if (workstation == null) throw new InvalidOperationException("The Network Cables workstation is missing.");
            workstation.RestoreProgress(data.completedSteps);
            stageText.text = ProgressLabel(data.completedSteps);
            instructionsText.text = "Saved cable progress restored. " + CurrentPrompt(data.completedSteps);
            waitForRelease = true;
            placedFrame = Time.frameCount;
        }
        catch { ClearCheckpointWorkspace(false); throw; }
        finally { changingCheckpoint = false; }
    }
}
