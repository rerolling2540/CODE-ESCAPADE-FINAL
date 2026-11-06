using System.Collections.Generic;
using System.Linq;

/// <summary>Read-only quest steps shared by dialogue and the journal. Never awards progress.</summary>
public static class QuestLearningRoute
{
    public sealed class Step
    {
        public readonly string title, action;
        public readonly bool done;
        public Step(string title,string action,bool done){this.title=title;this.action=action;this.done=done;}
    }
    public static Step Lesson(SaveSlots.Record save,int topic)
    {
        string teacher=new[]{"Alden in Scene 2","Rowan in Scene 3","Mira in Map 4"}[topic/2];
        string subject=QuestJournalText.Topic(topic);
        return new Step(subject,"Visit "+teacher+". Open "+subject+" > Foundation; watch the example, then answer its practice question.",WardenQuest.Lesson(save,topic));
    }
    public static List<Step> Chapter(SaveSlots.Record save)
    {
        var steps=new List<Step>();
        switch(WardenQuest.Chapter(save))
        {
            case 0:
                steps.Add(Lesson(save,0));steps.Add(Lesson(save,1));
                steps.Add(new Step("Apply it: count the supplies","Speak to the elf in Scene 2 and choose Try the chapter challenge. Work out the new coin count.",WardenQuest.Has(save,"supplies")));break;
            case 1:
                steps.Add(new Step("Win the training battle","Find the Tutorial Training Goblin in Scene 2. Play a card, answer its question, then end your turn when ready.",save?.defeatedEncounters?.Contains(BattleTutorialGuide.EncounterId)==true));
                steps.Add(new Step("Read the battle report","Read your answers in the battle report and press Continue. For an older victory, ask the elf to review it.",WardenQuest.Has(save,"tutorial-report")));break;
            case 2:
                steps.Add(Lesson(save,2));steps.Add(new Step("Apply it: restore the gate","Speak to the elf in Scene 3. Choose the comparison that opens the gate with enough keys.",WardenQuest.Has(save,"gate")));break;
            case 3:
                steps.Add(Lesson(save,3));steps.Add(new Step("Apply it: light four watchfires","Speak to the elf in Scene 3. Choose how many times the loop must repeat.",WardenQuest.Has(save,"fires")));break;
            case 4:
                steps.Add(Lesson(save,4));steps.Add(new Step("Apply it: select a potion","Speak to the elf in Map 4. Choose the shelf position of the requested potion.",WardenQuest.Has(save,"shelf")));
                steps.Add(Lesson(save,5));steps.Add(new Step("Apply it: prepare a healing spell","Speak to the elf in Map 4. Choose the input that gives the requested healing result.",WardenQuest.Has(save,"heal")));break;
            case 5:
                for(int topic=0;topic<6;topic++)steps.Add(Lesson(save,topic));
                steps.Add(new Step("Win the Warden Final Trial","Use a travel monolith to reach Map 5 and defeat the Warden Final Trial. Its questions draw on your six foundation topics.",save?.defeatedEncounters?.Contains(WardenQuest.FinalEncounter)==true));
                steps.Add(new Step("Review the final battle","Read the final battle report and press Continue. If your victory is already saved, ask the elf to review it.",WardenQuest.Has(save,"final-report")));break;
        }
        return steps;
    }
    public static string Next(SaveSlots.Record save)
    {
        if(WardenQuest.Chapter(save)==6)return "Your journey is complete. Teachers remain available for practice.";
        if(save==null||!save.wardenAccepted)return "Speak to the elf in Scene 2 and accept Warden of the Hollow.";
        return Chapter(save).FirstOrDefault(s=>!s.done)?.action??"Speak to the elf to claim your chapter reward.";
    }
    public static string Checklist(IEnumerable<Step> steps)=>string.Join("\n",steps.Select(s=>(s.done?"<color=#24552A>[DONE]</color> ":"[  ] ")+s.title));
}
