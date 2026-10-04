using System;

public enum RJ45WiringStandard { T568A, T568B }
public enum RJ45WireColor { WhiteOrange, Orange, WhiteGreen, Green, WhiteBlue, Blue, WhiteBrown, Brown }

// Independent of Unity objects, so the same rules can serve an AR workstation.
public sealed class RJ45WireOrder
{
    private static readonly RJ45WireColor[] A = { RJ45WireColor.WhiteGreen, RJ45WireColor.Green, RJ45WireColor.WhiteOrange, RJ45WireColor.Blue, RJ45WireColor.WhiteBlue, RJ45WireColor.Orange, RJ45WireColor.WhiteBrown, RJ45WireColor.Brown };
    private static readonly RJ45WireColor[] B = { RJ45WireColor.WhiteOrange, RJ45WireColor.Orange, RJ45WireColor.WhiteGreen, RJ45WireColor.Blue, RJ45WireColor.WhiteBlue, RJ45WireColor.Green, RJ45WireColor.WhiteBrown, RJ45WireColor.Brown };
    private readonly int[] slots = new int[8];
    public RJ45WiringStandard Standard { get; private set; }
    public int Count { get { int count = 0; foreach (int wire in slots) if (wire >= 0) count++; return count; } }
    public bool IsFull => Count == 8;
    public bool IsCorrect { get { if (!IsFull) return false; for (int i = 0; i < 8; i++) if (!IsPinCorrect(i)) return false; return true; } }

    public RJ45WireOrder(RJ45WiringStandard standard = RJ45WiringStandard.T568B) { Reset(standard); }
    public void Reset(RJ45WiringStandard standard)
    {
        if (standard != RJ45WiringStandard.T568A && standard != RJ45WiringStandard.T568B) throw new ArgumentOutOfRangeException(nameof(standard));
        Standard = standard;
        for (int i = 0; i < 8; i++) slots[i] = -1;
    }
    public int WireAt(int pin) { CheckPin(pin); return slots[pin]; }
    public int PinOf(RJ45WireColor wire) { CheckWire(wire); return Array.IndexOf(slots, (int)wire); }
    public RJ45WireColor ExpectedAt(int pin) { CheckPin(pin); return (Standard == RJ45WiringStandard.T568A ? A : B)[pin]; }
    public bool IsPinCorrect(int pin) => WireAt(pin) == (int)ExpectedAt(pin);
    public void Place(RJ45WireColor wire, int pin)
    {
        CheckWire(wire); CheckPin(pin);
        int source = PinOf(wire);
        if (source == pin) return;
        int displaced = slots[pin];
        slots[pin] = (int)wire;
        // A wire coming from the tray sends the displaced wire back to its tray.
        if (source >= 0) slots[source] = displaced;
    }
    public void Remove(RJ45WireColor wire) { int pin = PinOf(wire); if (pin >= 0) slots[pin] = -1; }
    private static void CheckPin(int pin) { if (pin < 0 || pin >= 8) throw new ArgumentOutOfRangeException(nameof(pin)); }
    private static void CheckWire(RJ45WireColor wire) { if ((int)wire < 0 || (int)wire >= 8) throw new ArgumentOutOfRangeException(nameof(wire)); }
}
