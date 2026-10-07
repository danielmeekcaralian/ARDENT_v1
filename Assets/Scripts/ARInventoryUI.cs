using UnityEngine;
using UnityEngine.UI;

public class ARInventoryUI : MonoBehaviour
{
    [Header("Inventory")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform toolContainer;
    [SerializeField] private GameObject toolButtonPrefab;

    [Header("Placement")]
    [SerializeField] private ARPlacementManager placementManager;

    [Header("Sandbox Inventory (optional scene panel)")]
    [SerializeField] private SandboxInventoryPanel sandboxInventory;
    private bool useSandboxPanel;
    private GameObject VisibleInventory => useSandboxPanel && sandboxInventory != null ? sandboxInventory.gameObject : inventoryPanel;
    private ARActivityData currentActivity;

    public void PopulateInventory()
    {
        useSandboxPanel = false;
        if (currentActivity != null && currentActivity.activityType == ARActivityType.Sandbox && sandboxInventory != null)
        {
            useSandboxPanel = sandboxInventory.Populate(currentActivity.availableObjects, SelectObject);
            if (useSandboxPanel) return;
            Debug.LogWarning("Sandbox inventory is incomplete; using the existing inventory.", this);
        }
        if (currentActivity == null)
        {
            Debug.LogWarning("No AR Activity assigned.");
            return;
        }

        if (toolContainer == null)
        {
            Debug.LogError("Tool Container is not assigned.");
            return;
        }

        if (toolButtonPrefab == null)
        {
            Debug.LogError("Tool Button Prefab is not assigned.");
            return;
        }

        if (currentActivity.availableObjects == null)
        {
            Debug.LogWarning(
                "ARInventoryUI: Activity has no available objects."
            );

            return;
        }

        // Remove existing buttons
        foreach (Transform child in toolContainer)
        {
            Destroy(child.gameObject);
        }

        // Create buttons
        foreach (ARObjectData objectData in currentActivity.availableObjects)
        {
            if (objectData == null || objectData.prefab == null)
                continue;

            GameObject buttonObject =
                Instantiate(
                    toolButtonPrefab,
                    toolContainer
                );

            Image buttonImage =
                buttonObject.GetComponent<Image>();

            if (buttonImage != null)
            {
                buttonImage.sprite =
                    objectData.inventoryImage;

                buttonImage.preserveAspect = true;
            }

            Button button =
                buttonObject.GetComponent<Button>();

            if (button != null)
            {
                ARObjectData selectedData = objectData;

                button.onClick.AddListener(() =>
                {
                    SelectObject(selectedData);
                });
            }
        }
    }

    private void SelectObject(ARObjectData objectData)
    {
        if (placementManager == null)
            return;

        placementManager.SelectObject(objectData);

        ArdentMotion.SetPanelVisible(VisibleInventory, false);
    }

    public void OpenInventory()
    {
        ArdentMotion.SetPanelVisible(VisibleInventory, true);
    }

    public void ToggleInventory()
    {
        if (VisibleInventory == null)
            return;
        if (placementManager != null && !placementManager.CanPlaceObjects)
        {
            ArdentMotion.SetPanelVisible(VisibleInventory, false);
            return;
        }

        ArdentMotion.SetPanelVisible(VisibleInventory, !VisibleInventory.activeSelf);
    }

    public void SetActivity(ARActivityData activity)
    {
        currentActivity = activity;
        if (sandboxInventory == null)
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                sandboxInventory = HardwareLibraryUI.FindNamed<SandboxInventoryPanel>(root.transform, "SandboxInventoryPanel");
                if (sandboxInventory != null) break;
            }
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (sandboxInventory != null) sandboxInventory.Close();
        PopulateInventory();
    }
}
