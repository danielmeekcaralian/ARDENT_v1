public enum RJ45PreparationStage { SelectTool, StripJacket, RemovingJacket, SeparatePairs, ArrangeWires }
public sealed class RJ45PreparationState
{
    public RJ45PreparationStage Stage { get; private set; }
    private int pairMask;
    public int SeparatedCount { get { int n=0; for(int i=0;i<4;i++) if(IsSeparated(i)) n++; return n; } }
    public bool IsSeparated(int pair) => pair>=0 && pair<4 && (pairMask & (1<<pair))!=0;
    public void Reset() { Stage=RJ45PreparationStage.SelectTool; pairMask=0; }
    public bool SelectTool() { if(Stage!=RJ45PreparationStage.SelectTool) return false; Stage=RJ45PreparationStage.StripJacket; return true; }
    public bool Strip() { if(Stage!=RJ45PreparationStage.StripJacket) return false; Stage=RJ45PreparationStage.RemovingJacket; return true; }
    public bool FinishRemoval() { if(Stage!=RJ45PreparationStage.RemovingJacket) return false; Stage=RJ45PreparationStage.SeparatePairs; return true; }
    public bool Separate(int pair)
    {
        if(Stage!=RJ45PreparationStage.SeparatePairs || pair<0 || pair>=4 || IsSeparated(pair)) return false;
        pairMask|=1<<pair;
        if(pairMask==15) Stage=RJ45PreparationStage.ArrangeWires;
        return true;
    }
}
