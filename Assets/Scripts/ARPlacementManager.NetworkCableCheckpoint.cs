using System;
using UnityEngine;

public partial class ARPlacementManager
{
    public void RestoreNetworkCableCheckpointPlacement(NetworkCableARSession session, Pose pose)
    {
        if (!IsNetworkCableActivity || currentActivity.availableObjects == null || currentActivity.availableObjects.Length != 1)
            throw new InvalidOperationException("Network Cables activity is not configured.");
        CancelPlacement();
        currentObject = session.PlaceWorkstation(currentActivity.availableObjects[0].prefab, pose, contentParent);
        if (currentObject == null) throw new InvalidOperationException("Could not place the Network Cables workstation.");
        lastPlacedFrame = Time.frameCount;
    }
}
