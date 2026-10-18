# Mage artwork and animation

The playable mage uses DuskBorne-ArchDemon from Assets/Sprites/DuskBorne-ArchDemon/SpriteSheets. Select Mage (saved character index 2) in the main menu.

Idle uses all six hovering frames. Movement uses a faster hovering cycle; jump, fall and dash use short pose sequences from the same asset, because it has no dedicated walk or airborne sheets. The shared visual polish component adds restrained lean, takeoff stretch, landing recovery and a small hovering offset, while respecting pause and dialogue. The player root, physics and interactions use the existing movement system.

Battle casting uses all 15 original attack cells with a charge hold, fast spell release and recovery. Each cell is imported as a single sprite including its disconnected magic effects, rather than treating spell fragments as separate character frames. Sprite pivots keep a shared body anchor; point filtering, no compression and a 4096 maximum attack texture size preserve the original pixels. The body scale matches the warrior, allowing the halo to extend above it.

The starter deck contains Arcane Bolt, Fireball, Arcane Surge, Armor Up, Draw and Healing Potion. Character selection, battle setup and inventory resolve the mage from the same saved selection.

Resources: Mage.asset, MageMovement.controller, MageIdle/Run/Jump/Fall/Dash/Attack.anim. A standalone Mage.prefab is also available.
