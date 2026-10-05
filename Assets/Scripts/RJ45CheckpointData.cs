using System;

public enum RJ45SavedStage { Prepare, Arrange, ReadyToTrim, Trimmed, Inserted, Crimped, TestPassed }
[Serializable]
public sealed class RJ45CheckpointData
{
    public int version=1;
    public int standard;
    public RJ45SavedStage stage;
    // Pin index -> conductor identity, or -1 for empty. Free wires return to their authored homes.
    public int[] slots;
}
public static class RJ45CheckpointRules
{
    public static bool IsValid(RJ45CheckpointData data)
    {
        if(data==null||data.version!=1||data.standard<0||data.standard>1||
            (int)data.stage<0||(int)data.stage>6||data.slots==null||data.slots.Length!=8)return false;
        int mask=0;
        var expected=new RJ45WireOrder((RJ45WiringStandard)data.standard);
        for(int i=0;i<8;i++)
        {
            int wire=data.slots[i];
            if(wire< -1||wire>7)return false;
            if(wire>=0){if((mask&(1<<wire))!=0)return false;mask|=1<<wire;}
            if(data.stage==RJ45SavedStage.Prepare&&wire!=-1)return false;
            if(data.stage>=RJ45SavedStage.ReadyToTrim&&wire!=(int)expected.ExpectedAt(i))return false;
        }
        return true;
    }
}
