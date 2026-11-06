-- Run AFTER asap_database_fixes.sql and current_cards_seed.sql.
-- Catalog updates never grant ownership or replace player saves.
begin;
alter table public.cards
  add column if not exists character_class text,
  add column if not exists source_pack text,
  add column if not exists template_key text,
  add column if not exists icon_template_key text,
  add column if not exists is_active boolean not null default true,
  add column if not exists shop_available boolean not null default false,
  add column if not exists shop_price integer not null default 100,
  add column if not exists minimum_question_tier integer default 1,
  add column if not exists maximum_question_tier integer default 3;

-- Normalize legacy catalog rows without deleting inventory references.
update public.cards set minimum_question_tier = greatest(1,least(3,coalesce(minimum_question_tier,1))),
  maximum_question_tier = greatest(1,least(3,coalesce(maximum_question_tier,3)));
update public.cards set maximum_question_tier=minimum_question_tier where maximum_question_tier<minimum_question_tier;
update public.cards set game_card_key='retired.'||id::text, is_active=false where nullif(trim(game_card_key),'') is null;
-- The original CARDS asset GUID is also its bundled gameplay template.
-- Only initialize once, so rerunning this migration preserves admin shop settings.
update public.cards set template_key=game_card_key, icon_template_key=game_card_key,
  shop_available=true where source_pack='CARDS' and template_key is null;
alter table public.cards alter column game_card_key set not null;
alter table public.cards alter column minimum_question_tier set not null;
alter table public.cards alter column maximum_question_tier set not null;
alter table public.cards alter column minimum_question_tier set default 1;
alter table public.cards alter column maximum_question_tier set default 3;
alter table public.cards drop constraint if exists cards_content_bounds;
alter table public.cards add constraint cards_content_bounds check (
  minimum_question_tier between 1 and 3 and maximum_question_tier between minimum_question_tier and 3 and shop_price>0);

-- Both guests and signed-in players download definitions. Ownership stays private.
alter table public.cards enable row level security;
drop policy if exists "Public game catalog" on public.cards;
create policy "Public game catalog" on public.cards for select to anon,authenticated using(true);
grant select on public.cards to anon,authenticated;
revoke delete,truncate,references,trigger on public.cards,public.question_bank from anon,authenticated;
revoke insert,update on public.cards,public.question_bank from anon;
grant insert,update on public.cards,public.question_bank to authenticated;
-- Existing ASAP admin RLS policies remain in force for INSERT/UPDATE.

create or replace function public.preserve_content_identity() returns trigger
language plpgsql set search_path=public as $$
begin
  if new.id is distinct from old.id then raise exception 'Content ID cannot change; create a new entry instead'; end if;
  if tg_table_name='cards' then
    if to_jsonb(new)->>'game_card_key' is distinct from to_jsonb(old)->>'game_card_key' then
      raise exception 'Card ownership key cannot change; retire the card instead';
    end if;
  end if;
  return new;
end $$;
drop trigger if exists preserve_card_identity on public.cards;
create trigger preserve_card_identity before update on public.cards for each row execute function public.preserve_content_identity();
drop trigger if exists preserve_question_identity on public.question_bank;
create trigger preserve_question_identity before update on public.question_bank for each row execute function public.preserve_content_identity();
commit;
select 'Content flow ready: catalog sync, retirement, question unpublishing' as result;
