using System;
using System.Collections.Generic;
using UnityEngine;
using P = HardwareComponentProfile;

[CreateAssetMenu(fileName = "HardwareCompatibilityCatalog", menuName = "ARDENT/Compatibility Catalog")]
public sealed class HardwareCompatibilityCatalog : ScriptableObject
{
    public const string ResourcePath = "Compatibility/HardwareCompatibilityCatalog";
    public List<P> profiles = new List<P>();

    public static List<P> Collect(HardwareProfileCatalog existing, HardwareCompatibilityCatalog extra)
    {
        var result = new List<P>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Existing profiles win duplicate IDs to preserve their verified support references.
        Add(existing != null ? existing.profiles : null, result, ids);
        Add(extra != null ? extra.profiles : null, result, ids);
        result.Sort((a,b) => StringComparer.OrdinalIgnoreCase.Compare(Label(a), Label(b)));
        return result;
    }
    private static string Label(P p) => string.IsNullOrWhiteSpace(p.productName) ? p.name : p.productName;
    private static void Add(List<P> source, List<P> result, HashSet<string> ids)
    {
        if (source == null) return;
        foreach (var p in source)
        {
            if (p == null || result.Contains(p)) continue;
            if (p.kind != P.ComponentKind.CPU && p.kind != P.ComponentKind.Motherboard &&
                p.kind != P.ComponentKind.RAM && p.kind != P.ComponentKind.GPU) continue;
            if (!string.IsNullOrWhiteSpace(p.profileID) && !ids.Add(p.profileID.Trim())) continue;
            result.Add(p);
        }
    }
}
