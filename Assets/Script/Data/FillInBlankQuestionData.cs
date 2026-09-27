using UnityEngine;

[CreateAssetMenu(fileName = "NewFillInBlankQuestion", menuName = "CodeEscapade/Questions/Fill In The Blank")]
public class FillInBlankQuestionData : QuestionData
{
    [Header("Code with missing blank (e.g. 'for (int [___] = 0; ...')")]
    [TextArea(4, 8)]
    public string codeWithBlank;

    [Header("Options for the missing blank")]
    public string[] optionBlanks = new string[4];

    public int correctBlankIndex;

    public override bool CheckAnswer(int selectedIndex)
    {
        return selectedIndex == correctBlankIndex;
    }
}