using UnityEngine;

[CreateAssetMenu(
    fileName = "ARActivity",
    menuName = "ARDENT/AR Activity Data"
)]
public class ARActivityData : ScriptableObject
{
    [Header("Activity Information")]
    public string activityID;
    public string activityTitle;

    [TextArea(2, 5)]
    public string instruction;

    [Header("AR Content")]
    public GameObject modelPrefab;

    [Header("Placement")]
    public bool requirePlanePlacement = true;

    [Header("Interaction")]
    public bool allowRotation = true;
    public bool allowScaling = true;
    public bool allowMovement = true;
}