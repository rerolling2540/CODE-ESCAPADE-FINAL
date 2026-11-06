using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>One continuous, slot-owned campaign. Teaching receipts remain language-specific.</summary>
public static class WardenQuest
{
    public const string FinalEncounter = "Warden Final Trial";
    public static readonly string[] Titles={"Prepare for the Hollow","First Trial","Restore the Gate","Rekindle the Watchfires","The Scholar's Supplies","Warden of the Hollow"};
    public static readonly string[] Maps={"Whispering Hollow","Whispering Hollow","Shadowroot Depths","Shadowroot Depths","Frostbound Passage","Forest of Dawn"};
    public static readonly int[] XP={50,75,100,125,150,200};
    public static readonly int[] Gold={50,50,100,100,150,200};
    public static int Chapter(SaveSlots.Record s)=>Mathf.Clamp(s?.wardenChapter??0,0,6);
    public static string Proof(string id)=>LearningQuests.Language+"/"+id;
    public static bool Has(SaveSlots.Record s,string id)=>s?.wardenProofs?.Contains(Proof(id))==true;
    public static bool Lesson(SaveSlots.Record s,int topic)=>s?.workshopExercisesCompleted?.Contains(WorkshopProgression.Id(LearningQuests.Language,topic,0))==true || s?.learningQuestProofs?.Any(p=>p.StartsWith(LearningQuests.Language+"-teacher-")&&p.EndsWith("/"+VisualCodeLessons.Topics[topic]))==true;
    public static bool Ready(SaveSlots.Record s)
    {
        if(s==null||!s.wardenAccepted)return false;
        switch(Chapter(s)) {
            case 0:return Lesson(s,0)&&Lesson(s,1)&&Has(s,"supplies");
            case 1:return s.defeatedEncounters?.Contains(BattleTutorialGuide.EncounterId)==true&&Has(s,"tutorial-report");
            case 2:return Lesson(s,2)&&Has(s,"gate");
            case 3:return Lesson(s,3)&&Has(s,"fires");
            case 4:return Lesson(s,4)&&Lesson(s,5)&&Has(s,"shelf")&&Has(s,"heal");
            case 5:return Enumerable.Range(0,6).All(t=>Lesson(s,t))&&s.defeatedEncounters?.Contains(FinalEncounter)==true&&Has(s,"final-report");
            default:return false;
        }
    }
    public static void Record(string id)
    {
        var s=SaveSlots.Read(SaveSlots.Active);if(s==null)return;
        if(s.wardenProofs==null)s.wardenProofs=new List<string>();
        if(!Has(s,id)){s.wardenProofs.Add(Proof(id));SaveSlots.Write(s);}
    }
    public static string RewardText(int c)=>XP[c]+" XP, "+Gold[c]+" gold"+(c==3?", 3 low-logic scraps":c==4?", 1 library token":c==5?", 2 library tokens and the Warden title":"");
    public static string Objective(SaveSlots.Record s)
    {
        int c=Chapter(s);if(c==6)return "Warden of the Hollow: complete. Your title is earned; teachers remain available for review.";
        if(s==null||!s.wardenAccepted)return "Warden of the Hollow: speak to the elf in Scene 2 [E] to begin.";
        return "Chapter "+(c+1)+"/6: "+Titles[c]+"\n"+QuestLearningRoute.Next(s)+"\nReward: "+RewardText(c)+".";
    }
    public static void Accept()
    {
        var s=SaveSlots.Read(SaveSlots.Active);if(s==null)return;
        if(s.workshopExercisesCompleted==null)s.workshopExercisesCompleted=new List<string>();
        for(int topic=0;topic<6;topic++)if(WorkshopProgression.Passed(LearningQuests.Language,topic,0))
        {string id=WorkshopProgression.Id(LearningQuests.Language,topic,0);if(!s.workshopExercisesCompleted.Contains(id))s.workshopExercisesCompleted.Add(id);}
        s.wardenAccepted=true;SaveSlots.Write(s);
    }
    public static bool ApplyReward(SaveSlots.Record s,int expected)
    {
        if(Chapter(s)!=expected||!Ready(s))return false;
        PlayerProgression.ApplyQuestExperience(s,XP[expected]);s.wardenChapter++;
        return true;
    }
    public static bool Claim(int expected)
    {
        var s=SaveSlots.Read(SaveSlots.Active);if(Chapter(s)!=expected||!Ready(s))return false;
        var inv=Object.FindFirstObjectByType<CharacterInventory>();if(inv==null||!inv.IsCurrentSlot)return false;
        // Inventory grant has its own durable receipt. A crash between saves can retry safely.
        inv.GrantWardenReward(expected,Gold[expected],expected==3?3:0,expected==4?1:expected==5?2:0);
        if(!ApplyReward(s,expected))return false;SaveSlots.Write(s);return true;
    }
    public static string PendingBattleReview(SaveSlots.Record s)
    {
        if(s==null||!s.wardenAccepted)return null;
        int c=Chapter(s);
        string encounter=c==1?BattleTutorialGuide.EncounterId:c==5?FinalEncounter:null;
        string proof=c==1?"tutorial-report":c==5?"final-report":null;
        return encounter!=null&&s.defeatedEncounters?.Contains(encounter)==true&&!Has(s,proof)?proof:null;
    }
    public static bool ApplyBattleReview(SaveSlots.Record s)
    {
        string proof=PendingBattleReview(s);if(proof==null)return false;
        if(s.wardenProofs==null)s.wardenProofs=new List<string>();
        s.wardenProofs.Add(Proof(proof));return true;
    }
    static void FinishBattleReview()
    {
        var s=SaveSlots.Read(SaveSlots.Active);
        if(ApplyBattleReview(s))SaveSlots.Write(s);
    }
    public static DialogueSO Dialogue(Npc_Talk host)=>Dialogue(host,SaveSlots.Read(SaveSlots.Active));
    public static DialogueSO Dialogue(Npc_Talk host,SaveSlots.Record s)
    {
        int c=Chapter(s);
        if(c==6)return host.Node("You are Warden of the Hollow. You learned to read code, follow its changing values, and apply it to new problems. Your teachers will still welcome you for practice.");
        var root=host.Node("I travel ahead to tend the wards. Wherever you meet me, we will continue the same journey; you do not need to start again.",Objective(s));
        if(s==null||!s.wardenAccepted)root.options=new[]{new DialogueOption{optionText="Accept: Warden of the Hollow",onSelected=Accept},new DialogueOption{optionText="Not yet"}};
        else if(Ready(s))root.options=new[]{new DialogueOption{optionText="Claim chapter reward",onSelected=()=>Claim(c)},new DialogueOption{optionText="Later"}};
        else if(PendingBattleReview(s)!=null)
        {
            // One-time victories survive an interrupted report. Review them without replaying the enemy.
            string summary=c==1?
                "Your training victory already counts. Review: play a card, answer its code question, and check the effect. End your turn when ready; enemies then act. Read the battle report to understand mistakes and revisit the matching almanac topic.":
                "Your final trial victory already counts. Review: output shows messages, variables hold changing values, conditions choose a path, loops repeat instructions, arrays and lists use indexed positions, and functions turn inputs into results. Revisit your teachers for any topic you want to practice.";
            var review=host.Node(summary,"Your saved victory does not need to be repeated. Finish this review, then speak to me again for your remaining objective or chapter reward.");
            review.options=new[]{new DialogueOption{optionText="Finish battle review",onSelected=FinishBattleReview},new DialogueOption{optionText="Review later"}};
            root.options=new[]{new DialogueOption{optionText=c==1?"Review my training victory":"Review my final trial victory",nextDialogue=review},new DialogueOption{optionText="Farewell"}};
        }
        else {
            int task=c==0?0:c==2?1:c==3?2:c==4?(!Has(s,"shelf")?3:4):-1;
            var help=host.Node("First read the lesson, then watch one line at a time. Predict what changes. If you are unsure, replay the example or open the almanac [L]. Hints and retries are free; you do not need perfect accuracy.","Alden is in Scene 2. Rowan is in Scene3. Mira is in Map 4. Completed foundation lessons count even if you did them before accepting this chapter.");
            root.options=task>=0&&host.gameObject.scene.name==Maps[c]?new[]{new DialogueOption{optionText="Try the chapter challenge",nextDialogue=WardenChallenge.Build(host,task)},new DialogueOption{optionText="Learning route / help",nextDialogue=help},new DialogueOption{optionText="Farewell"}}:new[]{new DialogueOption{optionText="Learning route / help",nextDialogue=help},new DialogueOption{optionText="Farewell"}};
        }
        return root;
    }
}
