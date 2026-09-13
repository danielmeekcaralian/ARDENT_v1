using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class QuizManager : MonoBehaviour
{
    [Header("Quiz Data")]
    private QuizData quizData;

    [Header("Lesson Database")]
    [SerializeField] private LessonDatabase lessonDatabase;

    [Header("UI")]
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text questionCounter;
    [SerializeField] private Transform answerContainer;
    [SerializeField] private GameObject answerButtonPrefab;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button backButton;

    [Header("Results UI")]
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text percentageText;
    [SerializeField] private TMP_Text resultMessage;
    [SerializeField] private Button activityMenuButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button nextLessonButton;

    [SerializeField] private Image resultMascot;
    [SerializeField] private Sprite happyMascotSprite;
    [SerializeField] private Sprite sadMascotSprite;

    private int currentQuestionIndex = 0;
    private int[] selectedAnswers;

    private void Start()
    {
        LessonData currentLesson = LessonSession.CurrentLesson;

        if (currentLesson == null)
        {
            Debug.LogError(
                "QuizScene: No lesson selected."
            );

            return;
        }

        quizData = currentLesson.quizData;

        if (quizData == null)
        {
            Debug.LogError(
                "QuizScene: The selected lesson has no QuizData."
            );

            return;
        }

        if (quizData.questions == null ||
            quizData.questions.Length == 0)
        {
            Debug.LogError(
                "QuizScene: QuizData contains no questions."
            );

            return;
        }

        selectedAnswers =
            new int[quizData.questions.Length];

        for (int i = 0; i < selectedAnswers.Length; i++)
        {
            selectedAnswers[i] = -1;
        }

        resultsPanel.SetActive(false);

        nextButton.onClick.AddListener(NextQuestion);
        backButton.onClick.AddListener(PreviousQuestion);
        activityMenuButton.onClick.AddListener(BackToActivities);
        retryButton.onClick.AddListener(RetryQuiz);
        nextLessonButton.onClick.AddListener(NextLesson);

        DisplayQuestion();
    }

    private void DisplayQuestion()
    {
        QuizQuestion question = quizData.questions[currentQuestionIndex];
        
        questionText.text = question.question;
        
        questionCounter.text =
            $"Question {currentQuestionIndex + 1} of {quizData.questions.Length}";
        
        int savedAnswer = selectedAnswers[currentQuestionIndex];

        foreach (Transform child in answerContainer)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < question.answers.Length; i++)
        {
            GameObject buttonObject =
                Instantiate(answerButtonPrefab, answerContainer);

            QuizAnswerButton answerButton =
                buttonObject.GetComponent<QuizAnswerButton>();

            answerButton.Setup(question.answers[i], i, this);

            if (i == savedAnswer)
            {
                answerButton.SetSelected(true);
            }
        }

        backButton.interactable = currentQuestionIndex > 0;

        TMP_Text nextText = nextButton.GetComponentInChildren<TMP_Text>();

        if (currentQuestionIndex == quizData.questions.Length - 1)
        {
            nextText.text = "SUBMIT";
        }
        else
        {
            nextText.text = "NEXT";
        }
    }

    public void SelectAnswer(int answerIndex)
    {
        selectedAnswers[currentQuestionIndex] = answerIndex;

        QuizAnswerButton[] buttons =
            answerContainer.GetComponentsInChildren<QuizAnswerButton>();

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].SetSelected(i == answerIndex);
        }

        Debug.Log(
            $"Question {currentQuestionIndex + 1}: Selected answer {answerIndex}"
        );
    }

    private void NextQuestion()
    {
        if (selectedAnswers[currentQuestionIndex] == -1)
        {
            Debug.Log("Please select an answer first.");
            return;
        }

        if (currentQuestionIndex == quizData.questions.Length - 1)
        {
            SubmitQuiz();
            return;
        }

        currentQuestionIndex++;

        DisplayQuestion();
    }

    private void PreviousQuestion()
    {
        if (currentQuestionIndex <= 0)
        {
            return;
        }

        currentQuestionIndex--;

        DisplayQuestion();
    }

    private void SubmitQuiz()
    {
        int score = 0;

        for (int i = 0; i < quizData.questions.Length; i++)
        {
            if (selectedAnswers[i] ==
                quizData.questions[i].correctAnswerIndex)
            {
                score++;
            }
        }

        int totalQuestions = quizData.questions.Length;

        float percentage =
            (float)score / totalQuestions * 100f;

        int roundedPercentage = Mathf.RoundToInt(percentage);

        if (nextLessonButton != null)
        {
            nextLessonButton.interactable = roundedPercentage >= 100;
        }

        SaveQuizProgress(roundedPercentage);

        scoreText.text =
            $"Score: {score}/{totalQuestions}";

        percentageText.text =
            $"{percentage:0}%";

        if (percentage >= quizData.passingPercentage)
        {
            resultMessage.text = "PASSED!";
            resultMascot.sprite = happyMascotSprite;
        }
        else
        {
            resultMessage.text = "FAILED";
            resultMascot.sprite = sadMascotSprite;
        }

        questionText.gameObject.SetActive(false);
        questionCounter.gameObject.SetActive(false);
        answerContainer.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(false);
        backButton.gameObject.SetActive(false);

        resultsPanel.SetActive(true);

        Debug.Log(
            $"Quiz Complete! Score: {score}/{totalQuestions} ({percentage:0}%)"
        );
    }

    private void SaveQuizProgress(int percentage)
    {
        LessonData currentLesson =
            LessonSession.CurrentLesson;

        if (currentLesson == null)
        {
            Debug.LogError(
                "QuizManager: No current lesson found."
            );

            return;
        }

        if (ProgressManager.Instance == null)
        {
            Debug.LogError(
                "QuizManager: ProgressManager instance not found."
            );

            return;
        }

        ProgressManager.Instance.CompleteQuiz(
            currentLesson,
            percentage
        );

        Debug.Log(
            $"Lesson {currentLesson.lessonID} - " +
            $"Quiz Score: {percentage}%"
        );
    }

    private void RetryQuiz()
    {
        currentQuestionIndex = 0;

        for (int i = 0; i < selectedAnswers.Length; i++)
        {
            selectedAnswers[i] = -1;
        }

        resultsPanel.SetActive(false);

        questionText.gameObject.SetActive(true);
        questionCounter.gameObject.SetActive(true);
        answerContainer.gameObject.SetActive(true);
        nextButton.gameObject.SetActive(true);
        backButton.gameObject.SetActive(true);

        DisplayQuestion();
    }

    private void BackToActivities()
    {
        SceneManager.LoadScene("ActivitySelectionScene");
    }

    private void NextLesson()
    {
        LessonData currentLesson =
            LessonSession.CurrentLesson;

        if (currentLesson == null)
        {
            Debug.LogError(
                "QuizManager: No current lesson found."
            );

            return;
        }

        if (lessonDatabase == null)
        {
            Debug.LogError(
                "QuizManager: LessonDatabase is not assigned."
            );

            return;
        }

        if (ProgressManager.Instance == null)
        {
            Debug.LogError(
                "QuizManager: ProgressManager instance not found."
            );

            return;
        }

        if (!ProgressManager.Instance.IsLessonComplete(
                currentLesson))
        {
            Debug.LogWarning(
                "Next lesson is locked. " +
                "Complete all required activities " +
                "and achieve 100% on the quiz."
            );

            return;
        }

        LessonData nextLesson =
            lessonDatabase.GetNextLesson(
                currentLesson.lessonID
            );

        if (nextLesson == null)
        {
            Debug.Log(
                "There is no next lesson."
            );

            return;
        }

        LessonSession.SetLesson(nextLesson);

        SceneManager.LoadScene(
            "ActivitySelectionScene"
        );
    }
}