public sealed class RJ45TestSequence
{
    public bool Running { get; private set; }
    public bool Passed { get; private set; }
    public int Pin { get; private set; }
    private float elapsed;
    public bool Start(bool ready)
    {
        if(!ready||Running||Passed)return false;
        Running=true;Pin=1;elapsed=0;return true;
    }
    public void Advance(float seconds)
    {
        if(!Running||seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return;
        // Keep each pin visible even after a slow frame.
        elapsed+=System.Math.Min(seconds,.1f);
        if(elapsed<.55f)return;
        elapsed-=.55f;
        if(Pin<8)Pin++;else{Running=false;Passed=true;}
    }
    public void RestorePassed(){Running=false;Passed=true;Pin=8;elapsed=0;}
    public void Reset(){Running=false;Passed=false;Pin=0;elapsed=0;}
}
