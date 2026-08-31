using UnityEngine;
using UnityEngine.UI;

public class LessonSlideUI : MonoBehaviour
{
    [SerializeField] private Button nextButton;
    [SerializeField] private Button backButton;

    private void Start()
    {
        LessonSlideManager manager = FindFirstObjectByType<LessonSlideManager>();

        if (manager == null)
        {
            Debug.LogError("LessonSlideManager not found!");
            return;
        }

        nextButton.onClick.RemoveAllListeners();
        backButton.onClick.RemoveAllListeners();

        nextButton.onClick.AddListener(manager.NextSlide);
        backButton.onClick.AddListener(manager.PreviousSlide);
    }
}