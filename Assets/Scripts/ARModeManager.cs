using UnityEngine;

public class ARModeManager : MonoBehaviour
{
    [Header("Current Mode")]
    [SerializeField]
    private ARInteractionMode currentMode =
        ARInteractionMode.Edit;

    public ARInteractionMode CurrentMode => currentMode;

    public void SetPlaceMode()
    {
        currentMode = ARInteractionMode.Place;

        Debug.Log("AR Mode: PLACE");
    }

    public void SetEditMode()
    {
        currentMode = ARInteractionMode.Edit;

        Debug.Log("AR Mode: EDIT");
    }

    public void SetDeleteMode()
    {
        currentMode = ARInteractionMode.Delete;

        Debug.Log("AR Mode: DELETE");
    }
}