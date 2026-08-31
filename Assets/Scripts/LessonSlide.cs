using UnityEngine;

[System.Serializable]
public class LessonSlide
{
    public string title;

    [TextArea(5, 10)]
    public string content;

    public Sprite image;
}