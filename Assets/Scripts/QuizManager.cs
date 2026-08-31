using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuizManager : MonoBehaviour
{
    [Header("Quiz Data")]
    [SerializeField] private QuizData quizData;

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
    [SerializeField] private Button retryButton;
    [SerializeField] private Button continueButton;

    [SerializeField] private Image resultMascot;
    [SerializeField] private Sprite happyMascotSprite;
    [SerializeField] private Sprite sadMascotSprite;

    private int currentQuestionIndex = 0;
    private int[] selectedAnswers;

    private void Start()
    {
        if (quizData == null)
        {
            Debug.LogError("QuizData is not assigned.");
            return;
        }

        if (quizData.questions == null || quizData.questions.Length == 0)
        {
            Debug.LogError("QuizData contains no questions.");
            return;
        }

        selectedAnswers = new int[quizData.questions.Length];

        for (int i = 0; i < selectedAnswers.Length; i++)
        {
            selectedAnswers[i] = -1;
        }

        resultsPanel.SetActive(false);

        nextButton.onClick.AddListener(NextQuestion);
        backButton.onClick.AddListener(PreviousQuestion);
        retryButton.onClick.AddListener(RetryQuiz);

        DisplayQuestion();
    }

    private void DisplayQuestion()
    {
        QuizQuestion question = quizData.questions[currentQuestionIndex];

        // Display question
        questionText.text = question.question;

        // Display question number
        questionCounter.text =
            $"Question {currentQuestionIndex + 1} of {quizData.questions.Length}";

        // Get the saved answer for this question
        int savedAnswer = selectedAnswers[currentQuestionIndex];

        // Remove old answer buttons
        foreach (Transform child in answerContainer)
        {
            Destroy(child.gameObject);
        }

        // Create new answer buttons
        for (int i = 0; i < question.answers.Length; i++)
        {
            GameObject buttonObject =
                Instantiate(answerButtonPrefab, answerContainer);

            QuizAnswerButton answerButton =
                buttonObject.GetComponent<QuizAnswerButton>();

            answerButton.Setup(question.answers[i], i, this);

            // Restore previously selected answer
            if (i == savedAnswer)
            {
                answerButton.SetSelected(true);
            }
        }

        // Update Back button
        backButton.interactable = currentQuestionIndex > 0;

        // Update Next button text
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
        // Don't allow the player to continue without answering
        if (selectedAnswers[currentQuestionIndex] == -1)
        {
            Debug.Log("Please select an answer first.");
            return;
        }

        // If this is the last question, submit the quiz
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

        // Hide quiz UI
        questionText.gameObject.SetActive(false);
        questionCounter.gameObject.SetActive(false);
        answerContainer.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(false);
        backButton.gameObject.SetActive(false);

        // Show results
        resultsPanel.SetActive(true);

        Debug.Log(
            $"Quiz Complete! Score: {score}/{totalQuestions} ({percentage:0}%)"
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
}