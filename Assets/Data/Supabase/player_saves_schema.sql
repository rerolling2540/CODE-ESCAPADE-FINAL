-- Run once in the Supabase SQL Editor for cross-device player saves.
-- Each signed-in user can read their own save row. The protected RPC performs writes.
create table if not exists public.player_saves (
    user_id uuid primary key references auth.users(id) on delete cascade,
    save_data jsonb not null,
    updated_at timestamptz not null default now()
);

alter table public.player_saves enable row level security;

drop policy if exists "Players can manage their own saves" on public.player_saves;
drop policy if exists "Players can read their own saves" on public.player_saves;
create policy "Players can read their own saves"
    on public.player_saves
    for select
    to authenticated
    using (auth.uid() = user_id);

revoke insert, update, delete on public.player_saves from authenticated;
grant select on public.player_saves to authenticated;
-- Run asap_database_fixes.sql after the base schemas to install the protected save RPC.
