using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ActivitySelectionManager : MonoBehaviour
{
    [Header("Lesson Information")]
    [SerializeField] private TMP_Text lessonTitleText;
    [SerializeField] private TMP_Text lessonDescriptionText;

    [Header("Activity Buttons")]
    [SerializeField] private Button lessonButton;
    [SerializeField] private Button arActivityButton;
    [SerializeField] private Button quizButton;

    [Header("Unavailable Settings")]
    [SerializeField] private bool hideUnavailableButtons = false;

    private LessonData currentLesson;

    private void Start()
    {
        currentLesson = LessonSession.CurrentLesson;

        if (currentLesson == null)
        {
            Debug.LogError(
                "ActivitySelectionScene: No lesson selected."
            );

            return;
        }

        DisplayLesson();
        ConfigureActivities();
    }

    private void DisplayLesson()
    {
        if (lessonTitleText != null)
        {
            lessonTitleText.text =
                currentLesson.lessonTitle;
        }

        if (lessonDescriptionText != null)
        {
            lessonDescriptionText.text =
                currentLesson.description;
        }
    }

    private void ConfigureActivities()
    {
        // -----------------------------------------
        // LESSON
        // -----------------------------------------

        bool hasLesson =
            currentLesson.slides != null &&
            currentLesson.slides.Length > 0;

        ConfigureButton(
            lessonButton,
            hasLesson
        );

        // -----------------------------------------
        // AR ACTIVITY
        // -----------------------------------------

        bool hasARActivity =
            currentLesson.arActivity != null;

        ConfigureButton(
            arActivityButton,
            hasARActivity
        );

        // -----------------------------------------
        // QUIZ
        // -----------------------------------------

        bool hasQuiz =
            currentLesson.quizData != null;

        ConfigureButton(
            quizButton,
            hasQuiz
        );
    }

    private void ConfigureButton(
        Button button,
        bool available)
    {
        if (button == null)
            return;

        if (hideUnavailableButtons)
        {
            button.gameObject.SetActive(available);
        }
        else
        {
            button.interactable = available;
        }
    }

    // =====================================================
    // OPEN ACTIVITIES
    // =====================================================

    public void OpenLesson()
    {
        if (currentLesson == null)
            return;

        if (currentLesson.slides == null ||
            currentLesson.slides.Length == 0)
        {
            Debug.LogWarning(
                "This lesson has no lesson slides."
            );

            return;
        }

        SceneManager.LoadScene(
            currentLesson.lessonSceneName
        );
    }

    public void OpenARActivity()
    {
        if (currentLesson == null)
            return;

        if (currentLesson.arActivity == null)
        {
            Debug.LogWarning(
                "This lesson has no AR activity."
            );

            return;
        }

        SceneManager.LoadScene("ARScene");
    }

    public void OpenQuiz()
    {
        if (currentLesson == null)
            return;

        if (currentLesson.quizData == null)
        {
            Debug.LogWarning(
                "This lesson has no quiz."
            );

            return;
        }

        SceneManager.LoadScene("QuizScene");
    }

    // =====================================================
    // BACK
    // =====================================================

    public void GoBack()
    {
        if (string.IsNullOrEmpty(
                LessonSession.CurrentCOC))
        {
            Debug.LogError(
                "No COC has been selected."
            );

            return;
        }

        SceneManager.LoadScene(
            LessonSession.CurrentCOC
        );
    }
}