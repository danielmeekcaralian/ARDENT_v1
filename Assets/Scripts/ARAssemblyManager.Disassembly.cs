using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class ARAssemblyManager
{
    public enum ActivityPhase { Idle, Assembly, ReadyForDisassembly, Disassembly, Complete }
    public ActivityPhase Phase { get; private set; }
    public bool CanPlaceObjects => Phase == ActivityPhase.Idle || Phase == ActivityPhase.Assembly;
    public bool RequiresEditMode => Phase == ActivityPhase.ReadyForDisassembly ||
        Phase == ActivityPhase.Disassembly || Phase == ActivityPhase.Complete;

    [Header("Disassembly")]
    [Tooltip("Optional. If empty, a Start Disassembly button is created on the step-title canvas.")]
    [SerializeField] private Button startDisassemblyButton;
    [SerializeField, Min(0.01f)] private float trayPadding = 0.08f;

    private readonly List<GameObject> installedParts = new List<GameObject>();
    private readonly List<Vector3> traySlots = new List<Vector3>();
    private readonly List<Image> trayTiles = new List<Image>();
    private Transform sharedRoot;
    private GameObject tray;
    private int removalIndex;
    private Vector2 slotSize;
    private bool ownsStartButton;
    private static readonly Color WaitingColor = new Color(0.10f, 0.25f, 0.32f, 0.85f);
    private static readonly Color ActiveColor = new Color(0.10f, 0.75f, 0.85f, 0.90f);
    private static readonly Color DoneColor = new Color(0.15f, 0.65f, 0.30f, 0.85f);

    private void ResetCombinedActivity()
    {
        Phase = ActivityPhase.Idle;
        installedParts.Clear();
        traySlots.Clear();
        trayTiles.Clear();
        sharedRoot = null;
        removalIndex = -1;
        if (tray != null) Destroy(tray);
        if (startDisassemblyButton != null)
        {
            startDisassemblyButton.onClick.RemoveListener(BeginDisassembly);
            startDisassemblyButton.gameObject.SetActive(false);
        }
    }

    public bool CanDeleteObject(GameObject candidate)
    {
        if (Phase == ActivityPhase.Idle) return true;
        if (Phase != ActivityPhase.Assembly) return false;
        if (candidate == null) return true;
        foreach (GameObject part in installedParts)
            if (part != null && (part == candidate || part.transform.IsChildOf(candidate.transform)))
                return false;
        return true;
    }

    public void RefreshProgress()
    {
        if (assemblyActivity == null || assemblyActivity.steps == null) return;
        var progress = FindFirstObjectByType<ARActivityProgress>();
        if (progress == null) return;
        int total = assemblyActivity.steps.Length;
        if (Phase == ActivityPhase.Disassembly)
            progress.SetAssemblyProgress("Disassembly", total - 1 - removalIndex, total);
        else if (Phase == ActivityPhase.Complete)
            progress.SetAssemblyProgress(assemblyActivity.includeDisassembly ? "Assembly + Disassembly" : "Assembly",
                total * (assemblyActivity.includeDisassembly ? 2 : 1),
                total * (assemblyActivity.includeDisassembly ? 2 : 1));
        else
            progress.SetAssemblyProgress("Assembly", Mathf.Min(currentStepIndex, total), total);
    }

    private void FinishAssemblyPhase()
    {
        if (!assemblyActivity.includeDisassembly)
        {
            FinishCombinedActivity();
            return;
        }

        Phase = ActivityPhase.ReadyForDisassembly;
        var placement = FindFirstObjectByType<ARPlacementManager>();
        if (placement != null) placement.CancelPlacement();
        if (stepTitleText != null) stepTitleText.text = "Assembly complete";
        if (instructionsText != null)
            instructionsText.text = "Select Start Disassembly to remove the components in reverse order.";
        RefreshProgress();
        EnsureStartButton();
        if (startDisassemblyButton != null)
        {
            startDisassemblyButton.onClick.RemoveListener(BeginDisassembly);
            startDisassemblyButton.onClick.AddListener(BeginDisassembly);
            startDisassemblyButton.gameObject.SetActive(true);
        }
    }

    private void EnsureStartButton()
    {
        if (startDisassemblyButton != null) return;
        Canvas canvas = stepTitleText != null ? stepTitleText.GetComponentInParent<Canvas>() : null;
        if (canvas == null && instructionsText != null)
            canvas = instructionsText.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Assign a Start Disassembly button or put the step text on a Canvas.");
            return;
        }
        canvas = canvas.rootCanvas;
        var buttonObject = new GameObject("StartDisassemblyButton", typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(canvas.transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(340f, 72f);
        rect.anchoredPosition = Vector2.zero;
        buttonObject.GetComponent<Image>().color = new Color(0.04f, 0.36f, 0.44f, 1f);
        startDisassemblyButton = buttonObject.GetComponent<Button>();
        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        var label = labelObject.GetComponent<TextMeshProUGUI>();
        if (stepTitleText != null) label.font = stepTitleText.font;
        label.text = "Start Disassembly";
        label.fontSize = 26f;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        ownsStartButton = true;
    }

    public void BeginDisassembly()
    {
        if (Phase != ActivityPhase.ReadyForDisassembly) return;
        var placement = FindFirstObjectByType<ARPlacementManager>();
        sharedRoot = placement != null ? placement.AssemblyRoot : null;
        if (sharedRoot == null || installedParts.Count != assemblyActivity.steps.Length)
        {
            Debug.LogError("Disassembly requires the placed assembly root and all installed parts.");
            return;
        }
        foreach (GameObject part in installedParts)
        {
            if (part == null || part.GetComponent<ARObjectManipulator>() == null)
            {
                Debug.LogError("An installed part is missing or has no ARObjectManipulator. Restart the activity.");
                return;
            }
        }
        placement.CancelPlacement();
        var interaction = FindFirstObjectByType<ARInteractionManager>();
        if (interaction != null) interaction.DeselectObject();
        var mode = FindFirstObjectByType<ARModeManager>();
        if (mode != null) mode.SetEditMode();

        // Only the base and the current removal part may be manipulated.
        foreach (var manipulator in sharedRoot.GetComponentsInChildren<ARObjectManipulator>())
            manipulator.SetLocked(true);
        AssemblyAnchor baseAnchor = FindAnchor(assemblyActivity.steps[0].anchorID);
        if (baseAnchor != null)
        {
            var baseManipulator = baseAnchor.GetComponent<ARObjectManipulator>();
            if (baseManipulator != null) baseManipulator.SetLocked(false);
        }
        CreateTray();
        Phase = ActivityPhase.Disassembly;
        removalIndex = installedParts.Count - 1;
        if (startDisassemblyButton != null) startDisassemblyButton.gameObject.SetActive(false);
        PrepareRemovalStep();
    }

    private void PrepareRemovalStep()
    {
        GameObject part = installedParts[removalIndex];
        // Keep the part attached until a successful tray drop, so moving the
        // motherboard carries every component that has not yet been removed.
        part.GetComponent<ARObjectManipulator>().SetLocked(false);
        int slotIndex = installedParts.Count - 1 - removalIndex;
        trayTiles[slotIndex].color = ActiveColor;
        string partName = part.GetComponent<ARObjectInfo>()?.objectName ?? part.name;
        if (stepTitleText != null) stepTitleText.text = $"Remove {partName}";
        if (instructionsText != null)
            instructionsText.text = $"Drag {partName} into the highlighted tray space and release it.";
        RefreshProgress();
    }

    private bool TryRemoveCurrentPart(GameObject candidate)
    {
        if (removalIndex < 0 || candidate == null || candidate != installedParts[removalIndex])
            return false;
        int slotIndex = installedParts.Count - 1 - removalIndex;
        Vector3 slot = traySlots[slotIndex];
        // Existing drag controls preserve height: use the part's visual center in XZ,
        // then settle its bottom on the tray after a successful drop.
        Bounds bounds = GetLocalVisualBounds(candidate.transform);
        Vector3 delta = bounds.center - slot;
        if (Mathf.Abs(delta.x) > slotSize.x * 0.5f || Mathf.Abs(delta.z) > slotSize.y * 0.5f)
        {
            if (instructionsText != null)
                instructionsText.text = "Move the selected part over the highlighted tray space, then release.";
            return false;
        }
        Vector3 correction = new Vector3(slot.x - bounds.center.x,
            slot.y - bounds.min.y, slot.z - bounds.center.z);
        // A removed part now belongs to the tray's shared root, not the board.
        // Preserve its world pose and scale when detaching from its anchor.
        candidate.transform.SetParent(sharedRoot, true);
        candidate.transform.position += sharedRoot.TransformVector(correction);
        candidate.GetComponent<ARObjectManipulator>().SetLocked(true);
        trayTiles[slotIndex].color = DoneColor;
        var interaction = FindFirstObjectByType<ARInteractionManager>();
        if (interaction != null) interaction.DeselectObject();
        removalIndex--;
        if (removalIndex < 0) FinishCombinedActivity();
        else PrepareRemovalStep();
        return true;
    }

    private void FinishCombinedActivity()
    {
        if (Phase == ActivityPhase.Complete) return;
        Phase = ActivityPhase.Complete;
        if (stepTitleText != null) stepTitleText.text = "Activity complete";
        if (instructionsText != null)
            instructionsText.text = assemblyActivity.includeDisassembly
                ? "Assembly and disassembly complete. View your completion result."
                : "Assembly complete. View your completion result.";
        RefreshProgress();
        var progress = FindFirstObjectByType<ARActivityProgress>();
        if (progress != null) progress.CompleteActivity();
    }

    private Bounds GetLocalVisualBounds(Transform target)
    {
        Bounds result = new Bounds(sharedRoot.InverseTransformPoint(target.position), Vector3.zero);
        bool found = false;
        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>())
        {
            Bounds world = renderer.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = world.center + Vector3.Scale(world.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 point = sharedRoot.InverseTransformPoint(corner);
                if (!found) { result = new Bounds(point, Vector3.zero); found = true; }
                else result.Encapsulate(point);
            }
        }
        return result;
    }

    private void CreateTray()
    {
        Bounds assemblyBounds = GetLocalVisualBounds(sharedRoot);
        float width = 0.1f, depth = 0.1f;
        foreach (GameObject part in installedParts)
        {
            Bounds partBounds = GetLocalVisualBounds(part.transform);
            // Allow Y rotation without overflowing the slot after the drop.
            float footprint = new Vector2(partBounds.size.x, partBounds.size.z).magnitude;
            width = Mathf.Max(width, footprint);
            depth = Mathf.Max(depth, footprint);
        }
        float padding = Mathf.Max(0.01f, trayPadding);
        slotSize = new Vector2(width + padding * 2, depth + padding * 2);
        int columns = Mathf.Min(2, installedParts.Count);
        int rows = Mathf.CeilToInt(installedParts.Count / (float)columns);
        float firstX = assemblyBounds.max.x + padding * 2 + slotSize.x * 0.5f;
        float firstZ = assemblyBounds.center.z - (rows - 1) * slotSize.y * 0.5f;
        float floor = assemblyBounds.min.y + 0.002f;

        tray = new GameObject("DisassemblyPartsTray");
        tray.transform.SetParent(sharedRoot, false);
        for (int i = 0; i < installedParts.Count; i++)
        {
            Vector3 position = new Vector3(firstX + (i % columns) * slotSize.x,
                floor, firstZ + (i / columns) * slotSize.y);
            traySlots.Add(position);
            var tileObject = new GameObject($"TraySlot_{i + 1}", typeof(RectTransform), typeof(Canvas), typeof(Image));
            var rect = tileObject.GetComponent<RectTransform>();
            rect.SetParent(tray.transform, false);
            rect.localPosition = position;
            rect.localRotation = Quaternion.Euler(90f, 0f, 0f);
            rect.sizeDelta = slotSize - Vector2.one * padding * 0.25f;
            var canvas = tileObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var image = tileObject.GetComponent<Image>();
            image.color = WaitingColor;
            image.raycastTarget = false;
            trayTiles.Add(image);
        }
    }

    private void OnDestroy()
    {
        if (tray != null) Destroy(tray);
        if (startDisassemblyButton != null)
        {
            startDisassemblyButton.onClick.RemoveListener(BeginDisassembly);
            if (ownsStartButton) Destroy(startDisassemblyButton.gameObject);
        }
    }
}
