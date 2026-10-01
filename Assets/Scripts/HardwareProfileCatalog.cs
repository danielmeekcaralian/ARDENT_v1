using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HardwareProfileCatalog", menuName = "ARDENT/Hardware Profile Catalog")]
public class HardwareProfileCatalog : ScriptableObject
{
    public List<HardwareComponentProfile> profiles = new List<HardwareComponentProfile>();
    public const string ResourcePath = "HardwareProfiles/HardwareProfileCatalog";

    public HardwareComponentProfile Find(GameObject prefab)
    {
        if (prefab == null || profiles == null) return null;
        HardwareComponentProfile result = null;
        foreach (var profile in profiles)
        {
            if (profile == null || profile.prefab != prefab) continue;
            if (result != null && result != profile)
            {
                Debug.LogWarning("Multiple hardware profiles reference " + prefab.name + ". Assign a distinct prefab to each variant.", this);
                return null;
            }
            result = profile;
        }
        return result;
    }

    // Display labels are separate from prefab names and ARObjectInfo lesson identities.
    public static string DisplayName(GameObject prefab)
    {
        if (prefab == null) return "Component";
        var profile = ForPrefab(prefab);
        if (profile != null && !string.IsNullOrWhiteSpace(profile.productName)) return profile.productName.Trim();
        var info = prefab.GetComponent<ARObjectInfo>();
        if (info != null && !string.IsNullOrWhiteSpace(info.objectName)) return info.objectName.Trim();
        return FriendlyFileName(prefab.name);
    }

    public static string InstanceDisplayName(GameObject instance)
    {
        if (instance == null) return "Component";
        var part = instance.GetComponent<SandboxAssemblyPart>();
        return DisplayName(part != null && part.SourcePrefab != null ? part.SourcePrefab : instance);
    }

    public static string FriendlyFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Component";
        name = name.Replace("(Clone)", "").Trim();
        foreach (var prefix in new[] { "cpu_", "mb_", "ram_", "gpu_" })
            if (name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
            { name = name.Substring(prefix.Length); break; }
        name = name.Replace('_', ' ').Trim();
        return string.IsNullOrWhiteSpace(name) ? "Component" : name;
    }

    public static HardwareComponentProfile ForPrefab(GameObject prefab)
    {
        var catalog = Resources.Load<HardwareProfileCatalog>(ResourcePath);
        return catalog != null ? catalog.Find(prefab) : null;
    }
}
