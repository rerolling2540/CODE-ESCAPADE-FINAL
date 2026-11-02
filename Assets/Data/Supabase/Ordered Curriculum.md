# Ordered question curriculum

The live published bank was read before authoring: 90 questions, 30 per language. `ordered_curriculum_expansion.sql` is prepared for Supabase SQL Editor. It has **not been applied** by this change: the connected project key supports public reads, and no authorized admin/SQL connection was available. No login credentials or service keys are included in the migration.

## Contents

372 original new questions (124 per language), plus the preserved 90, give 462 questions / 154 per hero language. New answer positions are balanced equally across A–D. Existing IDs, question text, answer values and performance history are preserved. The legacy `*-h07` arithmetic examples are reclassified as easy. Four variations cover each new topic/difficulty family; executable prediction examples and multiple-choice/fill-blank questions use the existing schema.

Order and earliest topic levels:

| Topic | Earliest level |
| --- | --- |
| Output, syntax, types | 1 |
| Variables, operators | 2 |
| Strings, comparisons | 3 |
| Conditions | 4 |
| Loops | 5 |
| Arrays/lists | 6 |
| Methods/functions | 8 |
| Objects | 10 |
| Collections | 12 |
| Exceptions | 14 |
| Algorithms | 16 |
| Recursion | 18 |

Difficulty minimums also apply: easy 1+, medium 6+, hard 16+. Use the **higher** of topic and difficulty minimums. For example, new object questions currently target hard difficulty and therefore start at 16, despite the topic's earliest possible level being 10. Older unlocked bands remain available for review; max_level is cleared rather than permanently removing earlier lessons as players level up.

## Selection

The next eligible topic receives priority. Advancing past a topic needs at least three resolved responses, at least two different question IDs (or all available IDs if fewer than two), and at least two-thirds accuracy among its latest ten resolved responses. This readiness rule is separate from mastery: mastery still needs five responses, three different questions and 80% accuracy. Known topics remain reviewable even if an old save learned them out of order. Weak-topic weights, hero-language isolation, card difficulty bounds and recent-repeat avoidance are retained. A high player level does not automatically skip all introductory topics.

The selector computes this order locally, so the game's existing REST query continues working before the new database column is installed. Database row order alone never controls teaching order. Published remote data remains authoritative; the game does not resurrect remotely removed/unpublished questions from the bundled copy. New questions are bundled for the offline bank; online players receive them after the migration is applied and the bank refreshes/reloads.

## Apply and verify

1. Open the project's Supabase SQL Editor with the authorized admin account.
2. Run `ordered_curriculum_expansion.sql`. It adds an indexed curriculum_order column, updates only the 90 snapshotted IDs whose prompt/code still match, and inserts the new IDs with ON CONFLICT DO NOTHING. It runs in one transaction, deletes no rows, and leaves unrelated/unpublished/custom rows alone. Re-running does not duplicate IDs or overwrite edited new rows.
3. Query `select language, count(*) from public.question_bank where published group by language;` (expected 154 each if the live bank has not otherwise changed).
4. Inspect using `select id,language,topic,difficulty,min_level,curriculum_order from public.question_bank where published order by language,curriculum_order,min_level,id;`.
5. Restart/re-enter the battle scene so the live question bank refreshes. Account-specific cloud writes and authenticated synchronization are outside this migration's verification.

## Validation

The generator validates unique IDs, valid/distinct choices, level bands and answer indexes. All 96 generated runnable Python prediction examples were actually executed and their console output compared to the stored answer. Java/C# are generated from the parallel templates; they have not been executed with Java/.NET compilers. Unity compilation and 43 deterministic adaptive-selection checks passed without modifying player data. SQL execution/live deployment remains pending.

To reproduce, run `build_curriculum.py <published-bank-snapshot.json>` using Python 3. The pre-change snapshot is stored in the task's local work artifacts, outside the game assets. The script updates the offline bank and migration together; review its output before applying it to a changed live bank.
