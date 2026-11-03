# Eleonore and Joanna: combat contracts

Both are saved variants of Shared Dialogue NPC, use Npc_Talk and the existing shared DialogueCanvas, and have their supplied portraits and idle frames. Move their normal prefab instances in the scene hierarchy; HuntQuestAuthoring.Apply preserves existing placements and never spawns NPCs at runtime.

| NPC / map | Contract | Targets | XP | Gold | Extra |
|---|---|---|---:|---:|---|
| Eleonore / Scene 2 | A Safer Hollow | 3 different cave encounters (training excluded) | 100 | 100 | 2 low-logic scraps |
| Eleonore / Scene 2 | The Executioner's Watch | Undead Executioner Puppet | 180 | 175 | 3 low-logic scraps |
| Eleonore / Scene 2 | Banish the Bringer | Bringer of Death | 250 | 250 | 1 library token |
| Joanna / Scene3 | Shadows on the Trail | 4 different Demon / Blood Monster encounters | 150 | 150 | 3 low-logic scraps |
| Joanna / Scene3 | Quiet the Wings | 3 different bat encounters | 200 | 200 | 4 low-logic scraps |
| Joanna / Scene3 | The Flying Tyrant | Flying Demon | 300 | 300 | 2 library tokens |

Accept each contract, win its listed battles, and return to the same NPC to claim the reward. Speak again to accept the next contract. Earlier victories count, preventing a previously defeated one-time boss from blocking progress. Progress uses permanent exact encounter IDs; repeated kills, failed battles, and the map's respawning visit counters do not add extra targets. Existing battle XP remains separate.

Acceptance and claimed XP receipts are saved per slot, independent of character language. Inventory grants have separate durable receipts to prevent duplicate currency/material rewards after an interrupted save. Old saves with absent contract fields start at the first offer and retain other progress.

Active contracts appear in the journal, with progress counts, locations and rewards. Available/active/reward-ready markers use the shared world and minimap rendering. Settings and modal overlays hide markers and prompts. The existing safe-ground NPC return system includes both NPCs; the reward selection panel now scrolls to support up to seven simultaneous rewards.

New fields travel through the existing JSON save snapshot. No database migration is needed; live cloud synchronization has not been tested for these fields.
