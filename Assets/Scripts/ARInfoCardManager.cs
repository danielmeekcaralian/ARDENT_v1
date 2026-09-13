using TMPro;
using UnityEngine;

public class ARInfoCardManager : MonoBehaviour
{
    [Header("Info Card")]
    [SerializeField] private GameObject infoCard;
    [SerializeField] private TMP_Text objectNameText;
    [SerializeField] private TMP_Text informationText;

    [Header("Position")]
    [SerializeField] private Vector3 cardOffset = new Vector3(0f, 0.5f, 0f);

    private ARObjectInfo currentObject;

    private void Start()
    {
        if (infoCard != null)
            infoCard.SetActive(false);
    }

    private void LateUpdate()
    {
        if (currentObject == null || infoCard == null)
            return;

        Vector3 targetPosition =
            currentObject.transform.position + cardOffset;

        infoCard.transform.position =
            targetPosition;
    }

    public void ShowInfo(ARObjectInfo objectInfo)
    {
        if (objectInfo == null)
            return;

        currentObject = objectInfo;

        if (objectNameText != null)
            objectNameText.text = objectInfo.objectName;

        if (informationText != null)
            informationText.text = objectInfo.information;

        infoCard.SetActive(true);
    }

    public void HideInfo()
    {
        currentObject = null;

        if (infoCard != null)
            infoCard.SetActive(false);
    }
}