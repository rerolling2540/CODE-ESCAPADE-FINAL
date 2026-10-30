-- Run once in the existing Supabase project SQL Editor after the inventory and performance schemas.

-- Preserves existing saves; slot numbers 1-10 use database indexes 0-9.

begin;

alter table public.player_card_inventory_slots drop constraint if exists player_card_inventory_slots_slot_index_check;

alter table public.player_card_inventory_slots add constraint player_card_inventory_slots_slot_index_check check (slot_index between 0 and 9);

alter table public.player_card_inventory drop constraint if exists player_card_inventory_slot_index_check;

alter table public.player_card_inventory add constraint player_card_inventory_slot_index_check check (slot_index between 0 and 9);

alter table public.player_question_attempts drop constraint if exists player_question_attempts_slot_index_check;

alter table public.player_question_attempts add constraint player_question_attempts_slot_index_check check (slot_index between 0 and 9);

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


commit;
