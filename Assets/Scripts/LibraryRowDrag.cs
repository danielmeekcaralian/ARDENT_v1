using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Put on the inner viewport: choose one scroll direction for the entire gesture.
public class LibraryRowDrag : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ScrollRect horizontal, vertical;
    private ScrollRect active;
    private int pointer;
    public void OnInitializePotentialDrag(PointerEventData e)
    {
        if (horizontal != null) horizontal.OnInitializePotentialDrag(e);
        if (vertical != null) vertical.OnInitializePotentialDrag(e);
    }
    public void OnBeginDrag(PointerEventData e)
    {
        if (active != null || e.button != PointerEventData.InputButton.Left) return;
        Vector2 distance = e.position - e.pressPosition;
        active = Mathf.Abs(distance.x) > Mathf.Abs(distance.y) ? horizontal : vertical;
        pointer = e.pointerId;
        if (active != null) active.OnBeginDrag(e);
    }
    public void OnDrag(PointerEventData e) { if (active != null && e.pointerId == pointer) active.OnDrag(e); }
    public void OnEndDrag(PointerEventData e)
    {
        if (active == null || e.pointerId != pointer) return;
        active.OnEndDrag(e); active = null;
    }
    private void OnDisable()
    {
        if (active != null && EventSystem.current != null)
            active.OnEndDrag(new PointerEventData(EventSystem.current) { pointerId = pointer });
        active = null;
    }
}
