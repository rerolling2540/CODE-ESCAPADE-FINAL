-- Run after cards_schema.sql in Supabase SQL Editor.
-- `cards` is the editable master catalog. `player_card_inventory` stores the
-- owned copies in each of the game's three save slots.

alter table public.cards
    add column if not exists game_card_key text;

do $$
begin
    if not exists (
        select 1 from pg_constraint
        where conname = 'cards_game_card_key_unique'
          and conrelid = 'public.cards'::regclass
    ) then
        alter table public.cards
            add constraint cards_game_card_key_unique unique (game_card_key);
    end if;
end $$;

-- These stable keys match InventoryCardCatalog, ChestCards and the merchant
-- catalog in Unity. Re-running this seed will preserve edits to existing rows.
insert into public.cards
    (game_card_key, name, description, mana, damage_type, base_damage, percent_damage, damage_multiplier)
values
('831451ed55fb384468dd7c7b07521312', 'Arrow Rain', 'Deal 65% Attack Power to all enemies.', 2, 1, 0, 65, 1),
('c0030000000000000000000000000003', 'Piercing Shot', 'Deal 150% Attack Power to one random enemy.', 2, 1, 0, 150, 1),
('c0040000000000000000000000000004', 'Quick Draw', 'Deal 75% Attack Power and draw 1 card.', 1, 1, 0, 75, 1),
('c0050000000000000000000000000005', 'Arcane Bolt', 'Deal 120% Attack Power to one random enemy.', 1, 1, 0, 120, 1),
('168c58146a32b25448627cb38164f1f3', 'Arcane Surge', 'Deal 180% Attack Power to one random enemy.', 3, 1, 0, 180, 1),
('980c18db59e9cf946920510c6e295487', 'Fireball', 'Deal 90% Attack Power to all enemies and apply 2 Burn.', 2, 1, 0, 90, 1),
('1725032973d00d54e9f4a9bdd0aab211', 'Armor Up', 'Add 5 Armor.', 1, 0, 0, 0, 1),
('a49b409c12903a740a4dfb32f653356a', 'Draw', 'Draw 2 cards.', 1, 0, 0, 0, 1),
('632aebffcbd6c0f42acd79eb887af739', 'Healing Potion', 'Restore 20 health.', 1, 0, 0, 0, 1),
('c0060000000000000000000000000006', 'Second Wind', 'Restore 12 health and draw 1 card.', 2, 0, 0, 0, 1),
('8e41b321c7264f369bc01e2d750ac106', 'Battle Focus', 'Draw 2 cards.', 1, 0, 0, 0, 1),
('0661d68524fd7ae47bf5bce5128c636e', 'Cleave', 'Deal 80% Attack Power to all enemies.', 2, 1, 0, 80, 1),
('8e41b321c7264f369bc01e2d750ac104', 'Crippling Blow', 'Deal 90% Attack Power and apply 2 Burn to all enemies.', 2, 1, 0, 90, 1),
('8e41b321c7264f369bc01e2d750ac105', 'Guarded Strike', 'Gain 5 Armor and deal 75% Attack Power to one random enemy.', 2, 1, 0, 75, 1),
('8e41b321c7264f369bc01e2d750ac101', 'Heavy Strike', 'Deal 150% Attack Power to one random enemy.', 2, 1, 0, 150, 1),
('96684006bfd95d94caf0267932ec6d1c', 'Iron Guard', 'Gain 8 Armor.', 1, 0, 0, 0, 1),
('8e41b321c7264f369bc01e2d750ac107', 'Second Wind', 'Restore 14 health and gain 3 Armor.', 2, 0, 0, 0, 1),
('3d24bedb84993bd4bb7bc07779a0ac40', 'Shield Bash', 'Deal 12 base damage to one random enemy.', 1, 0, 12, 0, 1),
('8e41b321c7264f369bc01e2d750ac103', 'Shield Wall', 'Gain 12 Armor.', 2, 0, 0, 0, 1),
('8e41b321c7264f369bc01e2d750ac102', 'Whirlwind', 'Deal 60% Attack Power to all enemies.', 2, 1, 0, 60, 1),
('chest.Hollow Tempest', 'Hollow Tempest', 'Deal 100% Attack Power to all enemies.', 2, 1, 0, 100, 1),
('chest.Trailblazer''s Insight', 'Trailblazer''s Insight', 'Draw 3 cards.', 1, 0, 0, 0, 1),
('chest.Warden''s Aegis', 'Warden''s Aegis', 'Gain 10 Armor.', 1, 0, 0, 0, 1),
('merchant.card.advanced.0', 'Runic Cleave', 'Advanced Warrior card. Deal 110% Attack Power to all enemies.', 2, 1, 0, 110, 1),
('merchant.card.advanced.1', 'Storm Volley', 'Advanced Archer card. Deal 90% Attack Power to all enemies.', 2, 1, 0, 90, 1),
('merchant.card.advanced.2', 'Arcane Tempest', 'Advanced Mage card. Deal 120% Attack Power to all enemies and apply 2 Burn.', 2, 1, 0, 120, 1)
on conflict (game_card_key) do nothing;

create table if not exists public.player_card_inventory_slots (
    user_id uuid not null references auth.users(id) on delete cascade,
    slot_index smallint not null check (slot_index between 0 and 9),
    updated_at timestamptz not null default now(),
    primary key (user_id, slot_index)
);

create table if not exists public.player_card_inventory (
    user_id uuid not null references auth.users(id) on delete cascade,
    slot_index smallint not null check (slot_index between 0 and 9),
    game_card_key text not null references public.cards(game_card_key) on update cascade on delete restrict,
    quantity integer not null check (quantity > 0),
    level integer not null default 1 check (level between 1 and 5),
    primary key (user_id, slot_index, game_card_key),
    foreign key (user_id, slot_index)
        references public.player_card_inventory_slots(user_id, slot_index)
        on delete cascade
);

alter table public.player_card_inventory_slots enable row level security;
alter table public.player_card_inventory enable row level security;

drop policy if exists "Players can read their card inventory slots" on public.player_card_inventory_slots;
create policy "Players can read their card inventory slots"
    on public.player_card_inventory_slots for select to authenticated
    using (auth.uid() = user_id);

drop policy if exists "Players can read their owned cards" on public.player_card_inventory;
create policy "Players can read their owned cards"
    on public.player_card_inventory for select to authenticated
    using (auth.uid() = user_id);

grant select on public.player_card_inventory_slots, public.player_card_inventory to authenticated;

-- Replaces exactly one save slot's card stacks atomically. The authenticated
-- caller can only replace rows belonging to their own auth.uid().
create or replace function public.replace_player_card_inventory(
    p_slot_index integer,
    p_cards jsonb,
    p_updated_at text default null
)
returns void
language plpgsql
security definer
set search_path = public
as $$
begin
    if auth.uid() is null then
        raise exception 'Sign in required';
    end if;
    if p_slot_index < 0 or p_slot_index > 9 then
        raise exception 'Invalid save slot';
    end if;
    if coalesce(jsonb_typeof(p_cards), '') <> 'array' then
        raise exception 'Card stacks must be a JSON array';
    end if;

    insert into public.player_card_inventory_slots (user_id, slot_index, updated_at)
    values (auth.uid(), p_slot_index, coalesce(nullif(p_updated_at, '')::timestamptz, now()))
    on conflict (user_id, slot_index)
    do update set updated_at = excluded.updated_at;

    delete from public.player_card_inventory
    where user_id = auth.uid() and slot_index = p_slot_index;

    insert into public.player_card_inventory (user_id, slot_index, game_card_key, quantity, level)
    select auth.uid(), p_slot_index, item.card_key, item.quantity, coalesce(item.level, 1)
    from jsonb_to_recordset(p_cards) as item(card_key text, quantity integer, level integer)
    where item.card_key is not null and item.quantity > 0;
end;
$$;

revoke all on function public.replace_player_card_inventory(integer, jsonb, text) from public;
grant execute on function public.replace_player_card_inventory(integer, jsonb, text) to authenticated;
