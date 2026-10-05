using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class RJ45WireArrangement : MonoBehaviour
{
    [Header("Board (local XY plane)")]
    public Camera interactionCamera;
    public RJ45DraggableWire[] wires;
    public Transform[] pinSlots;
    public TMP_Text[] pinLabels;
    public float snapRadius = 0.19f;
    [Header("Your scene UI")]
    public TMP_Text instructionsText;
    public TMP_Text standardText;
    public Button standardAButton;
    public Button standardBButton;
    public Button restartButton;
    [Header("Prototype only")]
    public bool fitOrthographicCamera = true;
    [Header("Raised once each time the arrangement becomes correct")]
    public UnityEvent onOrderCorrect = new UnityEvent();
    [HideInInspector] public bool inputEnabled = true;
    public bool IsOrderCorrect => order != null && order.IsCorrect;

    private RJ45WireOrder order;
    private RJ45DraggableWire dragged;
    private Vector3 dragOffset;
    private int touchId = -1;
    private bool suppressTouch;
    private bool lastCorrect;
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    public RJ45DraggableWire WireAtPin(int pin)
    {
        if (order == null || pin < 0 || pin >= 8) return null;
        int identity = order.WireAt(pin);
        foreach (var wire in wires) if ((int)wire.identity == identity) return wire;
        return null;
    }
    public int[] CaptureWireSlots()
    {
        if (order == null) return null;
        var result = new int[8];
        for (int i=0;i<8;i++) result[i]=order.WireAt(i);
        return result;
    }
    public void RestoreWireSlots(RJ45WiringStandard standard, int[] slots)
    {
        order = new RJ45WireOrder(standard);
        for (int i=0;i<8;i++) if(slots[i]>=0) order.Place((RJ45WireColor)slots[i],i);
        dragged = null;
        // The session explicitly restores the next stage; don't fire completion during reconstruction.
        lastCorrect = order.IsCorrect;
        Refresh();
    }
    private void Awake()
    {
        if (interactionCamera == null) interactionCamera = Camera.main;
        if (!ValidateBoard()) { enabled = false; return; }
        foreach (var wire in wires) wire.homePosition = wire.transform.localPosition;
        order = new RJ45WireOrder();
        Refresh();
    }
    private bool ValidateBoard()
    {
        bool valid = wires != null && wires.Length == 8 && pinSlots != null && pinSlots.Length == 8 && interactionCamera != null;
        var identities = new HashSet<RJ45WireColor>();
        if (valid) for (int i = 0; i < 8; i++)
            valid &= wires[i] != null && wires[i].pickCollider != null && wires[i].transform.parent == transform &&
                (int)wires[i].identity >= 0 && (int)wires[i].identity < 8 && identities.Add(wires[i].identity) && pinSlots[i] != null;
        if (!valid) Debug.LogError("RJ45 board needs a camera, eight unique wire identities with colliders directly under the board, and eight pin slots.", this);
        return valid;
    }
    private void OnEnable()
    {
        if (standardAButton != null) standardAButton.onClick.AddListener(SelectA);
        if (standardBButton != null) standardBButton.onClick.AddListener(SelectB);
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
    }
    private void OnDisable()
    {
        if (standardAButton != null) standardAButton.onClick.RemoveListener(SelectA);
        if (standardBButton != null) standardBButton.onClick.RemoveListener(SelectB);
        if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
        CancelDrag(); touchId = -1; suppressTouch = false;
    }
    private void OnApplicationFocus(bool focused) { if (!focused) { CancelDrag(); touchId = -1; suppressTouch = true; } }
    public void SelectA() => SelectStandard(RJ45WiringStandard.T568A);
    public void SelectB() => SelectStandard(RJ45WiringStandard.T568B);
    private void SelectStandard(RJ45WiringStandard standard)
    {
        if (order == null || order.Standard == standard) return;
        CancelDrag(); order.Reset(standard); lastCorrect = false; Refresh();
    }
    public void Restart()
    {
        if (order == null) return;
        CancelDrag(); order.Reset(order.Standard); lastCorrect = false; Refresh();
    }
    private void Update()
    {
        if (!inputEnabled || ARCheckpointSession.BlocksInput || UIManager.HasOpenPanel || SandboxInventoryPanel.IsOpen) { CancelDrag(); return; }
        if (order == null || interactionCamera == null) return;
        var screen = Touchscreen.current;
        int pressed = 0;
        if (screen != null) foreach (var touch in screen.touches) if (touch.press.isPressed) pressed++;
        if (pressed > 1) { CancelDrag(); touchId = -1; suppressTouch = true; return; }
        if (suppressTouch) { if (pressed == 0) suppressTouch = false; return; }
        if (touchId >= 0 && screen != null)
        {
            foreach (var touch in screen.touches)
                if (touch.touchId.ReadValue() == touchId)
                {
                    var pos = touch.position.ReadValue();
                    if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled) CancelDrag();
                    else if (touch.press.wasReleasedThisFrame) EndDrag(pos);
                    else if (touch.press.isPressed) { MoveDrag(pos); return; }
                    else CancelDrag();
                    touchId = -1; return;
                }
            CancelDrag(); touchId = -1; return;
        }
        if (pressed > 0)
        {
            foreach (var touch in screen.touches) if (touch.press.wasPressedThisFrame)
            { CancelDrag(); touchId = touch.touchId.ReadValue(); BeginDrag(touch.position.ReadValue()); break; }
            return;
        }
        var mouse = Mouse.current;
        if (mouse == null) return;
        var position = mouse.position.ReadValue();
        if (mouse.leftButton.wasPressedThisFrame) BeginDrag(position);
        if (mouse.leftButton.isPressed) MoveDrag(position);
        if (mouse.leftButton.wasReleasedThisFrame) EndDrag(position);
    }
    private void LateUpdate()
    {
        if (fitOrthographicCamera && interactionCamera != null && interactionCamera.orthographic)
            interactionCamera.orthographicSize = Mathf.Max(2.1f, 1.95f / Mathf.Max(0.1f, interactionCamera.aspect));
    }
    private bool OverUI(Vector2 position)
    {
        if (EventSystem.current == null) return false;
        uiHits.Clear(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, uiHits);
        foreach (var hit in uiHits) if (hit.module is GraphicRaycaster) return true;
        return false;
    }
    private bool BoardPoint(Vector2 position, out Vector3 local)
    {
        var ray = interactionCamera.ScreenPointToRay(position);
        var plane = new Plane(transform.forward, transform.position);
        if (plane.Raycast(ray, out float distance)) { local = transform.InverseTransformPoint(ray.GetPoint(distance)); local.z = 0; return true; }
        local = default; return false;
    }
    private void BeginDrag(Vector2 position)
    {
        if (OverUI(position) || !BoardPoint(position, out var point)) return;
        var ray = interactionCamera.ScreenPointToRay(position);
        float nearest = float.PositiveInfinity;
        foreach (var wire in wires) if (wire.pickCollider.Raycast(ray, out var hit, float.MaxValue) && hit.distance < nearest)
        { dragged = wire; nearest = hit.distance; }
        if (dragged != null) { dragOffset = dragged.transform.localPosition - point; dragOffset.z = 0; }
    }
    private void MoveDrag(Vector2 position)
    {
        if (dragged == null || !BoardPoint(position, out var point)) return;
        point += dragOffset; point.z = -0.08f;
        if (dragged.bendingTube != null) point = dragged.bendingTube.ConstrainTip(point);
        dragged.transform.localPosition = point;
    }
    private void EndDrag(Vector2 position)
    {
        if (dragged == null) return;
        MoveDrag(position);
        if (!OverUI(position))
        {
            Vector3 local = dragged.transform.localPosition;
            int best = -1; float distance = snapRadius;
            for (int i = 0; i < 8; i++)
            {
                var slot = transform.InverseTransformPoint(pinSlots[i].position);
                float candidate = Vector2.Distance(new Vector2(local.x, local.y), new Vector2(slot.x, slot.y));
                if (candidate < distance) { best = i; distance = candidate; }
            }
            if (best >= 0) order.Place(dragged.identity, best);
            else if (local.y < -0.35f) order.Remove(dragged.identity);
        }
        dragged = null; Refresh();
    }
    private void CancelDrag() { if (dragged == null) return; dragged = null; if (order != null) Refresh(); }
    private void Refresh()
    {
        foreach (var wire in wires)
        {
            int pin = order.PinOf(wire.identity);
            wire.transform.localPosition = pin < 0 ? wire.homePosition : transform.InverseTransformPoint(pinSlots[pin].position);
        }
        if (standardText != null) standardText.text = "RJ45 WIRE ARRANGEMENT | " + order.Standard;
        if (standardAButton != null) standardAButton.interactable = order.Standard != RJ45WiringStandard.T568A;
        if (standardBButton != null) standardBButton.interactable = order.Standard != RJ45WiringStandard.T568B;
        var wrong = new StringBuilder();
        for (int i = 0; i < 8; i++)
        {
            bool bad = order.IsFull && !order.IsPinCorrect(i);
            if (bad) { if (wrong.Length > 0) wrong.Append(", "); wrong.Append(i + 1); }
            if (pinLabels != null && i < pinLabels.Length && pinLabels[i] != null)
            {
                pinLabels[i].text = "PIN " + (i + 1) + (bad ? "\nFIX" : order.IsFull ? "\nOK" : "");
                pinLabels[i].color = bad ? new Color(1f, .4f, .35f) : order.IsFull ? new Color(.3f, 1f, .65f) : Color.white;
            }
        }
        if (instructionsText != null) instructionsText.text = !order.IsFull
            ? "Drag each wire into a numbered slot. " + order.Count + "/8 placed.\nAll eight placed = automatic check. Changing standard resets the board."
            : order.IsCorrect ? "Correct " + order.Standard + " arrangement!\nPrototype step passed. You can restart or try the other standard."
            : "Check pins " + wrong + ".\nDrag onto an occupied slot to swap. Your changes are checked automatically.";
        bool nowCorrect = order.IsCorrect;
        bool notify = nowCorrect && !lastCorrect;
        lastCorrect = nowCorrect;
        if (notify) onOrderCorrect.Invoke();
        ARCheckpointSession.SaveCurrent();
    }
}




