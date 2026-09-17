using UnityEngine;

public class ARActivityRouter : MonoBehaviour
{
    [Header("Activity Systems")]
    [SerializeField] private GameObject toolIdentificationSystem;
    [SerializeField] private GameObject hardwareIdentificationSystem;
    [SerializeField] private GameObject assemblySystem;
    [SerializeField] private GameObject disassemblySystem;

    [Header("Activity Managers")]
    [SerializeField] private ARAssemblyManager assemblyManager;

    public void RouteActivity(ARActivityData activityData)
    {
        if (activityData == null)
        {
            Debug.LogError(
                "ARActivityRouter: No activity data provided."
            );

            return;
        }

        DisableAllSystems();

        switch (activityData.activityType)
        {
            case ARActivityType.ToolIdentification:

                if (toolIdentificationSystem != null)
                    toolIdentificationSystem.SetActive(true);

                Debug.Log(
                    "ARActivityRouter: Tool Identification selected."
                );

                break;

            case ARActivityType.HardwareIdentification:

                if (hardwareIdentificationSystem != null)
                    hardwareIdentificationSystem.SetActive(true);

                Debug.Log(
                    "ARActivityRouter: Hardware Identification selected."
                );

                break;

            case ARActivityType.Assembly:

                if (activityData.assemblyActivity == null)
                {
                    Debug.LogError(
                        "ARActivityRouter: Assembly activity has no " +
                        "ARAssemblyActivityData assigned."
                    );

                    break;
                }

                if (assemblyManager != null)
                {
                    assemblyManager.SetActivity(
                        activityData.assemblyActivity
                    );
                }

                if (assemblySystem != null)
                {
                    assemblySystem.SetActive(true);
                }

                Debug.Log(
                    "ARActivityRouter: Assembly selected."
                );

                break;

            case ARActivityType.Disassembly:

                if (disassemblySystem != null)
                    disassemblySystem.SetActive(true);

                Debug.Log(
                    "ARActivityRouter: Disassembly selected."
                );

                break;

            default:

                Debug.LogWarning(
                    "ARActivityRouter: Unknown activity type."
                );

                break;
        }
    }

    private void DisableAllSystems()
    {
        if (toolIdentificationSystem != null)
            toolIdentificationSystem.SetActive(false);

        if (hardwareIdentificationSystem != null)
            hardwareIdentificationSystem.SetActive(false);

        if (assemblySystem != null)
            assemblySystem.SetActive(false);

        if (disassemblySystem != null)
            disassemblySystem.SetActive(false);
    }
}