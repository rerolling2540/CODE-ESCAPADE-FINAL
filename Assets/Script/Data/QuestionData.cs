using UnityEngine;

public enum QuestionType
{
    MultipleChoice,     // Standard A-D question
    FillInTheBlank,     // Single line code completion
    WhatsTheOutput      // Show code snippet, choices are output
}

public abstract class QuestionData : ScriptableObject
{
    public QuestionType questionType;

    [TextArea(3, 6)]
    public string questionText;

    [TextArea(1, 3)]
    public string hintText;

    public float timeLimitInSeconds = 15f; // Timer length for this question

    public abstract bool CheckAnswer(int selectedIndex);
}