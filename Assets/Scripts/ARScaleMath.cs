using System;

// Shared, engine-independent zoom limits. A multiplier of 1 is the calibrated size.
public static class ARScaleMath
{
    public static float NextMultiplier(float current, float amount, float maximum)
    {
        if (float.IsNaN(maximum) || float.IsInfinity(maximum)) maximum = 4f;
        maximum = Math.Max(1f, maximum);
        if (float.IsNaN(current) || float.IsInfinity(current)) current = 1f;
        current = Math.Max(1f, Math.Min(maximum, current));
        if (float.IsNaN(amount) || float.IsInfinity(amount)) return current;
        double next = current * Math.Exp(Math.Max(-1f, Math.Min(1f, amount)));
        return (float)Math.Max(1d, Math.Min(maximum, next));
    }
}
