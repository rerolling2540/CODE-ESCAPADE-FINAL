using UnityEngine;

[CreateAssetMenu(fileName = "NewMultipleChoiceQuestion", menuName = "CodeEscapade/Questions/Multiple Choice")]
public class MultipleChoiceQuestionData : QuestionData
{
    [Header("Code Snippet (Optional for What's the Output)")]
    [TextArea(4, 8)]
    public string codeSnippet;

    [Header("Choices (A, B, C, D)")]
    public string[] options = new string[4];

    public int correctAnswerIndex; // 0 for A, 1 for B, 2 for C, 3 for D

    public override bool CheckAnswer(int selectedIndex)
    {
        return selectedIndex == correctAnswerIndex;
    }
}