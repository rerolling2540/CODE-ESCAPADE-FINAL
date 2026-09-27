using System;
using System.Collections;
using UnityEngine;

public class QuestionSystem : Singleton<QuestionSystem>
{
    [SerializeField] private QuestionDatabase database;
    [SerializeField] private QuestionUI questionUI;

    private void OnEnable()
    {
        ActionSystem.AttachPerformer<QuestionGA>(PerformQuestion);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<QuestionGA>();
    }

    private IEnumerator PerformQuestion(QuestionGA action)
    {
        bool isDone = false;
        bool isCorrect = false;

        questionUI.SetupAndShow(
            action.Question,
            callback: (selectedIndex) =>
            {
                isCorrect = action.Question.CheckAnswer(selectedIndex);
                isDone = true;
            },
            onTimeOut: () =>
            {
                Debug.Log("Time ran out on Pop Quiz!");
                isCorrect = false;
                isDone = true;
            }
        );

        // Pause the ActionSystem until answered or timed out
        while (!isDone)
        {
            yield return null;
        }

        // Send answer result back to whoever queued the action
        action.OnAnswered?.Invoke(isCorrect);
    }
     public void AskRandomQuestion(Action<bool> onAnswered)
    {
        Debug.Log("QuestionSystem: AskRandomQuestion");
        QuestionData randomQuestion = database.GetRandomQuestion();

        Debug.Log("Question Text: " + randomQuestion.questionText);// temp

        ActionSystem.Instance.Perform(
            new QuestionGA(randomQuestion, onAnswered)
        );
    }
}