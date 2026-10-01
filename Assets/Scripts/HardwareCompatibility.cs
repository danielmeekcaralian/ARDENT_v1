using P = HardwareComponentProfile;

// Pairwise checks only. A positive result is not a whole-PC compatibility verdict.
public static class HardwareCompatibility
{
    public enum Status { Compatible, Incompatible, NeedsVerification }
    public readonly struct Result
    {
        public readonly Status status;
        public readonly string explanation;
        public Result(Status status, string explanation) { this.status = status; this.explanation = explanation; }
    }

    public static bool SupportsKind(P.ComponentKind kind)
    {
        return kind == P.ComponentKind.CPU ||
               kind == P.ComponentKind.Motherboard ||
               kind == P.ComponentKind.RAM ||
               kind == P.ComponentKind.Case || kind == P.ComponentKind.CPUCooler || kind == P.ComponentKind.GPU;
    }

    public static bool CanCompareKinds(P.ComponentKind a, P.ComponentKind b)
    {
        return
            (a == P.ComponentKind.Motherboard &&
             (b == P.ComponentKind.CPU ||
              b == P.ComponentKind.RAM ||
              b == P.ComponentKind.Case || b == P.ComponentKind.CPUCooler || b == P.ComponentKind.GPU)) ||
            (b == P.ComponentKind.Motherboard &&
             (a == P.ComponentKind.CPU ||
              a == P.ComponentKind.RAM ||
              a == P.ComponentKind.Case || a == P.ComponentKind.CPUCooler || a == P.ComponentKind.GPU));
    }

    public static Result Compare(P first, P second)
    {
        if (first == null || second == null)
            return Unknown("Choose two components with hardware profiles.");
        if (first == second)
            return Unknown("Choose two different components.");
        if (!CanCompareKinds(first.kind, second.kind))
            return Unknown("This checker currently compares a motherboard with a CPU, RAM module, GPU, case, or CPU cooler.");

        var board = first.kind == P.ComponentKind.Motherboard ? first : second;
        var part = board == first ? second : first;

        if (part.kind == P.ComponentKind.GPU)
        {
            if (part.graphicsInterface == P.GraphicsInterface.Unknown ||
                board.graphicsSlotSupport == P.GraphicsSlotSupport.Unknown)
                return Unknown("The GPU interface or motherboard graphics-slot specification is missing.");
            if (part.graphicsInterface != P.GraphicsInterface.PCIExpress)
                return Unknown("This checker currently verifies PCI Express graphics cards only.");
            if (board.graphicsSlotSupport == P.GraphicsSlotSupport.NoPCIExpressX16)
                return No("No PCI Express x16 graphics slot is recorded on this motherboard. Direct installation is not supported by this check; adapters are not evaluated.");
            if (board.graphicsSlotSupport != P.GraphicsSlotSupport.PCIExpressX16 ||
                part.graphicsPCIeGeneration <= 0 || board.graphicsPCIeGeneration <= 0)
                return Unknown("Confirm the PCI Express graphics slot and generation specifications for both parts.");
            int generation = System.Math.Min(part.graphicsPCIeGeneration, board.graphicsPCIeGeneration);
            return Yes($"Expansion-slot compatible: this PCIe {part.graphicsPCIeGeneration}.0 GPU can use the motherboard's PCIe {board.graphicsPCIeGeneration}.0 x16 slot at PCIe {generation}.0. " +
                "Different PCIe generations are compatible; available bandwidth may affect performance. This does not check PSU power/connectors, firmware, case fit, or overall build performance.");
        }

        if (part.kind == P.ComponentKind.CPUCooler)
        {
            if (board.cpuSocket == P.Socket.Unknown)
                return Unknown("The motherboard socket is missing.");
            var sockets = part.supportedCoolerSockets;
            if (sockets == null || sockets.Length == 0)
                return Unknown("The cooler mounting socket list is unverified.");
            foreach (var socket in sockets)
                if (socket == board.cpuSocket)
                    return Yes($"Mounting support confirmed for {board.cpuSocket} with the correct mounting kit. Case clearance, RAM clearance, and cooling performance are not checked.");
            return part.coolerSocketListComplete
                ? No($"Cooler mounting mismatch: its supported socket list excludes {board.cpuSocket}.")
                : Unknown($"Cooler mounting support for {board.cpuSocket} has not been verified.");
        }

        if (part.kind == P.ComponentKind.Case)
        {
            if (board.boardFormFactor == P.BoardFormFactor.Unknown)
                return Unknown("The motherboard's form factor is missing.");

            var supported = part.supportedBoardFormFactors;

            if (supported == null || supported.Length == 0)
                return Unknown("The case's supported motherboard sizes are unverified.");

            foreach (var form in supported)
            {
                if (form == board.boardFormFactor)
                    return Yes(
                        $"The case profile supports {board.boardFormFactor}. " +
                        "This checks the configured motherboard size only, " +
                        "not GPU, cooler, or other clearances.");
            }

            return Unknown(
                $"This case profile does not confirm support for " +
                $"{board.boardFormFactor}. Check the mounting points and clearance.");
        }

        if (part.kind == P.ComponentKind.CPU)
        {
            if (part.cpuSocket == P.Socket.Unknown || board.cpuSocket == P.Socket.Unknown)
                return Unknown("The CPU socket specification is missing for one of these components.");
            if (part.cpuSocket != board.cpuSocket)
                return No($"Socket mismatch: the CPU uses {part.cpuSocket}, while the motherboard uses {board.cpuSocket}.");
            if (!Contains(board.verifiedSupportedCPUs, part))
                return Unknown($"Both use {part.cpuSocket}, but this exact CPU has not been recorded as verified against the motherboard's CPU support list. Check its required BIOS version too. A missing entry does not mean the CPU is incompatible.");
            return Yes($"The {part.cpuSocket} socket matches and this CPU is recorded as supported by the motherboard. The board must run the BIOS version required for that CPU. This does not check the rest of the PC.");
        }

        // Report known conflicts before incomplete data; no inference from RAM speed alone.
        if (part.memoryGeneration != P.MemoryGeneration.Unknown && board.memoryGeneration != P.MemoryGeneration.Unknown &&
            part.memoryGeneration != board.memoryGeneration)
            return No($"Memory generation mismatch: this module is {part.memoryGeneration}, while the motherboard requires {board.memoryGeneration}.");
        if (part.moduleFormat != P.ModuleFormat.Unknown && board.moduleFormat != P.ModuleFormat.Unknown &&
            part.moduleFormat != board.moduleFormat)
            return No($"Module format mismatch: this RAM uses {Format(part.moduleFormat)}, while the motherboard requires {Format(board.moduleFormat)}.");
        if (part.moduleCapacityGB > 0 && board.maximumMemoryGB > 0 && part.moduleCapacityGB > board.maximumMemoryGB)
            return No($"This {part.moduleCapacityGB} GB module exceeds the motherboard's {board.maximumMemoryGB} GB total memory limit.");
        if (part.memoryGeneration == P.MemoryGeneration.Unknown || board.memoryGeneration == P.MemoryGeneration.Unknown ||
            part.moduleFormat == P.ModuleFormat.Unknown || board.moduleFormat == P.ModuleFormat.Unknown ||
            part.moduleCapacityGB <= 0 || board.maximumMemoryGB <= 0)
            return Unknown("A memory generation, module format, or capacity specification is missing. Fill in the profiles before verifying this pairing.");
        if (!Contains(board.verifiedSupportedRAM, part))
            return Unknown($"The {part.memoryGeneration} generation and {Format(part.moduleFormat)} format match, and one module is within the board's total memory limit. Exact module support still needs verification, including per-slot capacity and module organization. Its rated speed is not guaranteed.");
        return Yes($"This {part.memoryGeneration} {Format(part.moduleFormat)} module is recorded as supported by the motherboard. Actual speed depends on the CPU, BIOS, and memory settings. This checks one module, not a mixed kit or the whole PC.");
    }

    public readonly struct BuildResult
    {
        public readonly Result cpu, ram, gpu;
        public readonly Status status;
        public BuildResult(Result cpu, Result ram, Result gpu)
        {
            this.cpu = cpu; this.ram = ram; this.gpu = gpu;
            status = cpu.status == Status.Incompatible || ram.status == Status.Incompatible || gpu.status == Status.Incompatible
                ? Status.Incompatible
                : cpu.status == Status.NeedsVerification || ram.status == Status.NeedsVerification || gpu.status == Status.NeedsVerification
                    ? Status.NeedsVerification : Status.Compatible;
        }
    }

    // Each part is checked against the selected motherboard. A missing or misplaced
    // profile is unknown, never a pass. Known conflicts take precedence over unknowns.
    public static BuildResult CompareBuild(P motherboard, P cpu, P ram, P gpu)
    {
        return new BuildResult(BuildPair(motherboard, cpu, P.ComponentKind.CPU),
            BuildPair(motherboard, ram, P.ComponentKind.RAM),
            BuildPair(motherboard, gpu, P.ComponentKind.GPU));
    }

    private static Result BuildPair(P motherboard, P part, P.ComponentKind expected)
    {
        if (motherboard == null || motherboard.kind != P.ComponentKind.Motherboard)
            return Unknown("Select a motherboard profile.");
        if (part == null || part.kind != expected)
            return Unknown("Select a " + expected + " profile.");
        return Compare(motherboard, part);
    }

    private static bool Contains(P[] entries, P part)
    {
        if (entries == null) return false;
        foreach (var entry in entries) if (entry != null && entry == part) return true;
        return false;
    }
    private static string Format(P.ModuleFormat format) => format == P.ModuleFormat.SODIMM ? "SO-DIMM" : "desktop DIMM";
    private static Result Unknown(string message) => new Result(Status.NeedsVerification, message);
    private static Result No(string message) => new Result(Status.Incompatible, message);
    private static Result Yes(string message) => new Result(Status.Compatible, message);
}
