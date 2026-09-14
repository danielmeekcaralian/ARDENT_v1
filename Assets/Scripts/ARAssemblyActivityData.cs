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

    [Header("Assembly Steps")]
    public AssemblyStepData[] steps;
}