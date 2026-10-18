# Archer artwork

The playable archer uses DuskBorne-Elf from Assets/Sprites/DuskBorne-Elf/SpriteSheet.

- Idle: six frames at 8 fps, with subtle breathing during exploration.
- Run: eight frames at 10 fps, scaled by movement speed through the animator.
- Battle attack: seven frames with a held draw, quick release and eased recovery; total duration 0.73 seconds. The clip and HeroData use matching timing.
- Jump and fall: short three-pose sequences that hold their final pose, adapted from the walking frames because the pack has no dedicated airborne animation.
- Dash: a 0.35-second sequence with fast opening frames and slower recovery, scaled to the configured dash duration.

ArcherAnimationPolish adds small, smoothly interpolated visual adjustments: forward lean during running and dashing, takeoff stretch (including double jump), and landing squash weighted by fall speed. The artwork pivots at the feet. These adjustments affect only the child visual transform, respect pause and dialogue, and reset when disabled.

The three source sprite sheets use point filtering, no compression, and 16 pixels per unit to match the warrior in battle. Their frame IDs and rectangles are preserved; pivots share the original 160-pixel cell anchor and the foot baseline to prevent sliding when the bow extends. Exploration scales only the artwork to the existing player capsule.

The character selector, archer prefab, battle HeroData, and all archer animation clips reference DuskBorne-Elf. Movement, double jump, dash, cards, dialogue, and saves use the existing shared gameplay systems.
