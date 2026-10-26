-- Run once in Supabase SQL Editor for an existing question_bank table.
-- The Unity editor sends the signed-in user's JWT; RLS permits only this admin.
grant select, insert, update, delete on public.question_bank to authenticated;

drop policy if exists "Admin can manage question bank" on public.question_bank;
create policy "Admin can manage question bank" on public.question_bank
    for all to authenticated
    using (lower(coalesce(auth.jwt() ->> 'email', '')) = 'adeventurerank60@gmail.com')
    with check (lower(coalesce(auth.jwt() ->> 'email', '')) = 'adeventurerank60@gmail.com');
