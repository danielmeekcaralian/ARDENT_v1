using UnityEngine;

[System.Serializable]
public class LessonSlideData
{
    [Header("Subtopic")]
    public string subtopicID;

    [Header("Slide")]
    public SlideType slideType;

    [TextArea(3, 8)]
    public string content;

    public Sprite image;
}

public enum SlideType
{
    Content,
    ImageContent
}