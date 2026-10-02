using UnityEngine;
using UnityEngine.EventSystems;

// Added automatically to the generated buttons; no scene wiring is required.
public class ARPrecisionHoldButton : MonoBehaviour, IPointerDownHandler,
    IPointerUpHandler, IPointerExitHandler
{
    private ARInteractionManager owner;
    private ARInteractionManager.PrecisionAction action;
    private float direction;
    private int pointerId;
    private bool pressed;

    public void Configure(ARInteractionManager manager,
        ARInteractionManager.PrecisionAction adjustment, float sign)
    {
        owner = manager;
        action = adjustment;
        direction = sign;
    }

    public void OnPointerDown(PointerEventData data)
    {
        if (pressed || data.button != PointerEventData.InputButton.Left || owner == null)
            return;
        if (!owner.BeginPrecisionAdjustment(action, direction)) return;
        pressed = true;
        pointerId = data.pointerId;
    }

    public void OnPointerUp(PointerEventData data)
    {
        if (!pressed || data.pointerId != pointerId) return;
        pressed = false;
        if (owner != null) owner.EndPrecisionAdjustment();
    }

    public void OnPointerExit(PointerEventData data)
    {
        if (!pressed || data.pointerId != pointerId) return;
        Cancel();
    }

    private void OnDisable() { Cancel(); }

    private void Cancel()
    {
        if (!pressed) return;
        pressed = false;
        if (owner != null) owner.CancelPrecisionAdjustment();
    }
}
