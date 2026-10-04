using System;
// Samples the TOOL contact position, never a bare swipe over the cable.
public sealed class RJ45CrossStripGesture
{
    private int entrySide;
    private bool crossed;
    public void Reset() { entrySide=0;crossed=false; }
    public void Sample(float x,float y,float centerY,float halfWidth,float tolerance)
    {
        if(float.IsNaN(x)||float.IsNaN(y)||float.IsInfinity(x)||float.IsInfinity(y)||Math.Abs(y-centerY)>tolerance)
        {Reset();return;}
        if(entrySide==0) { if(x<=-halfWidth)entrySide=-1;else if(x>=halfWidth)entrySide=1;return; }
        crossed=entrySide<0 ? x>=halfWidth : x<=-halfWidth;
    }
    public bool CanFinish => crossed;
}
