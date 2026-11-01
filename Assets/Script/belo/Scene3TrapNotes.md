# Scene 3 trap passages

All new traps are selectable prefab instances under **Scene 3 Traps** in `Assets/Scenes/Scene3.unity`.
Their placements follow Scene 2: floor-mounted fire vents, ceiling-mounted worms, and a grouped electrical line with its own switch.

| Group | Location | First-pass mechanic |
| --- | --- | --- |
| 01 - Fire Timing | x 160–164, upper corridor | Two vents alternate. Each rests for 3 seconds, flashes for 0.6 seconds, then burns for 2 seconds. The second starts 1.4 seconds later in the cycle; both are clear together for 1.6 seconds. |
| 02 - Ceiling Worms | x 153–159, middle corridor | Both retract for 3.2 seconds, flash for 0.6 seconds, then attack for 1.4 seconds. Cross after they retract. |
| 03 - Kuryinti Pulse Gate | x 222–228, lower corridor | Three bolts share a 3-second safe opening, 0.6-second warning, and 2.2-second active period. Its dedicated puzzle switch permanently disables this circuit. |
| 05 - Ceiling Crusher | x 29, left cave passage | One prefab instance rests for 3 seconds, warns for 0.6 seconds, then drops and retracts over 1.4 seconds. Cross while it rests. Deals 18 damage only at the moving plate; chains are harmless. |

The new traps deal 18 damage per accepted hit and retain the player's normal damage cooldown.
The hanging spikes on **tinek 2** deal 5 damage. Upward-pointing floor spikes are on
**tinek 2 - Bottom Spikes (50 damage)** and deal 50 damage. Both use the same spike-hit sound.
Bottom-spike hits also poison the player for 5 seconds, dealing 5 HP each second (25 extra HP).
Further accepted spike hits refresh the duration without stacking poison or delaying its next tick.
A warning reading only **POISONED** blinks every 0.4 seconds at the top of the existing health HUD; it clears on expiry, death, or disable.
Poison pauses with the game, dialogue, and scene transitions, and never applies to the ceiling spikes.
Green floor spikes and selectable static toxic canisters under **04 - Poison Spike Markers** identify the hazard.
The canister body holds the supplied Toxic Trap Level 1 strip's first frame, matching its authored scene appearance.
Its separate **Bubbles** child loops the strip's upper bubble region at a gentle 6 FPS; only the bubbles animate.
Trap sounds retain the existing four-unit player-based hearing range.
Duplicate **Timed Ceiling Crusher 1** under **05 - Ceiling Crusher**, or use `Assets/Prefab/Traps/Scene 3 Timed Ceiling Crusher.prefab`.
The crusher uses the transparent PNG version of the Ceiling Trap Level 1 animation and **Mecha Geared Punch 001** for its drop impact, audible within four units. Its trigger follows the plate frame by frame.

Adjust **Timed Trap Cycle 2D** on an instance to refine the safe, warning, active, and offset timings.
The power-switch field is assigned only on the electrical gate; the clock cannot override a solved switch.
Inactive and warning phases disable damage and contact colliders. Attack animation frames still determine when an active trap can hurt.

## Authored Kuryinti circuits 4–9

Each group under **Scene 3 Traps** has its own matching **Kuryinti N Switch**, controlling only electrical segments inside that group.
These match Scene 2: powered initially, lethal electrical contact, a two-unit interaction range, and the shared ten-question puzzle fallback.
Solving a switch disables only its own circuit, including animation, contact damage, and colliders; each switch has a unique saved circuit identity.
Kuryinti 4 contains two segments; Kuryinti 5–9 each contain three. All authored trap and switch positions are preserved.
