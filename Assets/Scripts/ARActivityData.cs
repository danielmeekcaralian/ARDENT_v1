using UnityEngine;

public enum ARActivityType
{
    ToolIdentification,
    HardwareIdentification,
    Assembly,
    Disassembly,
    Sandbox,
    NetworkDesign,
    RJ45Termination
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

    [Header("Bonus Hardware Library Unlocks")]
    [Tooltip("Unlocked by this lesson's Gold medal, without adding required lesson activity objects.")]
    public ARObjectData[] libraryBonusObjects;

    public System.Collections.Generic.IEnumerable<ARObjectData> LibraryObjects()
    {
        if (availableObjects != null)
            foreach (var item in availableObjects) yield return item;
        if (libraryBonusObjects != null)
            foreach (var item in libraryBonusObjects) yield return item;
    }

    [Header("Placement")]
    public bool requirePlanePlacement = true;

    [Header("Interaction")]
    public bool allowRotation = true;
    public bool allowScaling = true;
    public bool allowMovement = true;
#if UNITY_EDITOR
    private void OnValidate()
    {
        foreach (var item in LibraryObjects())
            if (item != null) item.RefreshLibraryIdentity();
    }
#endif
}
