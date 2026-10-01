#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using P = HardwareComponentProfile;

public static class HardwareProfileSetup
{
    private const string Folder = "Assets/Resources/HardwareProfiles";

    [MenuItem("ARDENT/Hardware Library/Create Starter Hardware Profiles")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        { Debug.LogWarning("Exit Play Mode before creating hardware profiles."); return; }
        var cpu = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/cpu_Intel_Core_Ultra_7.prefab");
        var board = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/mb_Gigabyte_H810M.prefab");
        var ram = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/ram_Aorus.prefab");
        if (cpu == null || board == null || ram == null)
        { Debug.LogError("A starter CPU, motherboard, or RAM prefab is missing. No profiles were created."); return; }
        // Preflight all destination names before creating anything.
        foreach (string name in new[] { "CPU_CoreUltra7_265", "MB_H810M_H_Rev1", "RAM_Aorus_DDR5_16GB_5200", "HardwareProfileCatalog" })
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(Folder + "/" + name + ".asset");
            if (asset != null && !(name == "HardwareProfileCatalog" ? asset is HardwareProfileCatalog : asset is P))
            { Debug.LogError("A different asset already occupies the hardware profile path: " + name); return; }
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Resources", "HardwareProfiles");

        var cpuProfile = GetOrCreate("CPU_CoreUltra7_265", p => {
            p.profileID = "intel-core-ultra-7-265"; p.productName = "Intel Core Ultra 7 265";
            p.prefab = cpu; p.kind = P.ComponentKind.CPU; p.cpuSocket = P.Socket.LGA1851;
            p.cpuFamily = "Intel Core Ultra Series 2 (desktop)"; p.cpuModelNumber = "265";
            p.memoryGeneration = P.MemoryGeneration.DDR5;
            p.specificationURLs = new[] { "https://www.intel.com/content/www/us/en/products/sku/241068/intel-core-ultra-7-processor-265-30m-cache-up-to-5-30-ghz/specifications.html" };
            p.verificationNotes = "Using the user's model identification: desktop 265 without a suffix. Intel lists FCLGA1851; socket stored as LGA1851. Exact motherboard CPU support and required BIOS have not been verified.";
        });
        var boardProfile = GetOrCreate("MB_H810M_H_Rev1", p => {
            p.profileID = "gigabyte-h810m-h-rev-1-0"; p.productName = "GIGABYTE H810M H (rev. 1.0)";
            p.prefab = board; p.kind = P.ComponentKind.Motherboard; p.cpuSocket = P.Socket.LGA1851;
            p.cpuFamily = "Intel Core Ultra (check exact CPU support list)";
            p.memoryGeneration = P.MemoryGeneration.DDR5; p.moduleFormat = P.ModuleFormat.DIMM;
            p.boardFormFactor = P.BoardFormFactor.MicroATX; p.boardSizeMM = new Vector2(244, 215);
            p.physicalMemorySlots = 2; p.maximumMemoryGB = 128;
            p.specificationURLs = new[] { "https://www.gigabyte.com/Motherboard/H810M-H-rev-10/sp" };
            p.cpuSupportListURL = "https://www.gigabyte.com/Motherboard/H810M-H-rev-10/support#support-cpu";
            p.memorySupportListURL = "https://www.gigabyte.com/Motherboard/H810M-H-rev-10/support#support-memsup";
            p.verificationNotes = "Specifications verified against rev. 1.0. Exact CPU/BIOS and memory-module support lists remain unchecked. Physical slot count is independent of the prefab's currently configured single RAM snap target. Supports unbuffered DDR5; exact supported module organization also matters.";
        });
        var ramProfile = GetOrCreate("RAM_Aorus_DDR5_16GB_5200", p => {
            p.profileID = "aorus-ddr5-16gb-5200-module"; p.productName = "AORUS DDR5 16 GB module (5200 MT/s)";
            p.prefab = ram; p.kind = P.ComponentKind.RAM;
            p.memoryGeneration = P.MemoryGeneration.DDR5; p.moduleFormat = P.ModuleFormat.DIMM;
            p.moduleCapacityGB = 16; p.ratedSpeedMTs = 5200; p.spdSpeedMTs = 4800;
            p.memoryPerformanceProfile = "XMP 3.0";
            p.specificationURLs = new[] { "https://www.gigabyte.com/de/Memory/AORUS-Memory-DDR5-32GB--2x16GB-5200MT-s/sp" };
            p.displayNote = "One 16 GB module. Actual memory speed depends on the system configuration.";
            p.verificationNotes = "Reference: GP-ARS32G52D5, sold as a 2 x 16 GB kit. This profile represents the user's single stick, not a separate verified 1 x 16 GB retail SKU. UDIMM, 5200 MT/s tested with XMP, 4800 MT/s SPD. Exact motherboard memory support has not been verified.";
        });
        string catalogPath = Folder + "/HardwareProfileCatalog.asset";
        var catalog = AssetDatabase.LoadAssetAtPath<HardwareProfileCatalog>(catalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<HardwareProfileCatalog>();
            AssetDatabase.CreateAsset(catalog, catalogPath);
        }
        if (catalog.profiles == null) catalog.profiles = new List<P>();
        foreach (var profile in new[] { cpuProfile, boardProfile, ramProfile })
        {
            if (catalog.profiles.Contains(profile)) continue;
            if (catalog.profiles.Exists(p => p != null && p.prefab == profile.prefab))
            { Debug.LogWarning("Keeping the existing catalog mapping for " + profile.productName + ".", catalog); continue; }
            catalog.profiles.Add(profile);
        }
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssetIfDirty(catalog);
        Selection.activeObject = catalog;
        EditorGUIUtility.PingObject(catalog);
        Debug.Log("Hardware profiles ready in " + Folder + ". Existing profiles were preserved. Test an unlocked CPU, motherboard, or RAM in Hardware Library.", catalog);
    }

    private static P GetOrCreate(string name, Action<P> initialize)
    {
        string path = Folder + "/" + name + ".asset";
        var profile = AssetDatabase.LoadAssetAtPath<P>(path);
        if (profile != null) return profile;
        profile = ScriptableObject.CreateInstance<P>();
        initialize(profile);
        AssetDatabase.CreateAsset(profile, path);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssetIfDirty(profile);
        return profile;
    }
}
#endif
