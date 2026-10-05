#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using P = HardwareComponentProfile;

public static class HardwareCompatibilityCatalogSetup
{
    private const string Folder = "Assets/Resources/Compatibility";
    [MenuItem("ARDENT/Hardware Library/Add Compatibility Catalog")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Exit Play Mode first."); return; }
        var names = new[] { "CPU_Ryzen7600", "CPU_i5_12400F", "MB_PRO_B650M_P", "MB_PRO_B760M_P_DDR4", "RAM_KF556C36BBE_16", "RAM_KF432C16BB_8", "GPU_RTX4060_VENTUS_OC", "GPU_RX6600_MECH", "GPU_RX7600_MECH_OC", "HardwareCompatibilityCatalog" };
        foreach (var name in names)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(Folder + "/" + name + ".asset");
            if (asset != null && !(name == "HardwareCompatibilityCatalog" ? asset is HardwareCompatibilityCatalog : asset is P))
            { Debug.LogError("Another asset occupies " + name + ". No changes made."); return; }
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Resources", "Compatibility");
        var catalog = AssetDatabase.LoadAssetAtPath<HardwareCompatibilityCatalog>(Folder + "/HardwareCompatibilityCatalog.asset");
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<HardwareCompatibilityCatalog>();
            AssetDatabase.CreateAsset(catalog, Folder + "/HardwareCompatibilityCatalog.asset");
        }
        if (catalog.profiles == null) catalog.profiles = new List<P>();
        Add(catalog, names[0], "amd-ryzen-5-7600", "AMD Ryzen 5 7600", P.ComponentKind.CPU, p => {
            p.cpuSocket = P.Socket.AM5; p.cpuFamily = "AMD Ryzen 7000"; p.cpuModelNumber = "7600"; p.memoryGeneration = P.MemoryGeneration.DDR5;
            p.specificationURLs = new[] { "https://www.amd.com/en/products/processors/desktops/ryzen/7000-series/amd-ryzen-5-7600.html" };
        });
        Add(catalog, names[1], "intel-core-i5-12400f", "Intel Core i5-12400F", P.ComponentKind.CPU, p => {
            p.cpuSocket = P.Socket.LGA1700; p.cpuFamily = "Intel Core 12th generation"; p.cpuModelNumber = "12400F";
            p.displayNote = "Supports DDR4 or DDR5 depending on motherboard; requires a discrete graphics card.";
            p.specificationURLs = new[] { "https://www.intel.com/content/www/us/en/products/sku/134587/intel-core-i512400f-processor-18m-cache-up-to-4-40-ghz/specifications.html" };
        });
        Add(catalog, names[2], "msi-pro-b650m-p", "MSI PRO B650M-P", P.ComponentKind.Motherboard, p => {
            Board(p, P.Socket.AM5, P.MemoryGeneration.DDR5, 256, "PRO-B650M-P");
            p.cpuFamily = "AMD Ryzen 9000 / 8000 / 7000";
        });
        Add(catalog, names[3], "msi-pro-b760m-p-ddr4", "MSI PRO B760M-P DDR4", P.ComponentKind.Motherboard, p => {
            Board(p, P.Socket.LGA1700, P.MemoryGeneration.DDR4, 128, "PRO-B760M-P-DDR4");
            p.cpuFamily = "Intel Core 14th / 13th / 12th generation";
        });
        Add(catalog, names[4], "kingston-kf556c36bbe-16", "Kingston FURY Beast DDR5-5600 16 GB (KF556C36BBE-16)", P.ComponentKind.RAM, p => {
            Ram(p, P.MemoryGeneration.DDR5, 16, 5600, 4800, "AMD EXPO / Intel XMP 3.0", "KF556C36BBE-16");
        });
        Add(catalog, names[5], "kingston-kf432c16bb-8", "Kingston FURY Beast DDR4-3200 8 GB (KF432C16BB/8)", P.ComponentKind.RAM, p => {
            Ram(p, P.MemoryGeneration.DDR4, 8, 3200, 2400, "Intel XMP 2.0", "KF432C16BB_8");
        });
        Add(catalog, names[6], "msi-rtx4060-ventus-2x-black-8g-oc", "MSI GeForce RTX 4060 VENTUS 2X BLACK 8G OC", P.ComponentKind.GPU, p => {
            p.graphicsInterface = P.GraphicsInterface.PCIExpress; p.graphicsPCIeGeneration = 4;
            p.displayNote = "PCIe 4.0 x8 electrical interface. Power, case fit, and performance are outside this check.";
            p.specificationURLs = new[] { "https://storage-asset.msi.com/datasheet/vga/in/GeForce-RTX-4060-VENTUS-2X-BLACK-8G-OC.pdf" };
        });
        Add(catalog, names[7], "msi-rx6600-mech-2x-8g", "MSI AMD Radeon RX 6600 MECH 2X 8G", P.ComponentKind.GPU, p => {
            p.graphicsInterface = P.GraphicsInterface.PCIExpress; p.graphicsPCIeGeneration = 4;
            p.displayNote = "8 GB GDDR6; PCIe 4.0 x8 electrical interface. Power, case fit, and performance are outside this check.";
            p.specificationURLs = new[] { "https://www.msi.com/Graphics-Card/Radeon-RX-6600-MECH-2X-8G/Specification" };
            p.verificationNotes = "Interface and memory specification checked against MSI on 2026-10-05. No 3D model required.";
        });
        Add(catalog, names[8], "msi-rx7600-mech-2x-classic-8g-oc", "MSI AMD Radeon RX 7600 MECH 2X CLASSIC 8G OC", P.ComponentKind.GPU, p => {
            p.graphicsInterface = P.GraphicsInterface.PCIExpress; p.graphicsPCIeGeneration = 4;
            p.displayNote = "8 GB GDDR6; PCIe 4.0 x8 electrical interface. Power, case fit, and performance are outside this check.";
            p.specificationURLs = new[] { "https://www.msi.com/Graphics-Card/Radeon-RX-7600-MECH-2X-CLASSIC-8G-OC/Specification" };
            p.verificationNotes = "Interface and memory specification checked against MSI on 2026-10-05. No 3D model required.";
        });
        EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
        Selection.activeObject = catalog;
        Debug.Log("Compatibility catalog ready. Nine starter entries; existing edits preserved. All entries are available without lesson unlocks. No 3D models required.");
    }
    private static void Board(P p, P.Socket socket, P.MemoryGeneration memory, int maximum, string model)
    {
        p.cpuSocket = socket; p.memoryGeneration = memory; p.moduleFormat = P.ModuleFormat.DIMM;
        p.physicalMemorySlots = 4; p.maximumMemoryGB = maximum; p.boardFormFactor = P.BoardFormFactor.MicroATX;
        p.graphicsSlotSupport = P.GraphicsSlotSupport.PCIExpressX16; p.graphicsPCIeGeneration = 4;
        p.cpuSupportListURL = "https://www.msi.com/Motherboard/" + model + "/support#cpu";
        p.memorySupportListURL = "https://www.msi.com/Motherboard/" + model + "/support#mem";
        p.specificationURLs = new[] { "https://www.msi.com/Motherboard/" + model + "/Specification" };
        // Product specs establish socket/memory type, not exact CPU BIOS or RAM QVL matches.
        p.verificationNotes = "Specifications checked 2026-10-05. Exact CPU support/BIOS and RAM QVL not yet recorded. Keep verified lists empty until checked.";
    }
    private static void Ram(P p, P.MemoryGeneration generation, int capacity, int rated, int spd, string profile, string sheet)
    {
        p.memoryGeneration = generation; p.moduleFormat = P.ModuleFormat.DIMM; p.moduleCapacityGB = capacity;
        p.ratedSpeedMTs = rated; p.spdSpeedMTs = spd; p.memoryPerformanceProfile = profile;
        p.specificationURLs = new[] { "https://www.kingston.com/dataSheets/" + sheet + ".pdf" };
    }
    private static void Add(HardwareCompatibilityCatalog catalog, string file, string id, string label, P.ComponentKind kind, Action<P> configure)
    {
        var path = Folder + "/" + file + ".asset";
        var p = AssetDatabase.LoadAssetAtPath<P>(path);
        if (p == null)
        {
            p = ScriptableObject.CreateInstance<P>(); p.profileID = id; p.productName = label; p.kind = kind;
            configure(p); AssetDatabase.CreateAsset(p, path);
        }
        if (!catalog.profiles.Contains(p)) catalog.profiles.Add(p);
    }
}
#endif
