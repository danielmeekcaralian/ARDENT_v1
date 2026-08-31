using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuizAnswerButton : MonoBehaviour
{
    [SerializeField] private TMP_Text answerText;
    [SerializeField] private Button button;
    [SerializeField] private Image background;

    [Header("Button Sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite selectedSprite;

    private int answerIndex;
    private QuizManager quizManager;

    public void Setup(string text, int index, QuizManager manager)
    {
        answerText.text = text;
        answerIndex = index;
        quizManager = manager;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClicked);

        SetSelected(false);
    }

    private void OnClicked()
    {
        quizManager.SelectAnswer(answerIndex);
    }

    public void SetSelected(bool selected)
    {
        background.sprite = selected ? selectedSprite : normalSprite;
    }
}