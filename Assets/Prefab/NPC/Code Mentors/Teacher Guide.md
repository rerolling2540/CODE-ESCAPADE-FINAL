# Visual code teachers

## Learning book interface

The workshop now opens an actual two-page book. Contents selects chapters and practice levels. Each chapter has Understand, Watch, Try, and Review spreads. The outer Previous page / Next page arrows (or keyboard Left/Right) turn paper across the centre spine. Demonstration Step controls and Replay affect code execution independently. Contents returns to chapter selection; Esc closes the book.

WorkshopBookTurn clones the outgoing sheet with its text, keeps the stationary old page visible, shows the destination under the moving sheet, and rotates the front/back faces with a fold shadow. The 0.6-second transition uses unscaled time and blocks overlapping navigation. Settings pause it while retaining normal CanvasGroup visibility. This replaces the old cutout reveal; the Paper UI asset supplies the sheet artwork, while the page-turn motion is implemented by the book controller. Older WorkshopPaperAnimation/reveal resources are no longer attached to this workshop.

Practice attempts start after the turn finishes. Leaving an unanswered practice spread abandons its attempt; opening explanation/demonstration pages does not create attempts. Correct answers retain the existing green check, progress receipts, learning-quest proof and one-time XP claim rules.

## Ordered workshop exercises

The six-topic foundation route is output → variables → conditions → loops → arrays/lists → methods/functions. Later topics may be previewed, but solving their prediction requires the preceding foundation exercise. Each topic has three original variations: Foundation, Applied, Challenge. Each harder variation requires the preceding variation of that same topic; review stays available. The beginner visual trace stays as a scaffold for all variations, while the prediction code and explanation change. This is a compact foundation curriculum inspired by step-by-step tutorial ordering, not a reproduction of the full W3Schools syllabus.

There are 54 exercises across the three languages. Stable question IDs distinguish the tiers and report them as easy/medium/hard to the existing tracker. Original foundation IDs and quest reward rules remain unchanged. Unlock receipts are saved in the slot record; earlier correct history and saved learning-quest proofs also preserve foundation completion. Foundation predictions fulfill the existing beginner NPC quests; harder variations provide additional practice without repeatable XP rewards. Almanac books now sort by difficulty before their existing level/topic order.

## Learning quests and XP

Speak to each teacher and choose **Learning quest / XP**, then accept before answering. Solve that teacher's two topic predictions and return to claim XP: Alden 60, Rowan 80, Mira 100. The elf's **Beginner learning quest / XP** asks for output and variables at Alden's workshop and awards 60 XP. Accepting both the elf's and Alden's quests lets the same two learning successes satisfy both accepted objectives. The elf still offers the existing Warden of the Hollow questline separately.

Wrong answers may be retried safely. Each topic counts once toward an accepted quest; a reward may be claimed once per language in the save slot. The quest journal lists active learning objectives and expands to fit them. Quest acceptance, solved-topic proofs, and reward receipts are part of the existing save record; XP and its receipt are written together. Existing saves default to no accepted learning quests. Cloud snapshots already carry serialized slot records, so no new SQL table is needed; live cloud sync remains unverified.

Three normal prefab instances are saved in **Scene 2**, along the tutorial route:

| Hierarchy name | Initial X | Lessons |
| --- | --- | --- |
| Alden - Scrollkeeper | -2 | Output, variables |
| Rowan - Gate Warden | 35 | Conditions, loops |
| Mira - Potion Scholar | 73 | Arrays/lists, methods/functions |

Move them directly in the hierarchy. Their placements are saved; nothing spawns or repositions them at runtime. They are variants of **Shared Dialogue NPC**, using `Npc_Talk`, `DialogueManager`, and the elf's shared dialogue settings. Each has a five-frame layered idle using the GandalfHardcore pack and a matching assembled portrait.

Approach and press **E**, then select **Enter visual workshop**. The teacher chooses Java for Warrior, C# for Archer, or Python for Mage. Every teacher supports all three hero languages (18 lesson variants total).

Use **Next step**, **Back**, and **Replay** to inspect highlighted instructions, changing state boxes, coins, a gate, trail counters, indexed cells, and function inputs/results. At the end, **Predict new code** presents a different example. **Esc** closes the workshop. The world pauses, gameplay overlays hide, and settings can hide/block the workshop through its parent CanvasGroup.

Within each teacher's two topics, existing weak-topic history takes priority; otherwise the first prediction not yet answered correctly is suggested. Both topics remain available for voluntary review. Predictive answers are recorded against the active account, save slot, language and canonical topic using the existing `almanac` source, so the existing adaptive selector and progress report can use them. Watching/replaying a trace records no correct answer. Leaving an unanswered prediction records abandonment. Repeat predictions keep the same question ID; they cannot satisfy the three-distinct-question mastery rule on their own.

These are authored teaching traces, not arbitrary code execution. Java/C# snippets include a note about their class/Main context. Python uses indentation and calls its indexed collection a list. Change lesson content and visual states in `VisualCodeLessons.cs`; change interaction/layout in `CodeWorkshopUI.cs`. Add future teachers from the Shared Dialogue NPC prefab rather than copying DialogueCanvas.

`CodeMentorAuthoring.CreateTeachers()` is an explicit Editor authoring helper for Scene 2. It skips teacher roles already present, preserving existing placements. It does not run automatically. `RefreshPortraits()` rebuilds their composite portraits. No additional Supabase schema is required; live cloud synchronization has not been verified for this change.
