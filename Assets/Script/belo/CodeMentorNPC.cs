using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Npc_Talk))]
public sealed class CodeMentorNPC : MonoBehaviour, INPCDialogueProvider
{
    [Range(0,2)] public int workshop;
    public string Role => new[]{"Scrollkeeper","Gate Warden","Potion Scholar"}[Mathf.Clamp(workshop,0,2)];
    public DialogueSO BuildDialogue(Npc_Talk host)
    {
        string lang=CharacterAlmanac.LanguageForCharacter(PlayerPrefs.GetInt("SelectedCharacter",0));
        var root=host.Node("Welcome to the "+Role+"'s workshop. Your hero studies "+CharacterAlmanac.LanguageName(lang)+". Let us turn code into something you can see.",LearningQuests.Objective(workshop)+" Ask about your learning quest before solving its predictions.");
        var about=host.Node("Follow this foundation route: output, variables, conditions, loops, arrays or lists, then functions. Complete each foundation prediction to unlock the next topic. You can preview later lessons safely.","Every topic has Foundation, Applied and Challenge exercises. Solve the easier exercise to unlock the next variation. The visual demonstration remains a foundation scaffold; Applied and Challenge ask you to use it on new code. Completed exercises remain available for review.","Watch one instruction at a time. The highlighted line changes the boxes, gate, or scroll beside it. Back and Replay let you retrace every step.",
            "Then predict a different example. Only your prediction is recorded for learning guidance; demonstrations do not count as correct answers. These are guided code traces, not a code editor.");
        root.options=new[]{new DialogueOption {optionText="Enter visual workshop",onSelected=()=>{
            DialogueManager.Instance?.EndDialogue();
            var ui=GetComponent<CodeWorkshopUI>();if(ui==null)ui=gameObject.AddComponent<CodeWorkshopUI>();ui.Open(this);
        }},new DialogueOption {optionText="Learning quest / XP",nextDialogue=LearningQuests.Dialogue(host,workshop)},new DialogueOption {optionText="How do I learn here?",nextDialogue=about}};
        about.options=new[]{new DialogueOption {optionText="Back",nextDialogue=root}};
        return root;
    }
    public int SuggestedTopic(string language)
    {
        int first=Mathf.Clamp(workshop,0,2)*2;
        var history=QuestionPerformanceTracker.GetLearningHistory(language,SaveSlots.Active);
        // Weak topics take priority; otherwise start with the first uncompleted prediction.
        for(int i=first;i<first+2;i++)
            if(AdaptiveQuestionSelector.Summarize(history.Where(a=>a.topic==VisualCodeLessons.Topics[i])).NeedsReview)return i;
        for(int i=first;i<first+2;i++)
            if(!history.Any(a=>a.question_id=="mentor-"+language+"-"+VisualCodeLessons.Topics[i]+"-prediction"&&a.outcome=="correct"))return i;
        return first;
    }
}
