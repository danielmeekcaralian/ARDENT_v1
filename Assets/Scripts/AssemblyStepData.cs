using UnityEngine;

[System.Serializable]
public class AssemblyStepData
{
    [Header("Step Information")]
    public int stepNumber;

    public string stepTitle;

    [TextArea(2, 4)]
    public string instruction;

    [Header("Component")]
    public ARObjectData component;

    [Header("Target Anchor")]
    public string anchorID;

    [Header("Target")]
    public string targetID;

    [Header("Snap")]
    public float snapDistance = 0.15f;
}