using UnityEngine;

public partial class ARAssemblyManager
{
    public bool IsSandboxComponent(GameObject candidate)
    {
        if (!ARSandboxSession.IsActive || candidate == null) return false;
        var part = candidate.GetComponent<SandboxAssemblyPart>();
        if (part == null) return false;
        foreach (var rule in SandboxAssemblyRules.All)
            if (rule.ComponentPrefab == part.SourcePrefab) return true;
        return false;
    }

    public bool CanAlignSandboxComponent(GameObject candidate)
    {
        return IsSandboxComponent(candidate) && !candidate.GetComponent<SandboxAssemblyPart>().IsAttached;
    }

    public void ShowSandboxSelection(GameObject candidate)
    {
        if (!IsSandboxComponent(candidate)) return;
        var part = candidate.GetComponent<SandboxAssemblyPart>();
        string name = SandboxPartName(candidate);
        string notice = part.IsAttached ? SandboxCompatibilityNotice(SandboxCompatibility(part, part.Host, part.Target.targetID), part.Target.targetID) : "";
        SandboxMessage((part.IsAttached ? $"{name} installed. Drag it away to detach it."
            : $"Move {name} near its matching mounting target and release. Use Adjust > Align Rotation if needed.") + notice);
    }

    public bool DetachSandboxComponent(GameObject candidate)
    {
        if (!ARSandboxSession.IsActive || candidate == null) return false;
        var part = candidate.GetComponent<SandboxAssemblyPart>();
        if (part == null || !part.Detach()) return false;
        SandboxMessage($"{SandboxPartName(candidate)} detached. Release it away from the target; drag it back to reinstall.");
        return true;
    }

    private static string SandboxPartName(GameObject candidate)
    {
        return HardwareProfileCatalog.InstanceDisplayName(candidate);
    }

    private static string SandboxTargetName(string targetID)
    {
        switch (targetID)
        {
            case "cpu_target": return "CPU socket";
            case "ram_target": return "RAM slot";
            case "gpu_target": return "graphics card slot";
            case "rightPanel_target": return "right panel mount";
            case "glassPanel_target": return "glass panel mount";
            case "hdd_target": return "HDD mount";
            case "psu_target": return "PSU mount";
            case "mb_target": return "motherboard mount";
            case "cpuCooler_target": return "CPU cooler mount";
            default: return "mounting target";
        }
    }

    // Snap parenting is geometric; cooler compatibility uses the installed motherboard.
    private static HardwareCompatibility.Result SandboxCompatibility(SandboxAssemblyPart part,
        SandboxAssemblyPart host, string targetID)
    {
        var profile = HardwareProfileCatalog.ForPrefab(part.SourcePrefab);
        var hostProfile = HardwareProfileCatalog.ForPrefab(host != null ? host.SourcePrefab : null);
        var board = profile != null && profile.kind == HardwareComponentProfile.ComponentKind.Motherboard
            ? profile : FindSandboxMotherboard(host);
        if (targetID == "cpuCooler_target")
        {
            if (board == null)
                return new HardwareCompatibility.Result(HardwareCompatibility.Status.NeedsVerification,
                    "Install the CPU on a motherboard to check cooler mounting support. Case clearance is unverified.");
            return HardwareCompatibility.Compare(profile, board);
        }
        var result = HardwareCompatibility.Compare(profile, hostProfile);
        if (result.status == HardwareCompatibility.Status.Incompatible) return result;
        // Recheck a cooler carried on a CPU when that CPU is installed on another board.
        foreach (var child in part.GetComponentsInChildren<SandboxAssemblyPart>(true))
        {
            if (!child.IsAttached || child.Target.targetID != "cpuCooler_target") continue;
            var cooler = HardwareProfileCatalog.ForPrefab(child.SourcePrefab);
            var check = HardwareCompatibility.Compare(cooler, board);
            if (check.status == HardwareCompatibility.Status.Incompatible) return check;
            if (result.status == HardwareCompatibility.Status.Compatible &&
                check.status == HardwareCompatibility.Status.NeedsVerification) result = check;
        }
        return result;
    }

    private static HardwareComponentProfile FindSandboxMotherboard(SandboxAssemblyPart host)
    {
        while (host != null)
        {
            var profile = HardwareProfileCatalog.ForPrefab(host.SourcePrefab);
            if (profile != null && profile.kind == HardwareComponentProfile.ComponentKind.Motherboard) return profile;
            host = host.IsAttached ? host.Host : null;
        }
        return null;
    }

    private static string SandboxCompatibilityNotice(HardwareCompatibility.Result result, string targetID)
    {
        // These targets confirm physical mounting only, not electrical compatibility.
        // Preserve any explicit rejection if checks are added for these parts later.
        if (result.status != HardwareCompatibility.Status.Incompatible)
        {
            switch (targetID)
            {
                case "rightPanel_target":
                case "glassPanel_target":
                case "hdd_target":
                    return "";
                case "psu_target":
                    return "\nPSU mounting only; wattage and connector compatibility are not checked.";
            }
        }
        string title = result.status == HardwareCompatibility.Status.NeedsVerification ? "Needs verification" : result.status.ToString();
        return "\n" + title + ": " + result.explanation;
    }

    private void SandboxMessage(string message)
    {
        if (instructionsText != null)
        {
            instructionsText.gameObject.SetActive(true);
            instructionsText.text = message;
        }
        Debug.Log("AR Sandbox: " + message);
    }

    private bool FindSandboxTarget(SandboxAssemblyPart part, out SandboxAssemblyPart host,
        out AssemblyTarget target, out SandboxAssemblyRules.Rule match)
    {
        host = null; target = null; match = null;
        float nearest = float.PositiveInfinity;
        var placed = FindObjectsByType<SandboxAssemblyPart>(FindObjectsSortMode.None);
        foreach (var rule in SandboxAssemblyRules.All)
        {
            if (rule.ComponentPrefab != part.SourcePrefab) continue;
            foreach (var possible in placed)
            {
                if (possible == part || possible.gameObject.scene != part.gameObject.scene ||
                    possible.SourcePrefab != rule.HostPrefab) continue;
                var anchor = possible.GetComponent<AssemblyAnchor>();
                var socket = anchor != null ? anchor.FindTarget(rule.TargetID) : null;
                if (socket == null) continue;
                float distance = Vector3.Distance(part.transform.position, socket.transform.position);
                if (distance >= nearest) continue;
                nearest = distance; host = possible; target = socket; match = rule;
            }
        }
        return target != null;
    }

    private bool TargetOccupied(SandboxAssemblyPart host, AssemblyTarget target)
    {
        foreach (var part in host.GetComponentsInChildren<SandboxAssemblyPart>(true))
            if (part.IsAttached && part.Target == target) return true;
        return false;
    }

    private bool TryAlignSandboxComponent(GameObject candidate)
    {
        if (!CanAlignSandboxComponent(candidate)) return false;
        var part = candidate.GetComponent<SandboxAssemblyPart>();
        if (!FindSandboxTarget(part, out var host, out var target, out var rule))
        { SandboxMessage($"Place the matching host object for {SandboxPartName(candidate)} from the inventory first."); return false; }
        if (TargetOccupied(host, target))
        { SandboxMessage($"This {SandboxTargetName(rule.TargetID)} is occupied. Detach or delete its installed component first."); return false; }
        var compatibility = SandboxCompatibility(part, host, rule.TargetID);
        if (compatibility.status == HardwareCompatibility.Status.Incompatible)
        { SandboxMessage("Cannot install: " + compatibility.explanation); return false; }
        candidate.transform.rotation = target.transform.rotation;
        SandboxMessage($"Rotation aligned. Move {SandboxPartName(candidate)} to the {SandboxTargetName(rule.TargetID)} and release." + SandboxCompatibilityNotice(compatibility, rule.TargetID));
        return true;
    }

    private bool TrySnapSandboxComponent(GameObject candidate)
    {
        if (!IsSandboxComponent(candidate)) return false;
        var part = candidate.GetComponent<SandboxAssemblyPart>();
        if (part.IsAttached || part.ConsumeDetachRelease()) return false;
        if (!FindSandboxTarget(part, out var host, out var target, out var rule))
        { SandboxMessage($"Place the matching host object for {SandboxPartName(candidate)} from the inventory first."); return false; }
        float allowed = rule.SnapDistance * host.SizeMultiplier;
        float distance = Vector3.Distance(candidate.transform.position, target.transform.position);
        if (distance > allowed)
        { SandboxMessage($"Move {SandboxPartName(candidate)} closer to the {SandboxTargetName(rule.TargetID)}."); return false; }
        if (TargetOccupied(host, target))
        { SandboxMessage($"This {SandboxTargetName(rule.TargetID)} is occupied. Detach or delete its installed component first."); return false; }
        var compatibility = SandboxCompatibility(part, host, rule.TargetID);
        if (compatibility.status == HardwareCompatibility.Status.Incompatible)
        { SandboxMessage("Cannot install: " + compatibility.explanation); return false; }
        if (Quaternion.Angle(candidate.transform.rotation, target.transform.rotation) > rule.RotationTolerance)
        { SandboxMessage($"Rotate {SandboxPartName(candidate)} to match the {SandboxTargetName(rule.TargetID)}, or use Adjust > Align Rotation."); return false; }
        part.Attach(host, target);
        SandboxMessage($"{SandboxPartName(candidate)} installed. Move or resize the host object to move its installed components together. Drag this component away to detach." + SandboxCompatibilityNotice(compatibility, rule.TargetID));
        return true;
    }

    private bool CanDeleteSandboxObject(GameObject candidate)
    {
        if (candidate == null) return true;
        foreach (var part in candidate.GetComponentsInChildren<SandboxAssemblyPart>(true))
            if (part.IsAttached && part.Host.gameObject == candidate)
            { SandboxMessage($"Detach or delete the installed components before removing {SandboxPartName(candidate)}."); return false; }
        return true;
    }
}
