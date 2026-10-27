# Shared NPC dialogue

All NPCs use the existing elf DialogueCanvas through DialogueManager. Do not add a separate dialogue canvas to each NPC.

To add a new NPC:

1. Drag Shared Dialogue NPC.prefab into the desired scene.
2. Replace the NPC Visual sprites/animation frames with that character's artwork, keeping its feet at the root position.
3. Create an ActorSO for its name and portrait, and assign it to Npc_Talk.actor.
4. Assign a DialogueSO to Npc_Talk.dialogueSO. Its lines and choices open in the shared canvas. The actor is used when a line has no separate speaker.
5. Keep the trigger collider enabled and marked Is Trigger. The player's Player-tagged collider enables talking in range; E starts the conversation.

For an NPC with a service, implement INPCDialogueProvider on a component attached to that same root. Return dialogue nodes through the host's Node method; it manages their lifetime. BlacksmithNPC is the example. DialogueManager records which NPC owns a conversation so leaving another NPC's trigger cannot close it.

NPC/SharedDialogueSettings in Resources references Assets/Prefab/DialogueCanvas.prefab, the original elf canvas. Scenes that already contain the canvas reuse it. A scene without one instantiates the same prefab when an NPC needs dialogue, and ensures a UI EventSystem exists.

The blacksmith prefab is in Assets/Resources/NPC/Blacksmith.prefab. Scene 2 contains a saved prefab instance named Blacksmith in its hierarchy. Select and move that root object to place the character; its origin is at the feet. Its position is not changed by a runtime spawner.

Always place new authored NPCs directly in the intended scene hierarchy as movable prefab instances. Save their placement and retain the user's chosen positions. Only use runtime spawning when explicitly requested.

The Syntax Merchant is also a saved scene prefab, located beside the blacksmith. It uses the same dialogue canvas and offers card purchases, spare-card trades, scraps, shards, and title cosmetics. See [Merchant Guide](<Merchant Guide.md>) for prices, stock configuration, and save behavior.

# Refinement rules

Each card type has a shared saved refinement level from 1 through 5 in the current save slot's inventory. Existing saves without a level start at 1. All remaining copies of that type use its saved level.

| Upgrade | Spare copies consumed | Coins |
| --- | --- | --- |
| 1 to 2 | 1 | 50 |
| 2 to 3 | 2 | 100 |
| 3 to 4 | 3 | 150 |
| 4 to 5 | 4 | 200 |

At least one copy remains. Consumed starter duplicates are removed from the generated battle deck. Damage and healing gain 20 percent of base power for each level above 1; armor, burn stacks, and draw amounts gain 1 per level. Mana costs and shared CardData assets stay unchanged. The forge shows a preview, requires confirmation, revalidates affordability, and saves the coin/copy deduction together with the new level. Cloud inventory snapshots include the refinement level automatically.
