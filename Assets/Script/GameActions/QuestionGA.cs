using System;

public class QuestionGA : GameAction
{
    public QuestionData Question { get; private set; }
    public Action<bool> OnAnswered { get; private set; } // Returns true if correct, false if incorrect/timed out

    public QuestionGA(QuestionData question, Action<bool> onAnswered = null)
    {
        Question = question;
        OnAnswered = onAnswered;
    }
}