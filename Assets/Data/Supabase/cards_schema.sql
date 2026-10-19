-- Run this in the Supabase SQL Editor to create the table used by Card Data Editor.
-- Client writes are limited to signed-in users through RLS. Never put a service_role key in Unity.
create extension if not exists pgcrypto;

create table if not exists public.cards (
    id uuid primary key default gen_random_uuid(),
    name text not null,
    description text not null default '',
    mana integer not null default 0 check (mana >= 0),
    image_url text not null default '',
    damage_type integer not null default 0 check (damage_type in (0, 1)),
    base_damage integer not null default 10 check (base_damage >= 0),
    percent_damage numeric not null default 100 check (percent_damage >= 0),
    damage_multiplier numeric not null default 1 check (damage_multiplier >= 0),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

alter table public.cards enable row level security;

drop policy if exists "Authenticated users can manage cards" on public.cards;
create policy "Authenticated users can manage cards"
    on public.cards
    for all
    to authenticated
    using (true)
    with check (true);

grant select, insert, update, delete on public.cards to authenticated;
