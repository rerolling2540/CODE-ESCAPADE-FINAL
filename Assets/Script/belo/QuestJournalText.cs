using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class QuestJournalText
{
    public static string Topic(int t)=>new[]{"Showing messages","Storing values","Making decisions","Repeating instructions","Arrays and lists","Using functions"}[t];
    public static string Main(SaveSlots.Record s)
    {
        int c=WardenQuest.Chapter(s);
        if(c==6)return "<b>WARDEN OF THE HOLLOW</b>\nQuest complete. You earned the Warden title.";
        if(s==null||!s.wardenAccepted)return "<b>MAIN QUEST</b>\nSpeak to the elf in Scene 2 to begin.";
        string[] steps={"Learn output and variables with Alden.\nThen try the elf's supply challenge.","Win the tutorial goblin battle.\nRead its report and press Continue.","Learn conditions with Rowan.\nThen solve the elf's gate challenge.","Learn loops with Rowan.\nThen solve the elf's watchfire challenge.","Learn arrays/lists and functions with Mira.\nThen solve the elf's two potion challenges.","Finish all six foundation lessons.\nWin the final trial and read its report."};
        string status=WardenQuest.Ready(s)?"<color=#24552A><b>REWARD READY</b></color>\nSpeak to the elf to collect it.":"<b>Next step</b>\n"+steps[c]+"\nLocation: "+WardenQuest.Maps[c].Replace("Scene3","Scene 3");
        return "<b>MAIN QUEST  •  "+(c+1)+" / 6</b>\n<b>"+WardenQuest.Titles[c]+"</b>\n\n"+status+"\n\n<b>Reward</b>\n"+WardenQuest.RewardText(c);
    }
    public static string Teacher(SaveSlots.Record s,int giver)
    {
        string name=new[]{"Alden","Rowan","Mira"}[giver];int done=LearningQuests.Completed(s,giver);
        string task=done==2?"<color=#24552A><b>REWARD READY</b></color>\nSpeak to "+name+" to collect it.":"Foundation lessons: "+done+" / 2\n"+Topic(giver*2)+"\n"+Topic(giver*2+1);
        return "<b>"+name.ToUpperInvariant()+"'S QUEST</b>\n"+LearningQuests.Title(giver)+"\n\n"+task+"\n\n<b>Reward</b>\n"+LearningQuests.Reward(giver)+" XP";
    }
    public static string All(SaveSlots.Record s)
    {
        var parts=new List<string>{Main(s)};
        for(int i=0;i<3;i++)if(LearningQuests.Accepted(s,i)&&!LearningQuests.Claimed(s,i))parts.Add(Teacher(s,i));
        if(s!=null&&s.elfQuestStage>0&&s.elfQuestStage<4)parts.Add("<b>EARLIER CAVE CONTRACT</b>\n"+ElfQuestProgress.Objective(s));
        return string.Join("\n\n<color=#80603C>────────────────────</color>\n\n",parts);
    }
}
