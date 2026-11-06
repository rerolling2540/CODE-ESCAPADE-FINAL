using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>Published questions for exploration, with the same seed and cache as battle.</summary>
public sealed class WorldQuestionBank : MonoBehaviour
{
    private static WorldQuestionBank instance;
    private readonly List<MultipleChoiceQuestionData> questions=new List<MultipleChoiceQuestionData>();
    [Serializable] private sealed class Rows { public Row[] items; }
    [Serializable] private sealed class Row
    {
        public string id,language,topic,difficulty,prompt,code_snippet;
        public string[] choices;
        public int correct_index,min_level,max_level;
    }

    public static MultipleChoiceQuestionData[] GetQuestions()
    {
        if(instance==null)
        {
            var go=new GameObject("World Question Bank");
            instance=go.AddComponent<WorldQuestionBank>();
            DontDestroyOnLoad(go);
            instance.Load();
        }
        return instance.questions.ToArray();
    }

    private void Load()
    {
        var seed=Resources.Load<TextAsset>("QuestionBanks/StarterQuestions");
        if(seed!=null)Replace(seed.text);
        var settings=Resources.Load<WorldQuestionBankSettings>("QuestionBanks/Connection");
        string root=MainMenuSaveSlots.CurrentSupabaseProjectUrl;
        string key=MainMenuSaveSlots.CurrentSupabasePublishableKey;
        if(string.IsNullOrWhiteSpace(root)||string.IsNullOrWhiteSpace(key))
        {root=settings!=null?settings.projectUrl:null;key=settings!=null?settings.publishableKey:null;}
        if(string.IsNullOrWhiteSpace(root)||string.IsNullOrWhiteSpace(key))return;
        root=root.Trim().TrimEnd('/');
        if(!Uri.TryCreate(root,UriKind.Absolute,out var uri))return;
        string host=System.Text.RegularExpressions.Regex.Replace(uri.Host.ToLowerInvariant(),"[^a-z0-9]","_");
        string cache=Path.Combine(Application.persistentDataPath,"QuestionBank_"+host+".json");
        try{if(File.Exists(cache))Replace(File.ReadAllText(cache));}
        catch(Exception e){Debug.LogWarning("Exploration question cache could not be read: "+e.Message);}
        StartCoroutine(Refresh(root,key.Trim(),cache));
    }

    public static List<MultipleChoiceQuestionData> Parse(string json)
    {
        var rows=JsonUtility.FromJson<Rows>("{\"items\":"+json+"}")?.items;
        var result=new List<MultipleChoiceQuestionData>();
        foreach(var row in rows??Array.Empty<Row>())
        {
            if(row==null||string.IsNullOrWhiteSpace(row.id)||string.IsNullOrWhiteSpace(row.prompt)||
               row.choices==null||row.choices.Length<2||row.choices.Length>4||
               row.correct_index<0||row.correct_index>=row.choices.Length)continue;
            string lang=QuestionCurriculum.Language(row.language);
            if(lang!="java"&&lang!="csharp"&&lang!="python")continue;
            var q=ScriptableObject.CreateInstance<MultipleChoiceQuestionData>();
            q.name=q.questionId=row.id;q.language=lang;q.topic=QuestionCurriculum.Topic(row.topic,row.id);
            q.difficulty=QuestionCurriculum.Difficulty(QuestionCurriculum.Tier(row.difficulty));
            q.minLevel=Mathf.Max(1,row.min_level);q.maxLevel=row.max_level;
            q.questionText=row.prompt;q.codeSnippet=row.code_snippet??"";
            q.options=row.choices;q.correctAnswerIndex=row.correct_index;q.timeLimitInSeconds=25;
            QuestionCurriculum.Normalize(q);
            result.Add(q);
        }
        return result;
    }

    private bool Replace(string json)
    {
        List<MultipleChoiceQuestionData> next;
        try{next=Parse(json);}catch(Exception){return false;}
        // A partial or failed response must never strand one of the three heroes.
        if(new[]{"java","csharp","python"}.Any(lang=>!next.Any(q=>q.language==lang)))
        {foreach(var q in next)Destroy(q);return false;}
        foreach(var q in questions)if(q!=null)Destroy(q);
        questions.Clear();questions.AddRange(next);return true;
    }

    private IEnumerator Refresh(string root,string key,string cache)
    {
        while(true)
        {
            // Page explicitly: Supabase may contain more than its default row limit.
            var pages=new List<string>();int offset=0;bool success=true;
            while(true)
            {
                string url=root+"/rest/v1/question_bank?select=id,language,topic,difficulty,prompt,code_snippet,choices,correct_index,min_level,max_level&published=eq.true&order=id.asc&limit=1000&offset="+offset;
                using(var request=UnityWebRequest.Get(url))
                {
                    request.timeout=12;request.SetRequestHeader("apikey",key);
                    yield return request.SendWebRequest();
                    if(request.result!=UnityWebRequest.Result.Success){success=false;break;}
                    string page=request.downloadHandler.text.Trim();
                    Rows decoded=null;
                    try{decoded=JsonUtility.FromJson<Rows>("{\"items\":"+page+"}");}catch(Exception){}
                    if(decoded?.items==null){success=false;break;}
                    string inner=page.Substring(1,page.Length-2).Trim();if(inner.Length>0)pages.Add(inner);
                    offset+=decoded.items.Length;if(decoded.items.Length<1000)break;
                }
            }
            if(success)
            {
                // Keep the object backing an open question alive until it is resolved.
                while(DoorJavaQuestionPanel.IsOpen)yield return null;
                string json="["+string.Join(",",pages)+"]";
                if(Replace(json))
                {
                    try{File.WriteAllText(cache,json);}catch(Exception e){Debug.LogWarning("Exploration questions loaded but could not be cached: "+e.Message);}
                    yield break;
                }
            }
            yield return new WaitForSecondsRealtime(60);
        }
    }
    void OnDestroy(){if(instance==this)instance=null;foreach(var q in questions)if(q!=null)Destroy(q);}
}
