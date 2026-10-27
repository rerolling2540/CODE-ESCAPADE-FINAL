# Syntax Merchant

Scene 2 contains a saved Syntax Merchant prefab instance beside the blacksmith. Select the root in the Hierarchy to move it. The merchant uses the elf's shared dialogue canvas and the same NPC interaction system; no runtime spawning is used.

Press E nearby, choose Browse and trade, then use the market tabs. Transactions require a second confirmation. Esc cancels a pending confirmation or closes the market. Back to merchant returns to the shared conversation. The market pauses exploration and hides the map, journal, and other gameplay overlays.

| Exchange | Result |
| --- | --- |
| 50 coins | One basic/support card copy for the selected hero |
| One spare card | One Library Token |
| Recycle one spare card | Three low-logic scraps |
| Three low-logic scraps | One Library Token |
| Three Library Tokens | One syntax shard |
| Four syntax shards | One advanced class card |
| Eight or twelve Library Tokens | One equippable scholar title cosmetic |

The last copy of a card cannot be traded or recycled. All remaining copies retain their refinement level. Buying another copy also retains that card type's existing level. Purchased cards and extra copies are included in the current hero's generated battle deck; cards for another class cannot be purchased. Card damage and support effects continue to use the existing refinement system.

Advanced cards are Runic Cleave for Warrior, Storm Volley for Archer, and Arcane Tempest for Mage. Their assets are in Assets/Resources/MerchantCards. They use the existing battle question flow for the hero's language and level.

Ember Scholar and Azure Scholar are cosmetic titles shown above the character during exploration. They grant no combat bonuses. A purchased title is equipped automatically; select an owned title in the Cosmetics tab to equip or hide it without another charge.

Cards, currencies, owned titles, and the equipped title save together in the active inventory slot. Existing inventories without merchant fields initialize them to zero/empty. The existing cloud-save inventory snapshot includes these fields without a new table or migration.

Stock and prices are configured in Assets/Resources/NPC/SyntaxMerchantCatalog.asset. Keep offer IDs and card IDs stable after release, because saves refer to them. The merchant prefab is Assets/Prefab/NPC/Syntax Merchant.prefab. Its aligned idle frames use the supplied Merchant/Gipsy spritesheet.png, preserving the originally imported sprite slices.
