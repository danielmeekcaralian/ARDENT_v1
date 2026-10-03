using UnityEngine;
using UnityEngine.UI;

// Keep the authored layout and tint all child graphics, including TMP labels.
[DisallowMultipleComponent]
public sealed class ARButtonAvailability : MonoBehaviour
{
    private Graphic[] graphics;
    private Color[] originalColors;

    public static void Set(Button button, bool available)
    {
        if (button == null) return;
        var view = button.GetComponent<ARButtonAvailability>();
        if (view == null) view = button.gameObject.AddComponent<ARButtonAvailability>();
        if (view.graphics == null)
        {
            view.graphics = button.GetComponentsInChildren<Graphic>(true);
            view.originalColors = new Color[view.graphics.Length];
            for (int i = 0; i < view.graphics.Length; i++)
                view.originalColors[i] = view.graphics[i].color;
        }
        button.gameObject.SetActive(true);
        button.interactable = available;
        for (int i = 0; i < view.graphics.Length; i++)
        {
            if (view.graphics[i] == null) continue;
            Color color = view.originalColors[i];
            if (!available)
            {
                float gray = Mathf.Max(0.35f, color.grayscale);
                color = new Color(gray, gray, gray, color.a * 0.45f);
            }
            view.graphics[i].color = color;
        }
    }
}
