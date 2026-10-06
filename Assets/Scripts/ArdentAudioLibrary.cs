using UnityEngine;

[CreateAssetMenu(fileName = "ArdentAudioLibrary", menuName = "ARDENT/Audio Library")]
public sealed class ArdentAudioLibrary : ScriptableObject
{
    [Header("Background Music")]
    public AudioClip mainBGM;
    public AudioClip hardwareLibraryBGM;
    public AudioClip quizBGM;

    [Header("UI")]
    public AudioClip buttonPress;
    public AudioClip lessonButtonPress;
    public AudioClip quizAnswerButtonPress;

    [Header("Activities and Notifications")]
    public AudioClip placeItem;
    public AudioClip correctItemPlacement;
    public AudioClip error;
    public AudioClip fail;
    public AudioClip success;
    public AudioClip subtopicComplete;
    public AudioClip itemUnlock;

    public AudioClip Clip(ArdentSound sound)
    {
        switch (sound)
        {
            case ArdentSound.Button: return buttonPress;
            case ArdentSound.LessonButton: return lessonButtonPress;
            case ArdentSound.QuizAnswerButton: return quizAnswerButtonPress;
            case ArdentSound.PlaceItem: return placeItem;
            case ArdentSound.CorrectPlacement: return correctItemPlacement;
            case ArdentSound.Error: return error;
            case ArdentSound.Fail: return fail;
            case ArdentSound.Success: return success;
            case ArdentSound.SubtopicComplete: return subtopicComplete;
            case ArdentSound.ItemUnlock: return itemUnlock != null ? itemUnlock : subtopicComplete;
            default: return null;
        }
    }
}

public enum ArdentSound
{
    Button,
    LessonButton,
    QuizAnswerButton,
    PlaceItem,
    CorrectPlacement,
    Error,
    Fail,
    Success,
    SubtopicComplete,
    ItemUnlock
}
