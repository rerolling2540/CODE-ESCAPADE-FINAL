# Original character almanac

The almanac is available only during exploration in Scene 2, Scene3, and Scene4. Press L or click its button to read it. It is intentionally unavailable in the lobby, main menu, character selection, and battle scenes.

Warrior studies Java, Archer studies C#, and Mage studies Python. OriginalLessons.json contains 90 authored lessons: 30 per language, with 10 beginner, 10 intermediate, and 10 advanced pages.

Every page includes a topic definition, purpose and usage guidance, a complete worked program, its output, a walkthrough, a separate practice program, and a revealable answer with reasoning. Practice is self-check study material, not a graded battle question. Examples use original traveler-themed values and names rather than the question-bank examples.

The Supabase question bank was read only to identify curriculum topics. The almanac no longer downloads or displays question-bank rows and does not need a database key. Battle questions and Supabase data are unchanged. Lessons are local and always available offline.

Opening the book pauses gameplay and hides gameplay overlays. Closing it restores the previous time scale. Leaving exploration hides and closes it. Reading does not change save slots, inventories, rewards, or quiz results.

Implementation: Assets/Script/belo/CharacterAlmanac.cs. ExplorationHUD creates the almanac; CharacterSelector no longer creates a launcher.
