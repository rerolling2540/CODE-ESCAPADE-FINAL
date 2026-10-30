-- Run once in the Supabase SQL Editor.
-- Adds question topic tags and private per-account question attempt history.

alter table public.question_bank
    add column if not exists topic text not null default 'general';

create index if not exists question_bank_topic_lookup_idx
    on public.question_bank (language, topic, difficulty) where published;

update public.question_bank set topic = case
    when id like '%-e01' then 'output'
    when id like '%-e02' then 'variables'
    when id like '%-e03' then 'strings'
    when id like '%-e04' then 'conditionals'
    when id like '%-e05' then 'arrays'
    when id like '%-e06' then 'methods'
    when id like '%-e07' then 'loops'
    when id like '%-e08' then 'syntax'
    when id like '%-e09' then 'comparisons'
    when id like '%-e10' then 'types'
    when id like '%-m01' then 'loops'
    when id like '%-m02' then 'strings'
    when id like '%-m03' then 'arrays'
    when id like '%-m04' then 'operators'
    when id like '%-m05' then 'strings'
    when id like '%-m06' then 'arrays'
    when id like '%-m07' then 'methods'
    when id like '%-m08' then 'conditionals'
    when id like '%-m09' then 'loops'
    when id like '%-m10' then 'arrays'
    when id like '%-h01' or id like '%-h10' then 'recursion'
    when id like '%-h02' then 'algorithms'
    when id like '%-h03' or id like '%-h06' or id like '%-h08' then 'collections'
    when id like '%-h04' then 'objects'
    when id like '%-h05' then 'exceptions'
    when id like '%-h07' then 'operators'
    when id like '%-h09' then 'loops'
    else topic
end
where topic = 'general' and id ~ '^(java|csharp|python)-(e|m|h)[0-9]{2}$';

create table if not exists public.player_question_attempts (
    attempt_id uuid primary key,
    user_id uuid not null references auth.users(id) on delete cascade,
    slot_index integer not null check (slot_index between 0 and 9),
    question_id text not null,
    topic text not null default 'general',
    language text not null check (language in ('java', 'csharp', 'python')),
    difficulty text not null check (difficulty in ('easy', 'medium', 'hard')),
    source text not null check (source in ('battle', 'door', 'almanac')),
    player_level integer not null check (player_level >= 1),
    outcome text not null check (outcome in ('correct', 'incorrect', 'timed_out', 'abandoned')),
    presented_at timestamptz not null,
    answered_at timestamptz,
    duration_seconds real not null default 0 check (duration_seconds >= 0)
);

create index if not exists player_question_attempts_report_idx
    on public.player_question_attempts (user_id, topic, presented_at desc);
create index if not exists player_question_attempts_slot_idx
    on public.player_question_attempts (user_id, slot_index, presented_at desc);

alter table public.player_question_attempts enable row level security;
revoke all on public.player_question_attempts from anon;
drop policy if exists "Players can read their own question history" on public.player_question_attempts;
create policy "Players can read their own question history"
    on public.player_question_attempts for select to authenticated
    using (auth.uid() = user_id);
drop policy if exists "Players can add their own question history" on public.player_question_attempts;
create policy "Players can add their own question history"
    on public.player_question_attempts for insert to authenticated
    with check (auth.uid() = user_id);
drop policy if exists "Players can update their own question history" on public.player_question_attempts;
create policy "Players can update their own question history"
    on public.player_question_attempts for update to authenticated
    using (auth.uid() = user_id)
    with check (auth.uid() = user_id);

grant select, insert, update on public.player_question_attempts to authenticated;
