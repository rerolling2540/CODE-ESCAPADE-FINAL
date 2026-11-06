-- Five selectable bundled icons. Run in Supabase SQL Editor.
-- Images remain bundled in Unity; these rows identify them without external URLs.
begin;
create table if not exists public.card_icon_choices (
  id text primary key,
  name text not null,
  icon_template_key text not null unique
);
alter table public.card_icon_choices enable row level security;
revoke all on public.card_icon_choices from anon, authenticated;
grant select on public.card_icon_choices to anon, authenticated;
drop policy if exists "Read card icon choices" on public.card_icon_choices;
create policy "Read card icon choices" on public.card_icon_choices for select to anon, authenticated using(true);
insert into public.card_icon_choices(id,name,icon_template_key) values
 ('sword','Sword','8b9db185c6ae5ef44a465f972ebea930'),
 ('fire','Fireball','1535b2e54c2048bfb878c6b12d00e87b'),
 ('ice','Ice Spear','b1812b95b62b482c9189c59dcaf01a1f'),
 ('heal','Healing Light','bf08b862ca3d4f0b9b0ef7ef8a1e2d43'),
 ('arrows','Arrow Rain','0cd9f73eee97459fa6dd88a01f8b8777')
on conflict(id) do update set name=excluded.name,icon_template_key=excluded.icon_template_key;
commit;
select name from public.card_icon_choices order by name;
