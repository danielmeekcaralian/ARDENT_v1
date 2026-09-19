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
    [Min(0.001f)]
    public float snapDistance = 0.15f;

    [Header("Rotation")]
    [Range(0f, 180f)]
    public float rotationTolerance = 15f;
}