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

    private ARActivityData currentActivity;

    public void PopulateInventory()
    {
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

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    public void ToggleInventory()
    {
        if (inventoryPanel == null)
            return;

        inventoryPanel.SetActive(
            !inventoryPanel.activeSelf
        );
    }

    public void SetActivity(ARActivityData activity)
    {
        currentActivity = activity;

        PopulateInventory();
    }
}