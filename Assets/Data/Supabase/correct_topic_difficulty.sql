-- Run in the Supabase SQL editor. Preserves question IDs, content and attempt history.
begin;
with curriculum(topic, minimum_level, minimum_tier) as (
 values ('output',1,1),('syntax',1,1),('types',1,1),('variables',2,1),
 ('operators',2,1),('strings',3,1),('comparisons',6,2),('conditionals',7,2),
 ('loops',8,2),('arrays',10,2),('methods',12,2),('objects',14,2),
 ('collections',16,3),('exceptions',16,3),('algorithms',16,3),('recursion',18,3)
), corrected as (
 select q.id, greatest(c.minimum_tier,case q.difficulty when 'hard' then 3 when 'medium' then 2 else 1 end) as tier,
 greatest(coalesce(q.min_level,1),c.minimum_level) as minimum_level
 from public.question_bank q join curriculum c on lower(q.topic)=c.topic
), bands as (
 select id,tier,greatest(minimum_level,case tier when 3 then 16 when 2 then 6 else 1 end) as minimum_level from corrected
)
update public.question_bank q set
 difficulty=case b.tier when 3 then 'hard' when 2 then 'medium' else 'easy' end,
 min_level=b.minimum_level,
 max_level=case when q.max_level<b.minimum_level then null else q.max_level end
from bands b where q.id=b.id;
commit;
