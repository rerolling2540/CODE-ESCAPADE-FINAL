# Mirelight Crossing

Open `Assets/Scenes/Mirelight Crossing.unity` and press Play.

A standalone swamp route built from Fantasy Swamp Forest: seven mossy land masses, eight raised canopy islands, rope crossings, layered parallax forest, reflective marsh water and firefly accents. The lower path leads to the Keeper shrine; the high path offers a more demanding double-jump route.

Controls use the existing Warrior prefab: A/D or arrow keys to move, W/Space to jump and double-jump, Left Shift to dash. Four lamp waypoints activate recovery positions when approached. Falling into the marsh returns the player to the last visited waypoint. Checkpoints reset when Play mode ends.

The Grid contains separate Ground, Canopy Islands, Rope Walkways and decoration tilemaps. The three solid maps use static Rigidbody2D, TilemapCollider2D and merged CompositeCollider2D geometry. Solid objects carry the Ground tag required by the existing controller.

`MirelightJourney` owns only this scene's camera tracking and recovery. Backgrounds reuse `ParallaxBackground`. Village Props supplies lamps, signs, the shrine, campfire and decorative chests. Chests and the destination shrine are scenery, with no loot or scene-transition hookup. This scene is not added to the game's build or portal progression.

Validation: live Play mode frames advanced; the Warrior settled on terrain; checkpoint 3 recovered the player after a water fall; no console errors during the check. A complete manual traversal of both routes remains useful for tuning jump difficulty.
