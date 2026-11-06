-- Run this entire file once in the existing project's Supabase SQL Editor.
-- Requires the existing cards, player_saves, inventory and question-performance tables.
-- Re-runnable. Preserves existing rows, ownership, account IDs and saves.
begin;

-- Replace permissive card-write policies, including any older differently named policy.
do $$
declare policy_row record;
begin
    for policy_row in select polname from pg_policy
        where polrelid = 'public.cards'::regclass and polcmd in ('a','w','d','*')
    loop
        execute format('drop policy %I on public.cards', policy_row.polname);
    end loop;
end $$;
drop policy if exists "Signed-in players can read cards" on public.cards;
create policy "Signed-in players can read cards" on public.cards
    for select to authenticated using (true);
create policy "Admin can manage cards" on public.cards
    for all to authenticated
    using (lower(coalesce(auth.jwt()->>'email','')) = 'adeventurerank60@gmail.com')
    with check (lower(coalesce(auth.jwt()->>'email','')) = 'adeventurerank60@gmail.com');
alter table public.cards enable row level security;
revoke insert, update, delete, truncate, references, trigger on public.cards from anon;
revoke truncate, references, trigger on public.cards from authenticated;
grant select, insert, update, delete on public.cards to authenticated;

-- Inventory writes must go through the version-checked RPC. TRUNCATE bypasses
-- RLS, so explicitly remove any older/default table-wide grants as well.
revoke all on public.player_card_inventory_slots, public.player_card_inventory from anon;
revoke insert, update, delete, truncate, references, trigger
    on public.player_card_inventory_slots, public.player_card_inventory from authenticated;
grant select on public.player_card_inventory_slots, public.player_card_inventory to authenticated;

-- Bring existing three-slot projects up to the client's ten slots.
alter table public.player_card_inventory_slots drop constraint if exists player_card_inventory_slots_slot_index_check;
alter table public.player_card_inventory_slots add constraint player_card_inventory_slots_slot_index_check check (slot_index between 0 and 9);
alter table public.player_card_inventory drop constraint if exists player_card_inventory_slot_index_check;
alter table public.player_card_inventory add constraint player_card_inventory_slot_index_check check (slot_index between 0 and 9);
alter table public.player_question_attempts drop constraint if exists player_question_attempts_slot_index_check;
alter table public.player_question_attempts add constraint player_question_attempts_slot_index_check check (slot_index between 0 and 9);

-- Same merge rule as SaveSlots.MergeCloudSnapshots: newer per-slot timestamps win,
-- including an empty string with a timestamp (a deleted slot). Ties keep the server.
create or replace function public.merge_player_save_snapshots(p_local jsonb, p_cloud jsonb)
returns jsonb language plpgsql set search_path = public as $$
declare
    merged jsonb;
    slots jsonb := '[]'; inventories jsonb := '[]'; stamps jsonb := '[]';
    local_time timestamptz; cloud_time timestamptz;
    local_global timestamptz; cloud_global timestamptz;
    chosen jsonb; i integer;
begin
    p_local := coalesce(p_local, '{}'::jsonb);
    p_cloud := coalesce(p_cloud, '{}'::jsonb);
    local_global := nullif(p_local->>'modifiedUtc','')::timestamptz;
    cloud_global := nullif(p_cloud->>'modifiedUtc','')::timestamptz;
    merged := case when cloud_global is null or local_global > cloud_global then p_local else p_cloud end;
    for i in 0..9 loop
        local_time := nullif(p_local->'slotModifiedUtc'->>i,'')::timestamptz;
        cloud_time := nullif(p_cloud->'slotModifiedUtc'->>i,'')::timestamptz;
        chosen := case when local_time > cloud_time
            or (local_time is not null and cloud_time is null)
            or (local_time is null and cloud_time is null and coalesce(p_cloud->'slots'->>i,'') = '')
            then p_local else p_cloud end;
        slots := slots || jsonb_build_array(coalesce(chosen->'slots'->>i,''));
        inventories := inventories || jsonb_build_array(coalesce(chosen->'inventories'->>i,''));
        stamps := stamps || jsonb_build_array(coalesce(chosen->'slotModifiedUtc'->>i,''));
    end loop;
    return merged || jsonb_build_object('slots',slots,'inventories',inventories,'slotModifiedUtc',stamps);
end $$;
revoke all on function public.merge_player_save_snapshots(jsonb,jsonb) from public;

drop function if exists public.sync_player_save(jsonb);
create or replace function public.sync_player_save(p_save_data jsonb, p_base_slot_timestamps jsonb default '[]'::jsonb)
returns jsonb language plpgsql security definer set search_path = public as $$
declare saved jsonb; merged jsonb; field_name text; i integer;
    base_stamp text; saved_stamp text; incoming_stamp text;
    conflicts jsonb := '[]';
begin
    if auth.uid() is null then raise exception 'Sign in required'; end if;
    if jsonb_typeof(p_save_data) is distinct from 'object' then
        raise exception 'Save snapshot must be an object';
    end if;
    foreach field_name in array array['slots','inventories','slotModifiedUtc'] loop
        if jsonb_typeof(p_save_data->field_name) is distinct from 'array' then
            raise exception 'Missing save array: %', field_name;
        end if;
        if jsonb_array_length(p_save_data->field_name) <> 10 then
            raise exception 'Save arrays must contain ten slots';
        end if;
    end loop;
    if jsonb_typeof(p_base_slot_timestamps) is distinct from 'array' then
        raise exception 'Observed save versions must be an array';
    end if;
    -- Insert or lock before reading. Concurrent devices merge against the latest
    -- committed row, never against an earlier GET response.
    insert into public.player_saves(user_id,save_data) values(auth.uid(),'{}')
        on conflict(user_id) do nothing;
    select save_data into saved from public.player_saves where user_id = auth.uid() for update;
    merged := public.merge_player_save_snapshots(p_save_data,saved);
    for i in 0..9 loop
        base_stamp := coalesce(p_base_slot_timestamps->>i,'');
        saved_stamp := coalesce(saved->'slotModifiedUtc'->>i,'');
        incoming_stamp := coalesce(p_save_data->'slotModifiedUtc'->>i,'');
        -- A device edited a slot from an out-of-date baseline. Keep the server
        -- branch even if that device's clock/timestamp is later, and report it
        -- so Unity can preserve the conflicting branch in a local backup.
        if saved_stamp <> base_stamp and incoming_stamp <> base_stamp and incoming_stamp <> saved_stamp
            and (coalesce(p_save_data->'slots'->>i,'') <> coalesce(saved->'slots'->>i,'')
                or coalesce(p_save_data->'inventories'->>i,'') <> coalesce(saved->'inventories'->>i,'')) then
            conflicts := conflicts || jsonb_build_array(i);
            merged := jsonb_set(merged,array['slots',i::text],to_jsonb(coalesce(saved->'slots'->>i,'')));
            merged := jsonb_set(merged,array['inventories',i::text],to_jsonb(coalesce(saved->'inventories'->>i,'')));
            merged := jsonb_set(merged,array['slotModifiedUtc',i::text],to_jsonb(saved_stamp));
        end if;
    end loop;
    update public.player_saves set save_data = merged, updated_at = now() where user_id = auth.uid();
    return jsonb_build_object('save_data',merged,'conflicted_slots',conflicts);
end $$;
revoke all on function public.sync_player_save(jsonb,jsonb) from public;
grant execute on function public.sync_player_save(jsonb,jsonb) to authenticated;

-- Old clients must not bypass the merge through a direct REST upsert/delete.
alter table public.player_saves enable row level security;
drop policy if exists "Players can manage their own saves" on public.player_saves;
drop policy if exists "Players can read their own saves" on public.player_saves;
create policy "Players can read their own saves" on public.player_saves
    for select to authenticated using (auth.uid() = user_id);
revoke insert, update, delete, truncate, references, trigger on public.player_saves from anon, authenticated;
grant select on public.player_saves to authenticated;

-- Atomic normalized inventory replacement with stale-write protection.
create or replace function public.replace_player_card_inventory(
    p_slot_index integer, p_cards jsonb, p_updated_at text default null
)
returns void language plpgsql security definer set search_path = public as $$
declare existing_time timestamptz; snapshot_time timestamptz; incoming_time timestamptz;
begin
    if auth.uid() is null then raise exception 'Sign in required'; end if;
    if p_slot_index is null or p_slot_index < 0 or p_slot_index > 9 then raise exception 'Invalid save slot'; end if;
    if jsonb_typeof(p_cards) is distinct from 'array' then raise exception 'Card stacks must be a JSON array'; end if;
    incoming_time := nullif(p_updated_at,'')::timestamptz;
    if incoming_time is null then raise exception 'Inventory timestamp required'; end if;

    -- Same row lock as save synchronization; no upload can race a newer deletion.
    select nullif(save_data->'slotModifiedUtc'->>p_slot_index,'')::timestamptz
        into snapshot_time from public.player_saves where user_id = auth.uid() for update;
    perform pg_advisory_xact_lock(hashtextextended(auth.uid()::text || ':' || p_slot_index,0));
    select updated_at into existing_time from public.player_card_inventory_slots
        where user_id = auth.uid() and slot_index = p_slot_index for update;
    if snapshot_time is null or incoming_time <> snapshot_time or incoming_time < existing_time then
        raise exception 'Newer inventory exists; download and merge before uploading' using errcode = '40001';
    end if;
    insert into public.player_card_inventory_slots(user_id,slot_index,updated_at)
        values(auth.uid(),p_slot_index,incoming_time)
        on conflict(user_id,slot_index) do update set updated_at = excluded.updated_at;
    delete from public.player_card_inventory where user_id = auth.uid() and slot_index = p_slot_index;
    insert into public.player_card_inventory(user_id,slot_index,game_card_key,quantity,level)
        select auth.uid(),p_slot_index,item.card_key,item.quantity,coalesce(item.level,1)
        from jsonb_to_recordset(p_cards) as item(card_key text,quantity integer,level integer)
        where item.card_key is not null and item.quantity > 0;
end $$;
revoke all on function public.replace_player_card_inventory(integer,jsonb,text) from public;
grant execute on function public.replace_player_card_inventory(integer,jsonb,text) to authenticated;
commit;

-- Read-only confirmation of the installed changes.
select 'ASAP fixes installed: admin card writes, protected cloud saves, ten slots' as result;
