using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ImageContentSlide : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text contentText;
    [SerializeField] private Image image;

    public void Setup(LessonSlideData data)
    {
        contentText.text = data.content;

        if (data.image != null)
        {
            image.sprite = data.image;
            image.preserveAspect = true;
        }
        else
        {
            image.gameObject.SetActive(false);
        }
    }
}