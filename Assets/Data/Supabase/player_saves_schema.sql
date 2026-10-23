-- Run once in the Supabase SQL Editor for cross-device player saves.
-- Each signed-in user can read and write only their own save row.
create table if not exists public.player_saves (
    user_id uuid primary key references auth.users(id) on delete cascade,
    save_data jsonb not null,
    updated_at timestamptz not null default now()
);

alter table public.player_saves enable row level security;

drop policy if exists "Players can manage their own saves" on public.player_saves;
create policy "Players can manage their own saves"
    on public.player_saves
    for all
    to authenticated
    using (auth.uid() = user_id)
    with check (auth.uid() = user_id);

grant select, insert, update, delete on public.player_saves to authenticated;
