using UnityEngine;

[CreateAssetMenu(fileName = "New Quiz", menuName = "ARDENT/Quiz")]
public class QuizData : ScriptableObject
{
    [Range(0, 100)]
    public int passingPercentage = 70;

    public QuizQuestion[] questions;
}