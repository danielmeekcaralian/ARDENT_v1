using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LessonProgressUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform progressPanel;
    [SerializeField] private Transform segmentContainer;
    [SerializeField] private GameObject segmentPrefab;

    [Header("Current Subtopic")]
    [SerializeField] private TMP_Text subtopicNameText;
    [SerializeField] private TMP_Text subtopicCounterText;

    [Header("Animation")]
    [SerializeField] private float slideDuration = 0.5f;

    [Header("Auto Hide")]
    [SerializeField] private float displayDuration = 2f;

    private List<Image> segments = new List<Image>();
    private HashSet<int> shownSubtopics = new HashSet<int>();
    private string pendingCompletedSubtopicName;
    private bool isPopupShowing;

    private Vector2 hiddenPosition;
    private Vector2 shownPosition;
    private LessonData currentLesson;


    private void Awake()
    {
        shownPosition = progressPanel.anchoredPosition;

        hiddenPosition = shownPosition;
        hiddenPosition.y = -progressPanel.rect.height;

        progressPanel.anchoredPosition = hiddenPosition;
    }

    private void Start()
    {
        Debug.Log("LessonProgressUI STARTED");

        currentLesson = LessonSession.CurrentLesson;

        if (LessonSession.CurrentLesson == null)
        {
            Debug.LogError("LessonProgressUI: CurrentLesson is NULL!");
            return;
        }

        Debug.Log(
            "Current lesson: " +
            LessonSession.CurrentLesson.lessonTitle
        );

        Debug.Log(
            "Subtopics: " +
            LessonSession.CurrentLesson.subtopics.Length
        );

        CreateSegments(
            LessonSession.CurrentLesson.subtopics.Length
        );

    }

    public void CreateSegments(int subtopicCount)
    {
        foreach (Transform child in segmentContainer)
        {
            Destroy(child.gameObject);
        }

        segments.Clear();

        for (int i = 0; i < subtopicCount; i++)
        {
            GameObject segment = Instantiate(
                segmentPrefab,
                segmentContainer
            );

            Image image = segment.GetComponent<Image>();

            if (image != null)
            {
                image.fillAmount = 0f;
                segments.Add(image);
            }
        }
    }

    public void CompleteSubtopic(int index)
    {
        if (index < 0 || index >= segments.Count)
            return;

        // Fill the completed segment
        segments[index].fillAmount = 1f;

        // Calculate completed subtopics
        int completedCount = 0;

        foreach (Image segment in segments)
        {
            if (segment.fillAmount >= 1f)
            {
                completedCount++;
            }
        }

        // Update counter
        if (subtopicCounterText != null)
        {
            subtopicCounterText.text =
                $"{completedCount} / {segments.Count}";
        }

        // Only show popup once
        if (!shownSubtopics.Contains(index))
        {
            shownSubtopics.Add(index);

            // Get the COMPLETED subtopic name
            if (currentLesson != null &&
                currentLesson.subtopics != null &&
                index < currentLesson.subtopics.Length)
            {
                pendingCompletedSubtopicName =
                    currentLesson.subtopics[index].subtopicName;
            }

            Show();
        }
    }

    public void SetCurrentSubtopic(string subtopicName)
    {
        if (isPopupShowing)
            return;

        if (subtopicNameText != null)
        {
            subtopicNameText.text = subtopicName;
        }
    }

    public void Show()
    {
        StopAllCoroutines();

        StartCoroutine(
            ShowAndAutoHide()
        );
    }

    public void Hide()
    {
        StopAllCoroutines();
        StartCoroutine(
            SlidePanel(shownPosition, hiddenPosition)
        );
    }

    private IEnumerator SlidePanel(
        Vector2 start,
        Vector2 target)
    {
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(elapsed / slideDuration);

            t = Mathf.SmoothStep(0f, 1f, t);

            progressPanel.anchoredPosition =
                Vector2.Lerp(start, target, t);

            yield return null;
        }

        progressPanel.anchoredPosition = target;
    }
    private IEnumerator ShowAndAutoHide()
    {
        isPopupShowing = true;

        // Set the completed subtopic BEFORE animation starts
        if (subtopicNameText != null &&
            !string.IsNullOrEmpty(pendingCompletedSubtopicName))
        {
            subtopicNameText.text =
                pendingCompletedSubtopicName;
        }

        // Now slide the popup in
        yield return StartCoroutine(
            SlidePanel(
                hiddenPosition,
                shownPosition
            )
        );

        // Keep it visible
        yield return new WaitForSeconds(
            displayDuration
        );

        // Slide it back down
        yield return StartCoroutine(
            SlidePanel(
                shownPosition,
                hiddenPosition
            )
        );

        isPopupShowing = false;
    }
}