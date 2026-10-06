using System;

public enum NetworkCableKind
{
    UTP,
    STP,
    Coaxial,
    FiberOptic
}

[Serializable]
public sealed class NetworkCableCheckpointData
{
    public int version = 1;
    public int completedSteps;
}

public static class NetworkCableCheckpointRules
{
    public const int TotalSteps = 4;

    public static bool IsValid(NetworkCableCheckpointData data)
    {
        // A completed activity clears its checkpoint, so only partial progress is saved.
        return data != null && data.version == 1 &&
            data.completedSteps > 0 && data.completedSteps < TotalSteps;
    }
}
