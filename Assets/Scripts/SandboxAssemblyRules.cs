using System.Collections.Generic;
using UnityEngine;

// Authored prefab connections from lessons and the Sandbox connections asset.
public static class SandboxAssemblyRules
{
    [System.Serializable]
    public sealed class Rule
    {
        public GameObject ComponentPrefab, HostPrefab;
        public string TargetID;
        public float SnapDistance, RotationTolerance;
    }

    private static readonly List<Rule> rules = new List<Rule>();
    public static IReadOnlyList<Rule> All => rules;
    public static void Clear() { rules.Clear(); }

    public static void Build(LessonDatabase database, List<ARObjectData> unlocked)
    {
        Clear();
        var prefabs = new HashSet<GameObject>();
        foreach (var item in unlocked ?? new List<ARObjectData>()) if (item?.prefab != null) prefabs.Add(item.prefab);
        foreach (var lesson in database?.lessons ?? new LessonData[0])
        {
            if (lesson == null || !lesson.hasARActivity) continue;
            var activity = lesson.arActivity;
            if (activity?.assemblyActivity?.steps == null || activity.availableObjects == null) continue;
            foreach (var step in activity.assemblyActivity.steps)
            {
                // Support the CPU and RAM connections configured by the lessons.
                if (step == null || (step.targetID != "cpu_target" &&
                                     step.targetID != "ram_target" &&
                                     step.targetID != "mb_target" &&
                                     step.targetID != "cpuCooler_target") ||
                                     step.component?.prefab == null || !prefabs.Contains(step.component.prefab)) continue;
                foreach (var host in activity.availableObjects)
                {
                    if (host?.prefab == null || !prefabs.Contains(host.prefab)) continue;
                    var anchor = host.prefab.GetComponent<AssemblyAnchor>();
                    if (anchor == null || anchor.anchorID != step.anchorID || anchor.FindTarget(step.targetID) == null) continue;
                    if (rules.Exists(r => r.ComponentPrefab == step.component.prefab &&
                        r.HostPrefab == host.prefab && r.TargetID == step.targetID)) continue;
                    rules.Add(new Rule {
                        ComponentPrefab = step.component.prefab, HostPrefab = host.prefab,
                        TargetID = step.targetID, SnapDistance = Mathf.Max(.001f, step.snapDistance),
                        RotationTolerance = Mathf.Clamp(step.rotationTolerance, 0f, 180f)
                    });
                }
            }
        }
        var connections = Resources.Load<SandboxAssemblyConnections>(SandboxAssemblyConnections.ResourcePath);
        if (connections?.connections == null) return;
        foreach (var entry in connections.connections)
        {
            if (entry == null || entry.ComponentPrefab == null || entry.HostPrefab == null ||
                entry.ComponentPrefab == entry.HostPrefab || string.IsNullOrWhiteSpace(entry.TargetID) ||
                !prefabs.Contains(entry.ComponentPrefab) || !prefabs.Contains(entry.HostPrefab)) continue;
            var anchor = entry.HostPrefab.GetComponent<AssemblyAnchor>();
            if (anchor == null || anchor.FindTarget(entry.TargetID) == null) continue;
            if (rules.Exists(r => r.ComponentPrefab == entry.ComponentPrefab &&
                r.HostPrefab == entry.HostPrefab && r.TargetID == entry.TargetID)) continue;
            rules.Add(new Rule {
                ComponentPrefab = entry.ComponentPrefab, HostPrefab = entry.HostPrefab, TargetID = entry.TargetID,
                SnapDistance = float.IsNaN(entry.SnapDistance) || float.IsInfinity(entry.SnapDistance) ? .15f : Mathf.Max(.001f, entry.SnapDistance),
                RotationTolerance = float.IsNaN(entry.RotationTolerance) || float.IsInfinity(entry.RotationTolerance) ? 15f : Mathf.Clamp(entry.RotationTolerance, 0f, 180f)
            });
        }
    }
}
