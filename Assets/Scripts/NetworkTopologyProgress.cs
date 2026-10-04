// Passed exercises retain credit even after their practice graph is changed.
public sealed class NetworkTopologyProgress
{
    private int passedMask;
    public int PassedMask => passedMask;
    public void Restore(int mask)
    {
        if (mask < 0 || mask > 7) throw new System.ArgumentOutOfRangeException(nameof(mask));
        passedMask = mask;
    }
    public bool IsComplete => passedMask == 7;
    public bool HasPassed(int index) => index >= 0 && index < 3 && (passedMask & (1 << index)) != 0;
    public int Count => (HasPassed(0) ? 1 : 0) + (HasPassed(1) ? 1 : 0) + (HasPassed(2) ? 1 : 0);
    public bool Record(int index, bool valid)
    {
        if (!valid || index < 0 || index > 2 || HasPassed(index)) return false;
        passedMask |= 1 << index;
        return true;
    }
    public void Reset() => passedMask = 0;
    public string Summary => $"Topologies completed: {Count}/3 | Star {(HasPassed(0) ? "done" : "pending")}, Ring {(HasPassed(1) ? "done" : "pending")}, Bus {(HasPassed(2) ? "done" : "pending")}";
}
