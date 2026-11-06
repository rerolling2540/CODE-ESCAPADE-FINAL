using UnityEngine;

[RequireComponent(typeof(Npc_Talk))]
public sealed class HuntQuestNPC : MonoBehaviour, INPCDialogueProvider
{
    [Range(0,1)] public int giver;
    public DialogueSO BuildDialogue(Npc_Talk host)
    {
        var save=SaveSlots.Read(SaveSlots.Active);int stage=HuntQuestProgress.Stage(save,giver);
        if(stage>=HuntQuestProgress.Stages)return host.Node("You have completed all three of my contracts. The hollow is safer because of you. Your rewards have been recorded.","Keep studying with the teachers and continue the elf's main quest. You can revisit lessons whenever you need practice.");
        var c=HuntQuestProgress.Get(giver,stage);
        if(!HuntQuestProgress.Accepted(save,giver))
        {
            var accepted=host.Node("Contract accepted: "+c.title+". "+c.target+".",c.advice,"Track this contract in the quest journal [Q]. Return to me for "+HuntQuestProgress.Reward(c)+". Earlier victories in this save already count; losses and retreats do not. This combat quest is separate from the elf's learning journey.");
            var offer=host.Node("I am "+HuntQuestProgress.Name(giver)+". "+c.story,"Contract "+(stage+1)+" of 3: "+c.title+". "+c.target+".\nReward: "+HuntQuestProgress.Reward(c)+".");
            offer.options=new[]{new DialogueOption{optionText="Accept: "+c.title,nextDialogue=accepted,onSelected=()=>HuntQuestProgress.Accept(giver,stage)},new DialogueOption{optionText="Not yet"}};return offer;
        }
        var root=host.Node(c.story,HuntQuestProgress.Ready(save,giver)?"Your contract is complete. Claim "+HuntQuestProgress.Reward(c)+".":c.target+": "+HuntQuestProgress.Count(save,giver,stage)+" / "+c.required+". "+c.advice);
        var help=host.Node(c.advice,"Only victorious battles against the listed targets count. Different encounters count once each, even if Shadowroot Depths respawns them on a later visit. Earlier victories in this save count too.","Your quest reward is extra to normal battle XP. Gold buys supplies from the syntax merchant; low-logic scraps support card refinement. Library tokens can be spent at the merchant.");
        help.options=new[]{new DialogueOption{optionText="Back to my contract",nextDialogue=root}};
        root.options=HuntQuestProgress.Ready(save,giver)?new[]{new DialogueOption{optionText="Claim XP and rewards",onSelected=()=>{
            bool paid=HuntQuestProgress.Claim(giver,stage);
            var result=host.Node(paid?"Reward collected: "+HuntQuestProgress.Reward(c)+".":"I could not grant the reward yet. Your contract is still available; close this conversation and try again.",paid?(stage+1<HuntQuestProgress.Stages?"Speak to me again when you are ready for the next contract.":"You completed my final contract. Thank you for protecting these paths."):"Your reward will not be paid twice.");
            DialogueManager.Instance?.StartDialogue(result,host,host.actor);
        }},new DialogueOption{optionText="Contract details",nextDialogue=help},new DialogueOption{optionText="Claim later"}}:new[]{new DialogueOption{optionText="Where are my targets?",nextDialogue=help},new DialogueOption{optionText="I will return"}};
        return root;
    }
}
