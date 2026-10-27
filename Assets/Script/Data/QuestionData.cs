using UnityEngine;

public enum QuestionType
{
    MultipleChoice,     // Standard A-D question
    FillInTheBlank,     // Single line code completion
    WhatsTheOutput      // Show code snippet, choices are output
}

public abstract class QuestionData : ScriptableObject
{
    [Header("Question identity and curriculum")]
    public string questionId;
    public string topic;
    public string language;
    public string difficulty;
    public int minLevel = 1;
    public int maxLevel;

    public QuestionType questionType;

    [TextArea(3, 6)]
    public string questionText;

    [TextArea(1, 3)]
    public string hintText;

    public float timeLimitInSeconds = 15f; // Timer length for this question

    public abstract bool CheckAnswer(int selectedIndex);
}
