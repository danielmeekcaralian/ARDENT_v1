using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LessonSlideManager : MonoBehaviour
{
    [Header("Slide Prefabs")]
    [SerializeField] private GameObject learningObjectivesSlidePrefab;
    [SerializeField] private GameObject contentSlidePrefab;
    [SerializeField] private GameObject imageContentSlidePrefab;

    [Header("Slide Container")]
    [SerializeField] private Transform slideContainer;

    [Header("Lesson Progress")]
    [SerializeField] private LessonProgressUI lessonProgressUI;

    [Header("Lesson Complete Popup")]
    [SerializeField] private GameObject lessonCompletePopup;
    [SerializeField] private TMP_Text lessonCompleteTitleText;
    [SerializeField] private Button arActivityButton;
    [SerializeField] private Button quizButton;
    [SerializeField] private Button lessonsMenuButton;

    private LessonData currentLesson;
    private GameObject[] slides;
    private int currentSlide = 0;

    private void Start()
    {
        currentLesson = LessonSession.CurrentLesson;

        if (currentLesson == null)
        {
            Debug.LogError("No lesson selected!");
            return;
        }

        CreateSlides(currentLesson);

        if (lessonProgressUI != null)
        {
            lessonProgressUI.CreateSegments(
                currentLesson.subtopics.Length
            );
        }

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

        TMP_Text[] texts =
            slide.GetComponentsInChildren<TMP_Text>(true);

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

        // Check whether the current slide completes
        // the current subtopic.
        CheckSubtopicCompletion();

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
            ShowLessonCompletePopup();
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

        UpdateCurrentSubtopicUI();
    }

    private void CheckSubtopicCompletion()
    {
        // Learning Objectives is not a subtopic.
        if (currentSlide == 0)
            return;

        int lessonSlideIndex = currentSlide - 1;

        if (lessonSlideIndex < 0 ||
            lessonSlideIndex >= currentLesson.slides.Length)
            return;

        string currentSubtopicID =
            currentLesson.slides[lessonSlideIndex].subtopicID;

        bool isLastSlideOfSubtopic =
            lessonSlideIndex ==
            currentLesson.slides.Length - 1 ||
            currentLesson.slides[lessonSlideIndex + 1].subtopicID
            != currentSubtopicID;

        if (!isLastSlideOfSubtopic)
            return;

        for (int i = 0;
             i < currentLesson.subtopics.Length;
             i++)
        {
            if (currentLesson.subtopics[i].subtopicID ==
                currentSubtopicID)
            {
                if (lessonProgressUI != null)
                {
                    lessonProgressUI.CompleteSubtopic(i);
                }

                Debug.Log(
                    $"Subtopic completed: " +
                    $"{currentLesson.subtopics[i].subtopicName}"
                );

                break;
            }
        }
    }

    private void UpdateCurrentSubtopicUI()
    {
        if (lessonProgressUI == null)
            return;

        if (currentLesson == null ||
            currentLesson.subtopics == null ||
            currentLesson.subtopics.Length == 0)
            return;

        // ==========================================
        // LEARNING OBJECTIVES
        // ==========================================

        if (currentSlide == 0)
        {
            lessonProgressUI.SetCurrentSubtopic(
                "Learning Objectives"
            );

            return;
        }

        // ==========================================
        // CURRENT LESSON SLIDE
        // ==========================================

        int lessonSlideIndex = currentSlide - 1;

        if (lessonSlideIndex < 0 ||
            lessonSlideIndex >= currentLesson.slides.Length)
            return;

        string currentSubtopicID =
            currentLesson.slides[lessonSlideIndex].subtopicID;

        // Find the subtopic that belongs to this slide.
        for (int i = 0;
             i < currentLesson.subtopics.Length;
             i++)
        {
            if (currentLesson.subtopics[i].subtopicID ==
                currentSubtopicID)
            {
                // Only update the name.
                //
                // The counter is handled by
                // CompleteSubtopic().
                lessonProgressUI.SetCurrentSubtopic(
                    currentLesson.subtopics[i].subtopicName
                );

                return;
            }
        }

        Debug.LogWarning(
            $"No matching subtopic found for ID: " +
            $"{currentSubtopicID}"
        );
    }

    private void ShowLessonCompletePopup()
    {
        if (lessonCompletePopup == null)
        {
            Debug.LogError(
                "Lesson Complete Popup is not assigned!"
            );

            return;
        }

        lessonCompletePopup.SetActive(true);

        if (lessonCompleteTitleText != null &&
        currentLesson != null)
        {
            lessonCompleteTitleText.text =
                currentLesson.lessonTitle;
        }

        // ==========================================
        // CHECK AR ACTIVITY AVAILABILITY
        // ==========================================

        bool hasARActivity =
            currentLesson != null &&
            currentLesson.hasARActivity &&
            currentLesson.arActivity != null;

        if (arActivityButton != null)
        {
            arActivityButton.gameObject.SetActive(true);
            arActivityButton.interactable = hasARActivity;

            arActivityButton.onClick.RemoveAllListeners();

            if (hasARActivity)
            {
                arActivityButton.onClick.AddListener(
                    OpenARActivity
                );
            }
        }

        // ==========================================
        // CHECK QUIZ AVAILABILITY
        // ==========================================

        bool hasQuiz =
            currentLesson != null &&
            currentLesson.hasQuiz &&
            currentLesson.quizData != null;

        if (quizButton != null)
        {
            quizButton.gameObject.SetActive(true);
            quizButton.interactable = hasQuiz;

            quizButton.onClick.RemoveAllListeners();

            if (hasQuiz)
            {
                quizButton.onClick.AddListener(
                    OpenQuiz
                );
            }
        }

        // ==========================================
        // LESSONS MENU
        // ==========================================

        if (lessonsMenuButton != null)
        {
            lessonsMenuButton.gameObject.SetActive(true);
            lessonsMenuButton.interactable = true;

            lessonsMenuButton.onClick.RemoveAllListeners();
            lessonsMenuButton.onClick.AddListener(
                ReturnToLessonsMenu
            );
        }
    }

    private void OpenARActivity()
    {
        SceneManager.LoadScene("ARScene");
    }

    private void OpenQuiz()
    {
        SceneManager.LoadScene("QuizScene");
    }

    private void ReturnToLessonsMenu()
    {
        SceneManager.LoadScene("COCScene");
    }
}