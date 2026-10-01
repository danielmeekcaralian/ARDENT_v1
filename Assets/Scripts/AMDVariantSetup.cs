#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using P = HardwareComponentProfile;

public static class AMDVariantSetup
{
    private const string Folder = "Assets/Resources/HardwareProfiles/";
    private static readonly string[] Models = { "cpu_AMD_Ryzen_5_5600", "mb_MSI_B550M", "ram_Kingston_DDR4" };
    private static readonly string[] Names = { "CPU_Ryzen5_5600", "MB_MSI_B550M_PRO_VDH_WIFI", "RAM_Kingston_KF432C16BB_16" };

    [MenuItem("ARDENT/Hardware Library/Add AMD and DDR4 Variants")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        { Debug.LogWarning("Exit Play Mode first."); return; }
        var catalog = AssetDatabase.LoadAssetAtPath<HardwareProfileCatalog>(Folder + "HardwareProfileCatalog.asset");
        var activity = AssetDatabase.LoadAssetAtPath<ARActivityData>("Assets/Scripts/COC1_L4_AR.asset");
        if (catalog == null || activity == null)
        { Debug.LogError("The hardware profile catalog or assembly lesson activity is missing."); return; }
        var prefabs = new GameObject[3];
        for (int i = 0; i < Models.Length; i++)
        {
            prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/" + Models[i] + ".prefab");
            var existing = AssetDatabase.LoadMainAssetAtPath(Folder + Names[i] + ".asset");
            if (prefabs[i] == null || (existing != null && !(existing is P)))
            { Debug.LogError("Missing prefab or conflicting profile path: " + Models[i]); return; }
            if (existing is P profile && profile.prefab != prefabs[i])
            { Debug.LogError("Existing profile references another prefab: " + Names[i]); return; }
            if (catalog.profiles != null && catalog.profiles.Exists(p => p != null && p.prefab == prefabs[i] && p != existing))
            { Debug.LogError("A different profile already maps this prefab: " + Models[i]); return; }
        }

        var cpu = GetOrCreate(0, p => {
            p.profileID = "amd-ryzen-5-5600"; p.productName = "AMD Ryzen 5 5600";
            p.prefab = prefabs[0]; p.kind = P.ComponentKind.CPU; p.cpuSocket = P.Socket.AM4;
            p.cpuFamily = "AMD Ryzen 5000 Series"; p.cpuModelNumber = "5600"; p.memoryGeneration = P.MemoryGeneration.DDR4;
            p.specificationURLs = new[] { "https://www.amd.com/en/support/downloads/drivers.html/processors/ryzen/ryzen-5000-series/amd-ryzen-5-5600.html" };
            p.verificationNotes = "Standard 5600, not 5600G or 5600X. AM4/DDR4 specifications checked 2026-10-01. Exact board BIOS support remains unverified.";
        });
        var board = GetOrCreate(1, p => {
            p.profileID = "msi-b550m-pro-vdh-wifi"; p.productName = "MSI B550M PRO-VDH WIFI";
            p.prefab = prefabs[1]; p.kind = P.ComponentKind.Motherboard; p.cpuSocket = P.Socket.AM4;
            p.cpuFamily = "AMD Ryzen (check exact CPU support list)";
            p.memoryGeneration = P.MemoryGeneration.DDR4; p.moduleFormat = P.ModuleFormat.DIMM;
            p.physicalMemorySlots = 4; p.maximumMemoryGB = 128;
            p.boardFormFactor = P.BoardFormFactor.MicroATX; p.boardSizeMM = new Vector2(244, 244);
            p.graphicsSlotSupport = P.GraphicsSlotSupport.PCIExpressX16;
            // The board supports Gen4/Gen3 depending on CPU. Leave the fixed generation
            // unknown until the checker models that dependency, avoiding false speed claims.
            p.graphicsPCIeGeneration = 0;
            p.cpuSupportListURL = "https://www.msi.com/Motherboard/B550M-PRO-VDH-WIFI/support#cpu";
            p.memorySupportListURL = "https://www.msi.com/Motherboard/B550M-PRO-VDH-WIFI/support#mem";
            p.specificationURLs = new[] { "https://www.msi.com/Motherboard/B550M-PRO-VDH-WIFI/Specification" };
            p.displayNote = "Graphics slot supports PCIe 4.0/3.0 depending on CPU. Exact CPU/BIOS and RAM support remain unverified.";
            p.verificationNotes = "Specifications checked 2026-10-01. Socket and DDR mismatches can be checked; no exact support-list entries are inferred from family support.";
        });
        var ram = GetOrCreate(2, p => {
            p.profileID = "kingston-kf432c16bb-16"; p.productName = "Kingston FURY Beast DDR4-3200 16 GB";
            p.prefab = prefabs[2]; p.kind = P.ComponentKind.RAM;
            p.memoryGeneration = P.MemoryGeneration.DDR4; p.moduleFormat = P.ModuleFormat.DIMM;
            p.moduleCapacityGB = 16; p.ratedSpeedMTs = 3200; p.spdSpeedMTs = 2400;
            p.memoryPerformanceProfile = "XMP 2.0";
            p.specificationURLs = new[] { "https://www.kingston.com/dataSheets/KF432C16BB_16.pdf" };
            p.verificationNotes = "KF432C16BB/16, one 16 GB 1Rx8 module. Datasheet verified 2026-10-01. XMP 3200 MT/s; JEDEC SPD 2400 MT/s. Exact motherboard support list remains unverified.";
            p.displayNote = "One 16 GB module. 3200 MT/s is the XMP rating; actual speed depends on configuration.";
        });
        if (catalog.profiles == null) catalog.profiles = new List<P>();
        foreach (var profile in new[] { cpu, board, ram })
            if (!catalog.profiles.Contains(profile)) catalog.profiles.Add(profile);

        var bonus = new List<ARObjectData>(activity.libraryBonusObjects ?? new ARObjectData[0]);
        var originals = new[] { "cpu_Intel_Core_Ultra_7", "mb_Gigabyte_H810M", "ram_Aorus" };
        for (int i = 0; i < prefabs.Length; i++)
        {
            var existing = bonus.Find(item => item != null && item.prefab == prefabs[i]);
            if (existing != null) { existing.RefreshLibraryIdentity(); continue; }
            Sprite icon = null;
            if (activity.availableObjects != null)
                foreach (var item in activity.availableObjects)
                    if (item?.prefab != null && item.prefab.name == originals[i]) { icon = item.inventoryImage; break; }
            var reward = new ARObjectData { prefab = prefabs[i], inventoryImage = icon };
            reward.RefreshLibraryIdentity(); bonus.Add(reward);
        }
        activity.libraryBonusObjects = bonus.ToArray();
        EditorUtility.SetDirty(activity); AssetDatabase.SaveAssetIfDirty(activity);
        EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssetIfDirty(catalog);
        Selection.activeObject = catalog;
        Debug.Log("AMD/DDR4 profiles and assembly-lesson Gold bonus unlocks are ready. Existing profiles and lesson steps were preserved. Open Hardware Library to synchronize existing Gold medals. AR setup is a separate next step.", catalog);
    }

    private static P GetOrCreate(int index, Action<P> initialize)
    {
        string path = Folder + Names[index] + ".asset";
        var profile = AssetDatabase.LoadAssetAtPath<P>(path);
        if (profile != null) return profile;
        profile = ScriptableObject.CreateInstance<P>(); initialize(profile);
        AssetDatabase.CreateAsset(profile, path); AssetDatabase.SaveAssetIfDirty(profile);
        return profile;
    }
}
#endif
