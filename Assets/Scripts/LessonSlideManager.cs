using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LessonSlideManager : MonoBehaviour
{
    [Header("Slide Prefabs")]
    [SerializeField] private GameObject learningObjectivesSlidePrefab;
    [SerializeField] private GameObject contentSlidePrefab;
    [SerializeField] private GameObject imageContentSlidePrefab;

    [Header("Slide Container")]
    [SerializeField] private Transform slideContainer;

    private GameObject[] slides;
    private int currentSlide = 0;

    private void Start()
    {
        LessonData lesson = LessonSession.CurrentLesson;

        if (lesson == null)
        {
            Debug.LogError("No lesson selected!");
            return;
        }

        CreateSlides(lesson);
        ShowSlide();
    }

    private void CreateSlides(LessonData lesson)
    {
        // +1 because the Learning Objectives slide
        // is automatically added as the first slide.
        int totalSlides = 1 + lesson.slides.Length;

        slides = new GameObject[totalSlides];

        // ==========================================
        // LEARNING OBJECTIVES SLIDE
        // ==========================================

        GameObject objectivesSlide = Instantiate(
            learningObjectivesSlidePrefab,
            slideContainer
        );

        slides[0] = objectivesSlide;

        SetupNavigationButtons(objectivesSlide);

        // ==========================================
        // REGULAR LESSON SLIDES
        // ==========================================

        for (int i = 0; i < lesson.slides.Length; i++)
        {
            LessonSlideData slideData = lesson.slides[i];

            GameObject prefab = null;

            switch (slideData.slideType)
            {
                case SlideType.Content:
                    prefab = contentSlidePrefab;
                    break;

                case SlideType.ImageContent:
                    prefab = imageContentSlidePrefab;
                    break;

                default:
                    Debug.LogError(
                        $"Invalid slide type at index {i}."
                    );
                    continue;
            }

            if (prefab == null)
            {
                Debug.LogError(
                    $"No prefab assigned for slide type: " +
                    $"{slideData.slideType}"
                );

                continue;
            }

            GameObject slide = Instantiate(
                prefab,
                slideContainer
            );

            // +1 because slide 0 is Learning Objectives
            slides[i + 1] = slide;

            SetupSlide(slide, slideData);

            SetupNavigationButtons(slide);
        }
    }

    private void SetupSlide(
        GameObject slide,
        LessonSlideData data)
    {
        // ==========================================
        // CONTENT
        // ==========================================

        // Get all TMP text components in the slide.
        TMP_Text[] texts =
            slide.GetComponentsInChildren<TMP_Text>(true);

        // The first TMP_Text is assumed to be the
        // content text of the slide.
        if (texts.Length > 0)
        {
            texts[0].text = data.content;
        }

        // ==========================================
        // IMAGE
        // ==========================================

        if (data.slideType == SlideType.ImageContent)
        {
            ImageContentSlide imageSlide =
                slide.GetComponent<ImageContentSlide>();

            if (imageSlide != null)
            {
                imageSlide.Setup(data);
            }
            else
            {
                Debug.LogError(
                    "ImageContentSlide component is missing " +
                    "from the ImageContentSlide prefab!"
                );
            }
        }
    }

    private void SetupNavigationButtons(GameObject slide)
    {
        Button[] buttons =
            slide.GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            string buttonName =
                button.gameObject.name.ToLower();

            // NEXT BUTTON
            if (buttonName.Contains("next"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(NextSlide);
            }

            // BACK BUTTON
            else if (buttonName.Contains("back"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(PreviousSlide);
            }
        }
    }

    public void NextSlide()
    {
        if (slides == null || slides.Length == 0)
        {
            Debug.LogWarning("No slides available.");
            return;
        }

        if (currentSlide < slides.Length - 1)
        {
            currentSlide++;

            ShowSlide();

            Debug.Log(
                $"Next slide: {currentSlide + 1}/{slides.Length}"
            );
        }
        else
        {
            Debug.Log("Already on the last slide.");
        }
    }

    public void PreviousSlide()
    {
        if (slides == null || slides.Length == 0)
        {
            Debug.LogWarning("No slides available.");
            return;
        }

        if (currentSlide > 0)
        {
            currentSlide--;

            ShowSlide();

            Debug.Log(
                $"Previous slide: {currentSlide + 1}/{slides.Length}"
            );
        }
        else
        {
            Debug.Log("Already on the first slide.");
        }
    }

    private void ShowSlide()
    {
        if (slides == null)
            return;

        for (int i = 0; i < slides.Length; i++)
        {
            if (slides[i] != null)
            {
                slides[i].SetActive(i == currentSlide);
            }
        }
    }
}