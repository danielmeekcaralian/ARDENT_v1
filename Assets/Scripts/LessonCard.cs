using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LessonCard : MonoBehaviour
{
    [Header("Text")]
    public TMP_Text titleText;
    public TMP_Text descriptionText;

    [Header("UI")]
    public Button startButton;

    public Image lockIcon;
    public Image medalIcon;

    [Header("Sprites")]
    public Sprite bronzeSprite;
    public Sprite silverSprite;
    public Sprite goldSprite;

    private LessonData currentLesson;

    public void Setup(LessonData lesson, bool unlocked, Medal medal)
    {
        currentLesson = lesson;

        titleText.text = lesson.lessonTitle;
        descriptionText.text = lesson.description;

        // Lock icon
        lockIcon.gameObject.SetActive(!unlocked);

        // Start button
        startButton.interactable = unlocked;

        // Start button action
        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(StartLesson);

        // Medal
        if (!unlocked || medal == Medal.None)
        {
            medalIcon.gameObject.SetActive(false);
        }
        else
        {
            medalIcon.gameObject.SetActive(true);

            switch (medal)
            {
                case Medal.Bronze:
                    medalIcon.sprite = bronzeSprite;
                    break;

                case Medal.Silver:
                    medalIcon.sprite = silverSprite;
                    break;

                case Medal.Gold:
                    medalIcon.sprite = goldSprite;
                    break;
            }
        }
    }

    private void StartLesson()
    {
        LessonSession.SetLesson(currentLesson);
        SceneManager.LoadScene("ActivitySelectionScene");
    }
}