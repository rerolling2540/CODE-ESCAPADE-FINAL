using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class WorldQuestionChecks
{
    [MenuItem("Tools/Code Escapade/Check World Questions")]
    public static void CheckFromMenu()=>Debug.Log(Run());
    public static string Run()
    {
        int checks=0;
        Action<bool,string> check=(ok,message)=>{checks++;if(!ok)throw new InvalidOperationException(message);};
        var bank=WorldQuestionBank.Parse(Resources.Load<TextAsset>("QuestionBanks/StarterQuestions").text);
        try
        {
            foreach(string language in new[]{"java","csharp","python"})
            {
                var own=bank.Where(q=>q.language==language).ToList();
                check(own.Count==154,language+": missing seed questions.");
                foreach(int level in new[]{1,5,6,15,16,18})
                {
                    var history=new List<QuestionAttempt>();string previous=null;
                    for(int i=0;i<80;i++)
                    {
                        var q=AdaptiveQuestionSelector.Select(bank,language,level,history,(i%31)/30f,previous);
                        check(q!=null,language+": no question at level "+level);
                        check(q.language==language,language+": wrong language selected.");
                        check(Math.Max(q.minLevel,QuestionCurriculum.MinimumLevel(q.topic))<=level,language+": topic level bypass.");
                        check(QuestionCurriculum.Tier(q.difficulty)<=QuestionCurriculum.LevelTier(level),language+": difficulty level bypass.");
                        check(q.questionId!=previous,language+": immediate repeat.");
                        history.Add(new QuestionAttempt {question_id=q.questionId,language=language,topic=q.topic,
                            difficulty=q.difficulty,outcome="correct",presented_at=new DateTime(2026,1,1).AddSeconds(i).ToString("o")});
                        previous=q.questionId;
                    }
                }
                foreach(var q in own)
                    check(q.options.Length>=2&&q.options.Length<=4&&q.correctAnswerIndex>=0&&q.correctAnswerIndex<q.options.Length,
                        language+": unsupported answer choices.");
                var resolved=new[]{"correct","incorrect","timed_out"}.Select((result,i)=>new QuestionAttempt{
                    question_id="review-"+i,language=language,topic="output",difficulty="easy",outcome=result,
                    presented_at=new DateTime(2026,1,1).AddSeconds(i).ToString("o")}).ToArray();
                check(AdaptiveQuestionSelector.Summarize(resolved).NeedsReview,language+": errors must trigger review.");
                check(AdaptiveQuestionSelector.Select(bank,language,18,Array.Empty<QuestionAttempt>(),1).topic=="output",
                    language+": fresh heroes must start with foundations even at a high level.");
            }
            var config=Resources.Load<WorldQuestionBankSettings>("QuestionBanks/Connection");
            check(config!=null&&!string.IsNullOrWhiteSpace(config.projectUrl)&&!string.IsNullOrWhiteSpace(config.publishableKey),"Shared database connection missing.");
            check(typeof(DoorJavaQuestionPanel).GetMethod("Show",new[]{typeof(Action),typeof(Action),typeof(bool),typeof(bool)})!=null,
                "Door, portal and trap must support the shared-bank entry point.");
            return "PASS: "+checks+" world-question checks across all 3 languages and levels 1, 5, 6, 15, 16, 18; no player data changed.";
        }
        finally{foreach(var q in bank)UnityEngine.Object.DestroyImmediate(q);}
    }
}
