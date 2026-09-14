using UnityEngine;

public enum ARActivityType
{
    ToolIdentification,
    HardwareIdentification,
    Assembly,
    Disassembly
}

[CreateAssetMenu(
    fileName = "ARActivity",
    menuName = "ARDENT/AR Activity Data"
)]
public class ARActivityData : ScriptableObject
{
    [Header("Activity Information")]
    public string activityID;
    public string activityTitle;

    public ARActivityType activityType;

    [Header("Assembly")]
    public ARAssemblyActivityData assemblyActivity;

    [TextArea(2, 5)]
    public string instruction;

    [Header("Available AR Objects")]
    public ARObjectData[] availableObjects;

    [Header("Placement")]
    public bool requirePlanePlacement = true;

    [Header("Interaction")]
    public bool allowRotation = true;
    public bool allowScaling = true;
    public bool allowMovement = true;
}