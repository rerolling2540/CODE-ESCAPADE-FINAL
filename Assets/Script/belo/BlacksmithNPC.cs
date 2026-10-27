using UnityEngine;

[RequireComponent(typeof(Npc_Talk))]
public sealed class BlacksmithNPC : MonoBehaviour, INPCDialogueProvider
{
    public DialogueSO BuildDialogue(Npc_Talk host)
    {
        var greeting = host.Node("Welcome to the forge, traveller. Bring me spare copies of a card and I can strengthen every remaining copy of that card.");
        var help = host.Node("Cards can reach level 5. Each level adds 20% of the base damage and healing. Armor, burn, and draw effects gain one per level.",
            "Upgrading from level 1 costs one spare copy and 50 coins. Later upgrades need more copies and coins. I always leave you one copy, and the forge shows the exact cost before you confirm.");
        System.Action refine = () =>
        {
            var inventory = FindFirstObjectByType<CharacterInventory>();
            if (inventory == null)
            {
                var player = FindFirstObjectByType<playerMovement>();
                if (player != null) inventory = player.gameObject.AddComponent<CharacterInventory>();
            }
            if (inventory == null) return;
            DialogueManager.Instance?.EndDialogue();
            CardRefinementUI.Show(inventory, this);
        };
        greeting.options = new[] {
            new DialogueOption { optionText = "Refine my cards", onSelected = refine },
            new DialogueOption { optionText = "How does refinement work?", nextDialogue = help },
            new DialogueOption { optionText = "Farewell" }
        };
        help.options = new[] {
            new DialogueOption { optionText = "Open the forge", onSelected = refine },
            new DialogueOption { optionText = "Back", nextDialogue = greeting }
        };
        return greeting;
    }
}
