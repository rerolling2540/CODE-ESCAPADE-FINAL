using System.Collections.Generic;
using UnityEngine;

/// <summary>Capstone rotates through the six taught concepts; card rarity and level tier caps still apply.</summary>
public sealed class WardenFinalTrial : MonoBehaviour
{
    public static WardenFinalTrial Instance {get;private set;}
    private readonly List<MultipleChoiceQuestionData> owned=new List<MultipleChoiceQuestionData>();
    private int next;
    void Awake(){Instance=this;}
    void OnDestroy(){if(Instance==this)Instance=null;foreach(var q in owned)if(q!=null)Destroy(q);}
    public QuestionData Question(Card card)
    {
        int minimum=card?.CardData!=null?card.CardData.minimumQuestionTier:1;
        int maximum=card?.CardData!=null?card.CardData.maximumQuestionTier:3;
        int cap=Mathf.Min(maximum,QuestionCurriculum.LevelTier(QuestionPerformanceTracker.GetCurrentPlayerLevel()));
        if(minimum>cap)return null;
        string language=LearningQuests.Language;var saved=SaveSlots.Read(SaveSlots.Active);
        for(int tries=0;tries<6;tries++)
        {
            int topic=next++%6;if(!WardenQuest.Lesson(saved,topic))continue;
            string id="warden-final-"+language+"-"+VisualCodeLessons.Topics[topic]+"-tier-"+minimum;
            var existing=owned.Find(q=>q.questionId==id);if(existing!=null)return existing;
            var lesson=WorkshopVariations.Get(language,topic,minimum==3?2:1);
            var q=ScriptableObject.CreateInstance<MultipleChoiceQuestionData>();q.name=id;q.questionId=id;q.language=language;q.topic=lesson.topic;q.difficulty=minimum==1?"easy":minimum==2?"medium":"hard";
            q.questionType=QuestionType.WhatsTheOutput;q.questionText="Warden trial: predict what this code shows.";q.codeSnippet=lesson.challenge;q.options=lesson.answers;q.correctAnswerIndex=lesson.correct;q.hintText=lesson.explanation;q.timeLimitInSeconds=90;
            owned.Add(q);return q;
        }
        return null;
    }
}
