using TMPro;
using UnityEngine;

// Rebuild SDF text once the screen-space Canvas has settled after resizing.
[RequireComponent(typeof(Canvas))]
[DisallowMultipleComponent]
public sealed class ARCanvasTextRefresh : MonoBehaviour
{
    private Canvas targetCanvas;
    private RectTransform canvasRect;
    private Vector2 previousSize;
    private float previousScale;
    private int previousWidth, previousHeight;
    private int stableFramesRemaining;

    private void Awake()
    {
        targetCanvas = GetComponent<Canvas>();
        canvasRect = GetComponent<RectTransform>();
    }

    private void OnEnable() { stableFramesRemaining = 2; }

    private void LateUpdate()
    {
        if (targetCanvas == null || !targetCanvas.isActiveAndEnabled ||
            targetCanvas.renderMode == RenderMode.WorldSpace) return;

        var size = canvasRect.rect.size;
        var scale = targetCanvas.scaleFactor;
        if (size != previousSize || !Mathf.Approximately(scale, previousScale) ||
            Screen.width != previousWidth || Screen.height != previousHeight)
        {
            previousSize = size;
            previousScale = scale;
            previousWidth = Screen.width;
            previousHeight = Screen.height;
            stableFramesRemaining = 2;
            return;
        }

        if (stableFramesRemaining == 0 || --stableFramesRemaining != 0) return;
        Canvas.ForceUpdateCanvases();
        foreach (var label in GetComponentsInChildren<TextMeshProUGUI>())
            if (label.isActiveAndEnabled) label.ForceMeshUpdate();
    }
}
