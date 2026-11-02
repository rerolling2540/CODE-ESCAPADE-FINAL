# Warden of the Hollow: learning journey

## Saved scene placements
- Scene 2: Alden stays at the user's chosen position. A new Elf - Warden Guide stands near the beginning; the original cave elf remains in place.
- Scene3: Rowan and an elf guide are near the return entrance.
- map 4: Mira and an elf guide are near the Scene3 entrance.
- map 5: an elf guide, a final training encounter, and a return route to map 4.

All NPCs are selectable prefab instances using the existing shared DialogueCanvas. Moves and new placements are saved in the scene files. Warden Journey prefabs are in Assets/Prefab/NPC/Warden Journey.

## Main chapters and rewards
| Chapter | Completion | Reward |
| --- | --- | --- |
| Prepare for the Hollow | Output and variables foundations; fresh elf supply prediction | 50 XP, 50 gold |
| First Trial | Tutorial goblin victory and sealed battle report (elf review available for older victories) | 75 XP, 50 gold |
| Restore the Gate | Conditions foundation and gate comparison challenge | 100 XP, 100 gold |
| Rekindle the Watchfires | Loops foundation and four-repeat challenge | 125 XP, 100 gold, 3 low-logic scraps |
| The Scholar's Supplies | Arrays/lists and functions foundations; shelf index and healing-input challenges | 150 XP, 150 gold, 1 library token |
| Warden of the Hollow | All six foundations; final trial victory and sealed report | 200 XP, 200 gold, 2 library tokens, Warden title |

Teacher quest rewards remain separate: Alden 60 XP, Rowan 80 XP, Mira 100 XP. Completed foundation exercises count, including those finished before quest acceptance. The new main quest does not restore the removed elf workshop quest.

Rewards use the existing gold, refinement-material, and token economy. Card-choice rewards and healing consumables were not added: those need dedicated selection/consumable systems. Tokens can be used with the existing merchant. The Warden title displays when no merchant title is equipped.

## Learning mechanics
- Teacher books retain their manual page turns, visual step replay, foundation/applied/challenge variations, and previewable later topics.
- The elf provides new constrained application tasks: predict a variable's final value, choose a comparison operator, complete a loop limit, select an index, and supply a function input.
- Instructions, code, and hints are separate dialogue pages. Players can advance with left click, E, or Space.
- Wrong answers explain the relevant rule and offer free retries. Hinted answers and answers after a wrong attempt are stored as assisted completion, separately from independent completion. Quest completion does not set statistical topic mastery.
- Scene-authored visual aids show coin, gate, watchfire, shelf, and healing states. Watchfires use animated Tiny Swords particle frames. These are constrained demonstrations, not an arbitrary-code interpreter.
- The final encounter rotates through applied examples from the six taught topics, in the character's language. It keeps card question tier boundaries and player-level tier caps. Two low-damage training sentinels (70 HP / 3 attack each) provide an approachable capstone; balance still needs a full player walkthrough.

## Saving and compatibility
- Main chapter and proofs belong to the active save slot; lesson and challenge proofs include the selected language.
- XP and chapter advancement save together. Inventory grants have chapter receipts to make interrupted claims safely retryable without paying inventory twice.
- Existing unfinished cave contracts remain available as Earlier cave contract; their stages, original rewards, and defeated-enemy history are preserved.
- Map 5 is enabled in Build Settings and supported by exploration/save checks. Its saved shared UI includes inventory, almanac, health, settings, quest journal, and an EventSystem; inherited missing menu callbacks were repaired.
- A saved route from map 4 to map 5 unlocks after claiming chapter 5. The return route is available for review. Scene3 also has a new return route to Alden in Scene 2.
- New visual aids hide during settings and other gameplay overlays.
- Save fields travel in the existing JSON snapshot path. No Supabase schema change was required; live cloud synchronization for these new fields has not been verified.

## Verification
- Unity compilation: no errors or warnings.
- WardenQuestChecks.Run(): 108 targeted Edit Mode checks, with no player saves modified. Covers chapter advancement, repeated claims, XP, save round trips, legacy fields, language isolation, inventory reward receipts/overflow, question validity, authored NPC placements, and Map 5 build inclusion.
- Full end-to-end gameplay and visual balance across all four maps still require a player walkthrough.


## Quest journal and reward return
- Repaired missing journal references and kept the icon stationary while the panel opens/closes.
- Main and teacher quests have separate headings, short next steps, reward-ready status, and reward sections.
- Journal body uses a larger clear font, bounded scrolling, and a responsive height so the footer stays onscreen.
- When a completed quest's NPC is more than 18 units away or on another map, a paused reward-return panel offers travel. It waits for dialogue/workshop closure.
- Return later dismisses the prompt for that session/context. Return to quest NPC in the journal reopens it. Multiple ready quests appear separately.
- Travel finds the authored NPC's current position, checks safe ground, stops velocity, updates camera/checkpoint/save, and does not claim rewards automatically. No NPC positions are changed.
- The panel and travel honor settings/overlay visibility and block battle triggering during transit.
- Verified: compilation; 36 journal/readiness/save checks; rendered maximum-five-quest panel with no overflowing text; long journal scrolling; safe landing searches near the Scene 2 elf and Alden without moving the player or writing saves.
- Full runtime cross-map teleport testing remains unverified.
