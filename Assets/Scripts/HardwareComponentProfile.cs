using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HardwareProfile", menuName = "ARDENT/Hardware Component Profile")]
public class HardwareComponentProfile : ScriptableObject
{
    public enum ComponentKind { Unknown, CPU, Motherboard, RAM, Case, CPUCooler, GPU }
    public enum Socket { Unknown, LGA1851, LGA1700, AM4, AM5 }
    public enum MemoryGeneration { Unknown, DDR3, DDR4, DDR5 }
    public enum ModuleFormat { Unknown, DIMM, SODIMM }
    public enum BoardFormFactor { Unknown, ATX, MicroATX, MiniITX }

    public enum GraphicsInterface { Unknown, PCIExpress, AGP, PCI }
    public enum GraphicsSlotSupport { Unknown, PCIExpressX16, NoPCIExpressX16 }

    [Header("Graphics Expansion")]
    public GraphicsInterface graphicsInterface;
    [Tooltip("Motherboard desktop graphics slot. Unknown is distinct from confirmed absent. Adapters and open-ended slots are outside this check.")]
    public GraphicsSlotSupport graphicsSlotSupport;
    [Tooltip("GPU maximum PCIe generation, or motherboard primary graphics-slot generation. Zero means unknown; this is not lane count.")]
    [Min(0)] public int graphicsPCIeGeneration;

    [Header("Identity")]
    public string profileID;
    public string productName;
    public GameObject prefab;
    public ComponentKind kind;

    [Header("CPU / Motherboard")]
    public Socket cpuSocket;
    [Tooltip("CPU family for a CPU profile; advertised supported family for a motherboard. This alone does not verify BIOS support.")]
    public string cpuFamily;
    public string cpuModelNumber;

    [Header("Memory")]
    public MemoryGeneration memoryGeneration;
    public ModuleFormat moduleFormat;
    [Tooltip("Capacity of ONE modeled module, not the retail kit. Zero means unknown.")]
    [Min(0)] public int moduleCapacityGB;
    [Tooltip("Advertised/tested module speed. This is not a guaranteed operating speed.")]
    [Min(0)] public int ratedSpeedMTs;
    [Min(0)] public int spdSpeedMTs;
    public string memoryPerformanceProfile;

    [Header("Motherboard")]
    public BoardFormFactor boardFormFactor;
    [Min(0)] public int physicalMemorySlots;
    [Min(0)] public int maximumMemoryGB;
    [Tooltip("Physical PCB dimensions in millimeters; does not resize the model.")]
    public Vector2 boardSizeMM;
    [Tooltip("Exact CPU profiles verified against the manufacturer's support list. Empty means not yet checked, not incompatible.")]
    public HardwareComponentProfile[] verifiedSupportedCPUs = new HardwareComponentProfile[0];
    [Tooltip("Exact RAM profiles verified against manufacturer support data, including module organization and per-slot capacity. Empty or missing entries mean unverified, not incompatible.")]
    public HardwareComponentProfile[] verifiedSupportedRAM = new HardwareComponentProfile[0];
    public string cpuSupportListURL;
    public string memorySupportListURL;

    [Header("CPU Cooler")]
    public Socket[] supportedCoolerSockets = new Socket[0];
    [Tooltip("True only when the list covers all supported sockets represented by this app. Otherwise missing entries remain unverified.")]
    public bool coolerSocketListComplete;
    [Min(0)] public int coolerHeightMM;

    [Header("Case")]
    public BoardFormFactor[] supportedBoardFormFactors = new BoardFormFactor[0];

    [Header("Sources and Verification")]
    public string[] specificationURLs = new string[0];
    [TextArea(2, 6)] public string verificationNotes;
    [TextArea(2, 4)] public string displayNote;

    public string SpecificationSummary()
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(productName)) lines.Add(productName);
        if (kind == ComponentKind.CPU || kind == ComponentKind.Motherboard)
        {
            lines.Add("CPU socket: " + cpuSocket);
            if (!string.IsNullOrWhiteSpace(cpuFamily)) lines.Add("CPU family: " + cpuFamily);
        }
        if (kind == ComponentKind.CPU || kind == ComponentKind.Motherboard || kind == ComponentKind.RAM)
            lines.Add("Memory type: " + memoryGeneration);
        if (kind == ComponentKind.RAM || kind == ComponentKind.Motherboard)
            lines.Add("Module format: " + (moduleFormat == ModuleFormat.DIMM ? "Desktop DIMM" :
                moduleFormat == ModuleFormat.SODIMM ? "SO-DIMM" : "Unknown"));
        if (kind == ComponentKind.RAM)
        {
            lines.Add("Capacity per module: " + (moduleCapacityGB > 0 ? moduleCapacityGB + " GB" : "Unknown"));
            if (ratedSpeedMTs > 0) lines.Add("Rated speed: " + ratedSpeedMTs + " MT/s");
            if (spdSpeedMTs > 0) lines.Add("SPD speed: " + spdSpeedMTs + " MT/s");
            if (!string.IsNullOrWhiteSpace(memoryPerformanceProfile)) lines.Add("Performance profile: " + memoryPerformanceProfile);
        }
        if (kind == ComponentKind.Motherboard)
        {
            lines.Add("Form factor: " + FormFactorName(boardFormFactor));
            if (boardSizeMM.x > 0 && boardSizeMM.y > 0) lines.Add($"Board size: {boardSizeMM.x:0.#} x {boardSizeMM.y:0.#} mm");
            if (physicalMemorySlots > 0) lines.Add("Physical memory slots: " + physicalMemorySlots);
            if (maximumMemoryGB > 0) lines.Add("Maximum memory: " + maximumMemoryGB + " GB");
        }
        if (kind == ComponentKind.Case)
        {
            var forms = new List<string>();
            if (supportedBoardFormFactors != null)
                foreach (var form in supportedBoardFormFactors) forms.Add(FormFactorName(form));
            lines.Add("Supported motherboards: " + (forms.Count > 0 ? string.Join(", ", forms) : "Unknown"));
        }
        if (kind == ComponentKind.CPUCooler)
        {
            var sockets = new List<string>();
            if (supportedCoolerSockets != null)
                foreach (var socket in supportedCoolerSockets) sockets.Add(socket.ToString());
            lines.Add("Mounting sockets: " + (sockets.Count > 0 ? string.Join(", ", sockets) : "Unknown"));
            if (coolerHeightMM > 0) lines.Add("Cooler height: " + coolerHeightMM + " mm");
        }
        if (kind == ComponentKind.GPU)
            lines.Add("Graphics interface: " + graphicsInterface);
        if (kind == ComponentKind.Motherboard)
            lines.Add("Graphics slot: " + (graphicsSlotSupport == GraphicsSlotSupport.PCIExpressX16
                ? "PCI Express x16" : graphicsSlotSupport == GraphicsSlotSupport.NoPCIExpressX16
                ? "No PCI Express x16 slot" : "Unknown"));
        if ((kind == ComponentKind.GPU || kind == ComponentKind.Motherboard) && graphicsPCIeGeneration > 0)
            lines.Add("Graphics PCIe generation: " + graphicsPCIeGeneration + ".0");
        if (!string.IsNullOrWhiteSpace(displayNote)) lines.Add(displayNote);
        return string.Join("\n", lines);
    }

    private static string FormFactorName(BoardFormFactor form)
    {
        return form == BoardFormFactor.MicroATX ? "microATX" : form == BoardFormFactor.MiniITX ? "Mini-ITX" : form.ToString();
    }
}
