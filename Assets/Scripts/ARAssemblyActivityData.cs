using UnityEngine;

[CreateAssetMenu(
    fileName = "New Assembly Activity",
    menuName = "ARDENT/AR Assembly Activity"
)]
public class ARAssemblyActivityData : ScriptableObject
{
    [Header("Activity Information")]
    public string activityTitle;

    [TextArea(2, 5)]
    public string description;

    [Header("Combined Activity")]
    [Tooltip("After assembly, remove the installed parts in reverse order.")]
    public bool includeDisassembly;

    [Header("Assembly Steps")]
    public AssemblyStepData[] steps;
}