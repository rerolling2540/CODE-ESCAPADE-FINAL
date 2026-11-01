using System.Linq;

public static class WorkshopProgression
{
    public static string Id(string language,int topic,int tier)=>"mentor-"+language+"-"+VisualCodeLessons.Topics[topic]+"-prediction"+(tier==0?"":"-tier-"+(tier+1));
    public static bool Passed(string language,int topic,int tier)
    {
        var save=SaveSlots.Read(SaveSlots.Active);string id=Id(language,topic,tier);
        return save?.workshopExercisesCompleted?.Contains(id)==true||
            tier==0&&save?.learningQuestProofs?.Any(p=>p.StartsWith(language+"-teacher-")&&p.EndsWith("/"+VisualCodeLessons.Topics[topic]))==true||
            QuestionPerformanceTracker.GetLearningHistory(language,SaveSlots.Active).Any(a=>a.question_id==id&&a.outcome=="correct");
    }
    public static void MarkPassed(string language,int topic,int tier)
    {
        var save=SaveSlots.Read(SaveSlots.Active);if(save==null)return;
        if(save.workshopExercisesCompleted==null)save.workshopExercisesCompleted=new System.Collections.Generic.List<string>();
        string id=Id(language,topic,tier);if(save.workshopExercisesCompleted.Contains(id))return;
        save.workshopExercisesCompleted.Add(id);SaveSlots.Write(save);
    }
    public static bool Available(string language,int topic,int tier)=>
        (topic==0||Passed(language,topic-1,0))&&(tier==0||Passed(language,topic,tier-1));
    public static int SuggestedTier(string language,int topic)
    {for(int i=0;i<3;i++)if(!Passed(language,topic,i))return i;return 0;}
    public static string TierName(int tier)=>new[]{"Foundation","Applied","Challenge"}[tier];
    public static string Requirement(int topic,int tier)=>tier>0?"Complete "+TierName(tier-1)+" for this topic first.":"Complete the previous foundation topic: "+VisualCodeLessons.Topics[System.Math.Max(0,topic-1)]+".";
}
