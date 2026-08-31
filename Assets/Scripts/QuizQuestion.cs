using System;
using UnityEngine;

[Serializable]
public class QuizQuestion
{
    public string question;

    public string[] answers;

    public int correctAnswerIndex;
}