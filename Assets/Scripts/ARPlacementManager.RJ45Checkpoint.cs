using System;
using UnityEngine;

public partial class ARPlacementManager
{
    public void RestoreRJ45CheckpointPlacement(RJ45ARSession session,Pose pose)
    {
        if(!IsRJ45Activity||currentActivity.availableObjects==null||currentActivity.availableObjects.Length!=1)
            throw new InvalidOperationException("RJ45 activity is not configured.");
        CancelPlacement();
        currentObject=session.PlaceWorkstation(currentActivity.availableObjects[0].prefab,pose,contentParent);
        if(currentObject==null)throw new InvalidOperationException("Could not place the RJ45 workstation.");
        lastPlacedFrame=Time.frameCount;
    }
}
