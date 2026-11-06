using UnityEngine;

/// <summary>Authored, constrained code challenges; no arbitrary source-code execution.</summary>
public static class WardenChallenge
{
    public static readonly string[] IDs={"supplies","gate","fires","shelf","heal"};
    public static DialogueSO Build(Npc_Talk host,int task,bool hinted=false)
    {
        var s=SaveSlots.Read(SaveSlots.Active);int first=task==0?0:task==1?2:task==2?3:4;
        if(!WardenQuest.Lesson(s,first)||(task==0&&!WardenQuest.Lesson(s,1))||(task==4&&!WardenQuest.Lesson(s,5)))
            return host.Node("Before trying this task, complete the matching foundation lesson. "+new[]{"Visit Alden in Whispering Hollow for output and variables.","Visit Rowan in Shadowroot Depths for conditions.","Visit Rowan in Shadowroot Depths for loops.","Visit Mira in Frostbound Passage for arrays or lists.","Visit Mira in Frostbound Passage for functions."}[task]+" Your earlier completed lessons count.");
        bool py=LearningQuests.Language=="python",cs=LearningQuests.Language=="csharp";
        string print=py?"print":""+(cs?"Console.WriteLine":"System.out.println");
        string[] code={py?"coins = 3\ncoins = coins + 4\nprint(coins)":"int coins = 3;\ncoins = coins + 4;\n"+print+"(coins);",py?"keys = 2\nif keys __ 2:\n    print('Open')":"int keys = 2;\nif (keys __ 2) { "+print+"(\"Open\"); }",py?"for tower in range(__):\n    print('Light')":"for (int tower = 0; tower < __; tower++) {\n    "+print+"(\"Light\");\n}",py?"potions = [3, 6, 9]\nprint(potions[__])":"int[] potions = {3, 6, 9};\n"+print+"(potions[__]);",py?"def heal(hp):\n    return hp + 3\nprint(heal(__))":"static int Heal(int hp) { return hp + 3; }\n"+print+"(Heal(__));"};
        string[] prompt={"Predict the final coins shown. Follow the value in the box after each line.","Fill __ so the gate opens when you have at least two keys, including exactly two.","Fill __ to light exactly four watchfires. The counter starts at zero.","Fill __ to select the potion with value 6. Shelf positions start at zero.","Fill __ with the starting health so the healing function returns 8."};
        string[] hint={"The box starts with 3 coins. Add 4 and replace its old value. Print reads the latest value.","> means greater than. >= means greater than or equal to. The rule must work for 2, 3, and more keys.","The counter visits 0, 1, 2, 3. It stops before the limit. Count how many visits that makes.","The positions are 0 -> 3, 1 -> 6, 2 -> 9. Choose a position, not the potion's value.","The function adds 3 to its input. Which starting number plus 3 gives 8?"};
        string[][] answers={new[]{"7","3"},new[]{">=",">"},new[]{"4","3"},new[]{"1","6"},new[]{"5","8"}};
        int correct=task%2;var choices=new string[2];choices[correct]=answers[task][0];choices[1-correct]=answers[task][1];
        var question=host.Node(prompt[task],code[task]);
        var success=host.Node("Correct. "+hint[task]+" The ward responds to your code. Your progress is saved. Return to the elf for the next objective or reward.");
        var retry=host.Node("Not quite. "+hint[task]+"\nYou can try again without losing health or quest rewards.");
        foreach(var node in new[]{question,success,retry})node.learningVisualProof=IDs[task];
        retry.options=new[]{new DialogueOption{optionText="Try again",nextDialogue=question},new DialogueOption{optionText="Review later"}};
        question.options=new[]{new DialogueOption{optionText=choices[0],nextDialogue=correct==0?success:retry,onSelected=()=>Submit(task,correct==0,hinted)},new DialogueOption{optionText=choices[1],nextDialogue=correct==1?success:retry,onSelected=()=>Submit(task,correct==1,hinted)},new DialogueOption{optionText=hinted?"Leave and review":"Show a hint",nextDialogue=hinted?null:Hint(host,task,prompt[task],code[task],hint[task],correct,choices,success)}};
        return question;
    }
    static DialogueSO Hint(Npc_Talk host,int task,string prompt,string code,string hint,int correct,string[] choices,DialogueSO success)
    {
        var q=host.Node(prompt,"Hint: "+hint,code);
        var wrong=host.Node("Not quite. "+hint+" Try again, or review the matching workshop lesson.");
        q.learningVisualProof=wrong.learningVisualProof=IDs[task];
        wrong.options=new[]{new DialogueOption{optionText="Try again",nextDialogue=q},new DialogueOption{optionText="Review later"}};
        q.options=new[]{new DialogueOption{optionText=choices[0],nextDialogue=correct==0?success:wrong,onSelected=()=>Submit(task,correct==0,true)},new DialogueOption{optionText=choices[1],nextDialogue=correct==1?success:wrong,onSelected=()=>Submit(task,correct==1,true)},new DialogueOption{optionText="Review later"}};return q;
    }
    static void Submit(int task,bool correct,bool hinted)
    {
        // Completion is distinct from statistical mastery. Hinted successes stay identifiable.
        var save=SaveSlots.Read(SaveSlots.Active);
        bool assisted=hinted||WardenQuest.Has(save,IDs[task]+"-retry");
        WardenQuest.Record(IDs[task]+(correct?(assisted?"-assisted":"-independent"):"-retry"));
        if(correct)WardenQuest.Record(IDs[task]);
    }
}
