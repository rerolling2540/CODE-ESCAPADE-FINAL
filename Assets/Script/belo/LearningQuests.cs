using System.Collections.Generic;
using UnityEngine;

/// <summary>Accepted objectives and reward receipts belong to the save, not transient quiz history.</summary>
public static class LearningQuests
{
    public static string Language=>CharacterAlmanac.LanguageForCharacter(PlayerPrefs.GetInt("SelectedCharacter",0));
    public static string Key(int giver)=>Language+"-teacher-"+giver;
    public static int Reward(int giver)=>giver<0?60:60+20*giver;
    public static string Title(int giver)=>giver<0?"First Steps in Code":new[]{"The Speaking Scroll","Trials of the Gate","The Scholar's Toolkit"}[giver];
    public static int FirstTopic(int giver)=>giver<0?0:giver*2;
    public static bool Accepted(SaveSlots.Record save,int giver)=>save?.learningQuestsAccepted?.Contains(Key(giver))==true;
    public static bool Claimed(SaveSlots.Record save,int giver)=>save?.learningQuestsClaimed?.Contains(Key(giver))==true;
    public static int Completed(SaveSlots.Record save,int giver)
    {
        int count=0;
        for(int i=FirstTopic(giver);i<FirstTopic(giver)+2;i++)
            if(save?.learningQuestProofs?.Contains(Key(giver)+"/"+VisualCodeLessons.Topics[i])==true)count++;
        return count;
    }
    public static void Accept(int giver)
    {
        var save=SaveSlots.Read(SaveSlots.Active);if(save==null)return;
        if(save.learningQuestsAccepted==null)save.learningQuestsAccepted=new List<string>();
        if(!Accepted(save,giver)){save.learningQuestsAccepted.Add(Key(giver));SaveSlots.Write(save);}
    }
    public static void RecordCorrect(string language,string topic)
    {
        if(language!=Language)return;
        var save=SaveSlots.Read(SaveSlots.Active);if(save==null)return;
        if(save.learningQuestProofs==null)save.learningQuestProofs=new List<string>();
        for(int giver=-1;giver<3;giver++)
        {
            if(!Accepted(save,giver)||Claimed(save,giver))continue;
            for(int t=FirstTopic(giver);t<FirstTopic(giver)+2;t++)
                if(VisualCodeLessons.Topics[t]==topic&&!save.learningQuestProofs.Contains(Key(giver)+"/"+topic))save.learningQuestProofs.Add(Key(giver)+"/"+topic);
        }
        SaveSlots.Write(save);
    }
    public static string Objective(int giver)
    {
        var save=SaveSlots.Read(SaveSlots.Active);
        if(Claimed(save,giver))return Title(giver)+": complete.";
        if(!Accepted(save,giver))return "Accept "+Title(giver)+" to begin. Reward: "+Reward(giver)+" XP.";
        if(Completed(save,giver)==2)return Title(giver)+": return to "+(giver<0?"the elf":new[]{"Alden","Rowan","Mira"}[giver])+" to claim "+Reward(giver)+" XP.";
        return Title(giver)+": solve "+VisualCodeLessons.Topics[FirstTopic(giver)]+" and "+VisualCodeLessons.Topics[FirstTopic(giver)+1]+" predictions ("+Completed(save,giver)+"/2).";
    }
    public static string Journal()
    {
        string result="";var save=SaveSlots.Read(SaveSlots.Active);
        for(int i=-1;i<3;i++)if(Accepted(save,i)&&!Claimed(save,i))result+="\n"+Objective(i);
        return result;
    }
    public static DialogueSO Dialogue(Npc_Talk host,int giver)
    {
        var save=SaveSlots.Read(SaveSlots.Active);
        string route="Your hero learns "+CharacterAlmanac.LanguageName(Language)+". Start with Alden: output shows messages and variables store values. Rowan teaches decisions and repetition. Mira teaches indexed collections and reusable functions. These skills help you predict the code in battle questions.";
        if(Claimed(save,giver))return host.Node(Title(giver)+" is complete. You have claimed its XP. You may replay every lesson for practice without spending anything.",route);
        if(!Accepted(save,giver))
        {
            var accepted=host.Node("Quest accepted: "+Title(giver)+". "+ObjectiveText(giver),"Watch the visual steps, then solve both different-topic predictions. Wrong answers have no health penalty; replay and try again. Return to me for "+Reward(giver)+" XP. Track your objectives in the quest journal [Q].");
            var offer=host.Node(route,"Quest: "+Title(giver)+". "+ObjectiveText(giver)+" Reward: "+Reward(giver)+" XP, claimed once for this language in this save.");
            offer.options=new[]{new DialogueOption{optionText="Accept learning quest",nextDialogue=accepted,onSelected=()=>Accept(giver)},new DialogueOption{optionText="Not yet"}};return offer;
        }
        if(Completed(save,giver)<2)return host.Node(Objective(giver),ObjectiveText(giver)+" Return after solving both predictions. Answers from before accepting do not count.");
        var thanks=host.Node("Quest completed: "+Title(giver)+". Your "+Reward(giver)+" XP has been added. Continue along the learning route, or practise again whenever you wish.");
        var claim=host.Node(Objective(giver));
        claim.options=new[]{new DialogueOption{optionText="Claim "+Reward(giver)+" XP",nextDialogue=thanks,onSelected=()=>PlayerProgression.ClaimLearningQuest(giver)},new DialogueOption{optionText="Later"}};return claim;
    }
    static string ObjectiveText(int giver)=>giver<0?"Visit Alden's visual workshop and correctly predict output and variables.":"Use my visual workshop and correctly predict "+VisualCodeLessons.Topics[FirstTopic(giver)]+" and "+VisualCodeLessons.Topics[FirstTopic(giver)+1]+".";
}
