using UnityEngine;

[DisallowMultipleComponent]
public sealed class NetworkCableTarget : MonoBehaviour
{
    public NetworkCableKind kind;
    public Transform snapPoint;
    public Renderer indicator;

    public void SetState(bool current, bool complete)
    {
        if (indicator == null) return;
        Color color = complete
            ? new Color(0.18f, 0.82f, 0.32f)
            : current ? new Color(1f, 0.68f, 0.08f) : new Color(0.22f, 0.27f, 0.34f);
        indicator.material.color = color;
        if (indicator.material.HasProperty("_EmissionColor"))
        {
            indicator.material.EnableKeyword("_EMISSION");
            indicator.material.SetColor("_EmissionColor", current ? color * 1.7f : color * 0.15f);
        }
    }
}
