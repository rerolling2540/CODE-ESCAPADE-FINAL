# Adaptive learning

Battles and the shared door, portal and trap panel use the same selector. It considers the current account (or guest), save slot and character language. The Almanac Progress report still combines account history.

The selector starts at beginner pace. Five resolved responses among the latest ten at a difficulty, covering at least three different question IDs with 80% accuracy, allow the next difficulty. Character levels cap difficulty: easy at levels 1–5, medium at 6–15, and hard from 16. Answer history cannot bypass level unlocks.

After at least three recent responses, accuracy below 60% in the latest five reduces pace by one difficulty. Topics with three or more responses and less than 60% accuracy receive extra review weight. Other topics stay in rotation. Earlier level bands remain available for review; min_level is always respected. Pools lacking the desired difficulty use the closest legal difficulty.

Selection avoids the last three question IDs where alternatives exist, and avoids an immediate repeat even when another legal difficulty is needed. A pool with only one legal question remains usable. Abandoned attempts do not count as wrong answers or mastery evidence. Timeouts count as unsuccessful responses.

Topic mastery uses the same five responses / three different questions / 80% rule. These are game indicators, not independently validated measures of programming ability. Repeating one Almanac example cannot establish mastery. Reports show how many different questions have been answered so sparse content is visible.

## Almanac guidance and history

Pages have stable topic fields. Old lesson titles and seeded IDs map to these topics, preserving attempt IDs. Changed local records are queued for the existing idempotent cloud upsert. Back, Close, changing pages and disabling the Almanac abandon unfinished practice immediately. Battle teardown and destroying a question panel abandon active attempts too.

After repeated mistakes, Recommended book opens a beginner book for that topic and language. Buttons on the bookshelf and Progress report are disabled if no matching review is recommended. They share existing medieval artwork and settings CanvasGroup visibility rules.

## Card authoring

CardData.minimumQuestionTier and maximumQuestionTier constrain questions from 1 (beginner) to 3 (advanced). Existing cards default to 1–3. This hook supports future rarity/tier design; refinement level does not represent programming proficiency. Keep the minimum within the levels where a card is obtainable. Impossible ranges yield no eligible question rather than bypassing the level cap.

## Offline and cloud compatibility

Resources/QuestionBanks/StarterQuestions.json bundles the 90 existing SQL seed questions for a first offline launch in every language. A cached bank replaces the starter bank; a successful Supabase refresh replaces the cache. Almanac examples remain separate. The remote bank is authoritative when connected.

No database migration is required. Portal and trap attempts use the existing door source bucket, compatible with the three-source schema. Local storage and synchronization remain managed by QuestionPerformanceTracker and CloudSaveSync. This change does not assert that live cloud synchronization has been verified.

## Verification

Run Tools > Code Escapade > Check Adaptive Learning for deterministic read-only regression checks. They cover promotion, review, varied-question mastery, language/level restrictions, card bounds, repeat prevention and sparse pools. Live UI checks cover practice cancellation, recommendations, three character languages, settings visibility and resuming after portal questions.
