using UnityEngine;

public class ARSceneController : MonoBehaviour
{
    [Header("Activity")]
    [SerializeField] private ARActivityData activityData;

    [Header("Systems")]
    [SerializeField] private ARPlacementManager placementManager;

    private void Start()
    {
        if (activityData == null)
        {
            Debug.LogError("No AR Activity Data assigned.");
            return;
        }

        placementManager.SetActivity(activityData);
    }
}