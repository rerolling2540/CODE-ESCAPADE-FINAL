# Enemy map progression

EnemyMapBalance applies the source exploration map when EnemyView is created. Shared EnemyData health and XP remain the fallback for standalone battle scenes. Player level does not increase an enemy's stats after revisiting an earlier map. Attack power, status effects, artwork and scene placements are unchanged.

| Source map | Normal enemy base HP | Base XP | Base gold |
| --- | ---: | ---: | ---: |
| Scene 2 | 70 | 45 | 15 |
| Scene3 | 120 | 85 | 30 |
| map 4 | 190 | 140 | 50 |
| map 5 | 270 | 210 | 75 |

EnemyData progressionWeight preserves differences between species. Elites multiply HP by 1.65 and rewards by 1.75; bosses multiply HP by 2.8 and rewards by 3. Final values are rounded to whole numbers. The Bringer of Death has a higher weight than the earlier Executioner, and the Flying Demon is a boss rather than an ordinary bat.

The tutorial goblin keeps 55 HP, 10 XP and low attack power, with 5 gold. Each of the two Warden learning-trial sentinels has 190 HP, 140 XP and 50 gold, for 280 XP and 100 gold per trial victory. They retain their low attack power.

Rewards are summed across the original enemy squad and granted only on encounter victory. Retreats, defeats and standalone test battles do not grant encounter gold. XP keeps its existing permanent first-clear rule. Gold is also a first-clear reward, with a per-save inventory receipt keyed by scene and encounter name. Respawning enemies do not pay repeatedly. Gold is saved before the victory receipt and shown in the battle report; it is added automatically to inventory rather than spawning a physical pickup. Older inventories receive the new receipt list when needed. No retroactive rewards are granted for enemies already absent from an old save.

Map 5's copied Demon Slime and Mimic encounter names now use Map 5 IDs, avoiding collisions with encounters in earlier maps. Existing quest-target names in Scene 2 and Scene3 remain intact.

Run EnemyMapBalanceChecks.Run() in Edit Mode for map curves, actual EnemyView setup, squad totals, unique encounter IDs, duplicate gold protection, legacy inventory loading, save reload and overflow checks. Full combat pacing still needs player testing with each hero and deck.
