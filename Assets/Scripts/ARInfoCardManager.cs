using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Position after the card's default-order camera-facing rotation.
[DefaultExecutionOrder(100)]
public class ARInfoCardManager : MonoBehaviour
{
    [Header("Info Card")]
    [SerializeField] private GameObject infoCard;
    [SerializeField] private TMP_Text objectNameText;
    [SerializeField] private TMP_Text informationText;

    // Card visibility and spacing are shared preferences in Settings.

    private ARObjectInfo currentObject;
    private readonly List<Renderer> modelRenderers = new List<Renderer>();
    private readonly Vector3[] cardCorners = new Vector3[4];

    private void Start()
    {
        if (infoCard != null && currentObject == null)
            infoCard.SetActive(false);
    }

    private void LateUpdate()
    {
        if (infoCard == null) return;
        if (currentObject == null || !currentObject.gameObject.activeInHierarchy)
        {
            HideInfo();
            return;
        }

        infoCard.SetActive(ArdentSettings.ShowCards);
        if (ArdentSettings.ShowCards) PositionAboveModel();
    }

    private void PositionAboveModel()
    {
        // Refresh the list so assembled/removed parts and enabled renderers are reflected.
        currentObject.GetComponentsInChildren<Renderer>(false, modelRenderers);
        Bounds bounds = new Bounds(currentObject.transform.position, Vector3.zero);
        bool foundMesh = false;
        foreach (Renderer mesh in modelRenderers)
        {
            if (mesh == null || !mesh.enabled ||
                (!(mesh is MeshRenderer) && !(mesh is SkinnedMeshRenderer)) ||
                mesh.transform.IsChildOf(infoCard.transform) ||
                mesh.GetComponentInParent<Canvas>() != null)
                continue;

            if (!foundMesh) { bounds = mesh.bounds; foundMesh = true; }
            else bounds.Encapsulate(mesh.bounds);
        }

        Vector3 anchor = new Vector3(bounds.center.x,
            bounds.max.y + ArdentSettings.CardGap, bounds.center.z);
        infoCard.transform.position = anchor;

        // A centered pivot would put half the card into the model. Account for
        // the actual world-space rectangle, including camera-facing tilt and scale.
        RectTransform cardRect = infoCard.transform as RectTransform;
        if (cardRect != null)
        {
            cardRect.GetWorldCorners(cardCorners);
            float bottom = cardCorners[0].y;
            for (int i = 1; i < cardCorners.Length; i++)
                bottom = Mathf.Min(bottom, cardCorners[i].y);
            infoCard.transform.position += Vector3.up * (anchor.y - bottom);
        }
    }

    public void ShowInfo(ARObjectInfo objectInfo)
    {
        if (objectInfo == null || infoCard == null) return;
        currentObject = objectInfo;
        if (objectNameText != null) objectNameText.text = objectInfo.objectName;
        if (informationText != null) informationText.text = objectInfo.information;
        infoCard.SetActive(ArdentSettings.ShowCards);
        Canvas.ForceUpdateCanvases();
        PositionAboveModel();
    }

    public void HideInfo()
    {
        currentObject = null;
        modelRenderers.Clear();
        if (infoCard != null) infoCard.SetActive(false);
    }
}
