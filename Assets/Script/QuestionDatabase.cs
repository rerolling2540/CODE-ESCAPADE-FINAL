using UnityEngine;

[CreateAssetMenu(menuName = "CodeEscapade/Question Database")]
public class QuestionDatabase : ScriptableObject
{
    public QuestionData[] questions;

    public QuestionData GetRandomQuestion()
    {
        return questions[Random.Range(0, questions.Length)];
    }
    
}
