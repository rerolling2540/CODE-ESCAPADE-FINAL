using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Per-encounter snapshots; independent of account-wide performance history.</summary>
public static class BattleQuestionReport
{
    public sealed class Entry
    {
        public string id,prompt,code,answer,correctAnswer,topic,language,outcome="in_progress";
        public string[] choices;
        public float started,duration;
    }
    public static readonly List<Entry> Entries=new List<Entry>();
    public static bool Pending {get;private set;}
    public static string EncounterId {get;private set;}
    public static void BeginBattle(string encounterId=null){Entries.Clear();Pending=true;EncounterId=encounterId;}
    public static void Presented(string id,QuestionData question)
    {
        if(!Pending||question==null||Entries.Any(e=>e.id==id))return;
        string[] choices;string code;int correct;
        if(question is MultipleChoiceQuestionData mc){choices=mc.options;code=mc.codeSnippet;correct=mc.correctAnswerIndex;}
        else if(question is FillInBlankQuestionData fill){choices=fill.optionBlanks;code=fill.codeWithBlank;correct=fill.correctBlankIndex;}
        else{choices=new string[0];code="";correct=-1;}
        Entries.Add(new Entry{id=id,prompt=question.questionText,code=code,language=QuestionCurriculum.Language(question),topic=QuestionCurriculum.Topic(question),choices=choices?.ToArray()??new string[0],correctAnswer=choices!=null&&correct>=0&&correct<choices.Length?choices[correct]:"Not available",started=Time.realtimeSinceStartup});
    }
    public static void Resolve(string id,string outcome,int selected=-1)
    {
        var entry=Entries.FirstOrDefault(e=>e.id==id);if(entry==null||entry.outcome!="in_progress")return;
        entry.outcome=outcome;entry.answer=selected>=0&&selected<entry.choices.Length?entry.choices[selected]:"No answer";entry.duration=Mathf.Max(0,Time.realtimeSinceStartup-entry.started);
    }
    public static void Finish(){foreach(var e in Entries.Where(e=>e.outcome=="in_progress").ToArray())Resolve(e.id,"abandoned");Pending=false;}
    public static string Summary()
    {
        int correct=Entries.Count(e=>e.outcome=="correct"),wrong=Entries.Count(e=>e.outcome=="incorrect"),timed=Entries.Count(e=>e.outcome=="timed_out"),abandoned=Entries.Count(e=>e.outcome=="abandoned"),resolved=correct+wrong+timed;
        int unique=Entries.GroupBy(e=>new{e.language,e.prompt,e.code,e.correctAnswer}).Count();
        return "Questions: "+unique+"   Attempts: "+Entries.Count+"   Answered: "+(correct+wrong)+"\nCorrect: "+correct+"   Wrong: "+wrong+"   Timed out: "+timed+"\nUnfinished: "+abandoned+"   Accuracy: "+(resolved==0?"N/A":(100f*correct/resolved).ToString("0.#")+"%");
    }
}
