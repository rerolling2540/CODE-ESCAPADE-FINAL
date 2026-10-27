using UnityEngine;

[RequireComponent(typeof(Npc_Talk))]
public sealed class SyntaxMerchantNPC : MonoBehaviour,INPCDialogueProvider
{
    public DialogueSO BuildDialogue(Npc_Talk host)
    {
        var greeting=host.Node("Welcome, traveller. Spare cards need not gather dust. Trade them for Library Tokens, or browse the syntax I have gathered for your class.");
        var help=host.Node("One spare card earns one Library Token. You can also recycle it into three low-logic scraps, then exchange three scraps for one token. Your last copy and its refinement level are protected.",
            "Coins buy card copies. Tokens buy syntax shards and scholar titles. Shards buy an advanced card for your chosen hero. Purchased cards join your battle deck, and you can take duplicates to the blacksmith.");
        System.Action open=()=>
        {
            var inventory=FindFirstObjectByType<CharacterInventory>();
            if(inventory==null){var player=FindFirstObjectByType<playerMovement>();if(player!=null)inventory=player.gameObject.AddComponent<CharacterInventory>();}
            if(inventory==null)return;
            DialogueManager.Instance?.EndDialogue();SyntaxMerchantUI.Show(inventory,this);
        };
        greeting.options=new[]{new DialogueOption{optionText="Browse and trade",onSelected=open},new DialogueOption{optionText="How does trading work?",nextDialogue=help},new DialogueOption{optionText="Farewell"}};
        help.options=new[]{new DialogueOption{optionText="Open the market",onSelected=open},new DialogueOption{optionText="Back",nextDialogue=greeting}};
        return greeting;
    }
}
