# Class Card Library

The 90 class cards in this folder are implemented as playable `CardData` assets: 30 each for Warrior/Java, Archer/C#, and Mage/Python.

Each card is registered in `Resources/InventoryCardCatalog` and offered for purchase by the Syntax Merchant, filtered to its character class. Buying a card adds it to the player's saved inventory and battle deck. Cards in this library are not added to new players' starting decks; those stay at five cards.

The card effects use the existing battle systems for damage, healing, armor, drawing, mana, and status effects. Some attacks apply Burn, Poison, Bleed, or Frost; others use status combos. Status stacks follow the current battle status rules.

`Assets/Data/Supabase/class_card_library_seed.sql` adds matching master rows to `public.cards` so purchased card inventory can sync to Supabase. Run it after `card_inventory_schema.sql`. Unity's `CardData` assets hold the executable effects; the Supabase card rows hold editable catalog fields and stable inventory keys.
