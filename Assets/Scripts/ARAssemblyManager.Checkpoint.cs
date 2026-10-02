using System;
using UnityEngine;

public partial class ARAssemblyManager
{
    public void CaptureCheckpoint(ARCheckpointData data)
    {
        data.assembled = currentStepIndex;
        data.phase = (int)Phase;
        data.removed = Phase == ActivityPhase.Disassembly ? installedParts.Count - 1 - removalIndex : 0;
    }

    public void RestoreCheckpoint(ARCheckpointData data, ARActivityData activity, ARPlacementManager placement, Pose pose)
    {
        SetActivity(activity.assemblyActivity);
        placement.BeginCheckpointWorkspace(pose);
        for (int i = 0; i < data.assembled; i++)
        {
            var step = assemblyActivity.steps[i];
            var anchor = FindAnchor(step.anchorID);
            if (anchor == null)
            {
                GameObject prefab = null;
                foreach (var item in activity.availableObjects)
                {
                    var possible = item?.prefab != null ? item.prefab.GetComponent<AssemblyAnchor>() : null;
                    if (possible != null && possible.anchorID == step.anchorID) { prefab = item.prefab; break; }
                }
                if (prefab == null) throw new InvalidOperationException("Missing base: " + step.anchorID);
                anchor = placement.SpawnCheckpointObject(prefab, Vector3.zero).GetComponent<AssemblyAnchor>();
                RegisterAssemblyAnchor(anchor);
            }
            var target = anchor.FindTarget(step.targetID);
            if (target == null || step.component?.prefab == null)
                throw new InvalidOperationException("Missing component or mounting target for step " + (i + 1));
            // A board may already exist as the base of earlier steps before it is installed in the case.
            var prefabAnchor = step.component.prefab.GetComponent<AssemblyAnchor>();
            var existing = prefabAnchor != null ? FindAnchor(prefabAnchor.anchorID) : null;
            GameObject part = existing != null && !installedParts.Contains(existing.gameObject)
                ? existing.gameObject : placement.SpawnCheckpointObject(step.component.prefab, Vector3.zero);
            if (part == anchor.gameObject || anchor.transform.IsChildOf(part.transform))
                throw new InvalidOperationException("Invalid assembly hierarchy.");
            part.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
            part.transform.SetParent(anchor.transform, true);
            part.GetComponent<ARObjectManipulator>().SetLocked(true);
            var partAnchor = part.GetComponent<AssemblyAnchor>();
            if (partAnchor != null) RegisterAssemblyAnchor(partAnchor);
            installedParts.Add(part);
            currentStepIndex++;
        }
        if (currentStepIndex < assemblyActivity.steps.Length) ShowCurrentStep();
        else FinishAssemblyPhase();
        if (data.phase == (int)ActivityPhase.Disassembly)
        {
            BeginDisassembly();
            if (Phase != ActivityPhase.Disassembly) throw new InvalidOperationException("Could not restore disassembly.");
            for (int i = 0; i < data.removed; i++)
            {
                var part = installedParts[removalIndex];
                var bounds = GetLocalVisualBounds(part.transform);
                var slot = traySlots[installedParts.Count - 1 - removalIndex];
                part.transform.position += sharedRoot.TransformVector(slot - bounds.center);
                if (!TryRemoveCurrentPart(part)) throw new InvalidOperationException("Could not restore tray progress.");
            }
        }
    }
}
