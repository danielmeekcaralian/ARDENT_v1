using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SandboxInventoryPanel : MonoBehaviour
{
    public ScrollRect categoryScroll;
    public GameObject categoryRowPrefab, itemTilePrefab;
    public Button closeButton;
    public TMP_Text countText;
    public static bool IsOpen => active != null && active.gameObject.activeInHierarchy;
    private static SandboxInventoryPanel active;
    private readonly List<GameObject> rows = new List<GameObject>();
    private static readonly string[] Categories = { "PC Components", "Peripherals", "Tools", "Safety Equipment" };

    private void OnEnable()
    {
        active = this;
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }
    private void OnDisable()
    {
        if (active == this) active = null;
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
    }
    public void Close() { gameObject.SetActive(false); }

    public bool Populate(ARObjectData[] available, Action<ARObjectData> select)
    {
        if (categoryScroll == null || categoryScroll.content == null || categoryRowPrefab == null || itemTilePrefab == null) return false;
        foreach (var row in rows) if (row != null) { row.SetActive(false); Destroy(row); }
        rows.Clear();
        var items = new List<ARObjectData>(); var ids = new HashSet<string>();
        if (available != null) foreach (var item in available)
            if (item?.prefab != null && HardwareLibraryProgress.IsUnlocked(item) && ids.Add(item.LibraryItemId)) items.Add(item);
        items.Sort((a,b) => string.Compare(Name(a),Name(b),StringComparison.OrdinalIgnoreCase));
        if (countText != null) countText.text = $"{items.Count} unlocked items";
        for (int category = 0; category < Categories.Length; category++)
        {
            var group = items.FindAll(item => (int)ARSandboxSession.CategoryFor(item) == category);
            if (group.Count == 0) continue;
            var row = Instantiate(categoryRowPrefab,categoryScroll.content); row.SetActive(true); rows.Add(row);
            var title = HardwareLibraryUI.FindNamed<TMP_Text>(row.transform,"CategoryTitleText");
            if (title != null) title.text = Categories[category];
            var scroll = HardwareLibraryUI.FindNamed<ScrollRect>(row.transform,"ItemScrollView");
            if (scroll == null || scroll.content == null || scroll.viewport == null) { Debug.LogError("Sandbox category prefab needs a configured ItemScrollView.",row); continue; }
            // Fill the wider landscape panel without modifying the shared prefab.
            var rect = (RectTransform)scroll.transform;
            rect.anchorMin = new Vector2(0,rect.anchorMin.y); rect.anchorMax = new Vector2(1,rect.anchorMax.y);
            rect.sizeDelta = new Vector2(-40,rect.sizeDelta.y); rect.anchoredPosition = new Vector2(0,rect.anchoredPosition.y);
            foreach (Transform child in scroll.content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            var drag = scroll.viewport.GetComponent<LibraryRowDrag>();
            if (drag == null) drag = scroll.viewport.gameObject.AddComponent<LibraryRowDrag>();
            drag.horizontal = scroll; drag.vertical = categoryScroll;
            foreach (var item in group)
            {
                var tile = Instantiate(itemTilePrefab,scroll.content); tile.SetActive(true);
                var icon = HardwareLibraryUI.FindNamed<Image>(tile.transform,"ItemIcon");
                if (icon != null) { icon.sprite = item.inventoryImage; icon.color = Color.white; icon.preserveAspect = true; }
                var name = HardwareLibraryUI.FindNamed<TMP_Text>(tile.transform,"ItemNameText");
                if (name != null) name.text = Name(item);
                var overlay = HardwareLibraryUI.FindNamed<Transform>(tile.transform,"LockOverlay");
                if (overlay != null) overlay.gameObject.SetActive(false);
                foreach (var graphic in tile.GetComponentsInChildren<Graphic>(true))
                    if (graphic.gameObject != tile) graphic.raycastTarget = false;
                var button = tile.GetComponent<Button>();
                if (button != null) { var selected = item; button.onClick.AddListener(() => { Close(); select?.Invoke(selected); }); }
            }
        }
        return true;
    }
    private static string Name(ARObjectData item)
    {
        return HardwareProfileCatalog.DisplayName(item?.prefab);
    }
}
