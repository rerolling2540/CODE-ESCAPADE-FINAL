These unchanged sprites come from the project's Humble Gift - Paper UI System v1.1/Sprites/Content folder:

| Runtime texture | Source |
| --- | --- |
| Book | 1 Items/1.png |
| Quill | 1 Items/12.png |
| Scroll (message sheet alias) | 2 Icons/22.png |
| Coins | 1 Items/14.png |
| Complete | 2 Icons/10.png |
| Lock | 2 Icons/6.png |
| Banner | 10 Banners & Headers/Plain/1.png |

The learning book uses books for chapters, lock/check icons for progression, scroll/coin imagery for output and variable states, quill/book ornaments in the introduction, and ribbons for page headings. Artwork uses point filtering and never receives raycasts. All graphics remain children of the book pages and inherit their flips, opening/closing transform and settings visibility. Source pack artwork and NPC placements are unchanged.

The former Scroll source (1 Items/6.png) was a wood log. It has been replaced by the paper/message-sheet icon. The player-facing output title and visual state now say message sheet/messages; the runtime texture alias is retained for compatibility.

Additional original project assets:

- Key: Assets/RPG Icons/single/release_v1.2-single_88.png.
- Gate: Assets/RPG Icons/single/release_v1.2-single_33.png.
- Heal: Assets/RPG Icons/single/release_v1.2-single_38.png.
- Step: Assets/DungeonMonsters2D/UserInterface/Arrow_right_silver.png.
- Shelf: Humble Gift Content/2 Icons/14.png.

Each chapter now has a topic icon on Contents and Understand. Demonstrations use paper for messages, coins for variables, key and lock/check for conditions, arrows/checks for waiting/completed loop repeats, shelf cells for indexed values, and paper/health crosses for function inputs and health results. A small legend explains the imagery. The loop does not use an hourglass, which could incorrectly imply a question timer. Shelf cells are not represented by unrelated food or bottle sprites.
