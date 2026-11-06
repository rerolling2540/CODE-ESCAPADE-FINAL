# Monolith travel

The Portal Monolitic asset provides 63 point-filtered animation frames. Travel Monolith prefab instances are saved in Scene 2, Scene3, map 4, and map 5. Six monoliths replace the three added Warden gates and provide camp access near the teachers. Existing map puzzle portals remain intact.

Approach a monolith and press E, or click its prompt. The panel pauses gameplay and offers Scene 2, Scene 3, Map 4, Map 5, and Lobby. Travel is free. Previously visited maps are available, and claiming the elf's Map 4 chapter reward unlocks the first journey to Map 5. This quest unlock does not mark Map 5 as visited. Other undiscovered maps show a disabled visit-first label; locked Map 5 explains its chapter requirement. Lobby remains available. Discovery is stored per save slot in visitedScenes, recorded on exploration scene entry and saving, and checked again by the travel action. Older saves retain their known saved/checkpoint maps without assuming other maps were visited. Final-trial participation still follows its own quest prerequisites. The current map is marked You are here.

Destinations are configured in Assets/Resources/Travel/WorldTravelCatalog.asset. Each exploration map has exactly one world-monolith arrival anchor. New arrivals use the character's collider height and SafeCheckpoint ground checks, stop velocity, update the camera/checkpoint, and save the resulting position.

Settings takes priority over the travel panel. Gameplay HUD is hidden while the travel panel is open, and original CanvasGroup state is restored on close. Portal prompts respect journal occlusion and fit their label width.

Learning displays now follow the user's elf placement instead of remaining on their earlier map floor. They use larger labels and appear only during the matching NPC challenge dialogue, not merely from walking nearby. Opening an ordinary conversation, closing dialogue, leaving, disabling the display, or opening another overlay hides the illustrations. Shelf and healing displays alternate rather than overlap. TextMeshPro's blue editor icons are hidden; they were editor gizmos, not game objects. NPC positions were preserved.

Additional live-test fixes: HeroTM is marked Serializable for SerializeReference card effects. Landing smoke uses its actual 64x64 frame cells (13 frames), resolving the repeated missing-smoke warning.

Verification: Unity compiled without errors or warnings; 56 targeted scene/data/effect checks passed. The live portal interaction path opened the panel and paused gameplay with no label overflow. Travel Scene3 -> map 4 -> map 5 -> Scene 2 -> Scene3 reached safe ground in each map and reopened the panel. HUD hiding/restoration, settings priority, and resumed portal animation were checked in Play Mode. Scoped save/profile test data restoration was verified. Unity was returned to Edit Mode.


