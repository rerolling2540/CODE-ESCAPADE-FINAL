using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Deterministic regression checks; never reads or writes a player's saved history.</summary>
public static class AdaptiveLearningChecks
{
    [MenuItem("Tools/Code Escapade/Check Adaptive Learning")]
    public static void CheckFromMenu() => Debug.Log(Run());

    public static string Run()
    {
        var assets = new List<QuestionData>();
        int checks = 0;
        Action<bool, string> check = (ok, message) => { if (!ok) throw new InvalidOperationException(message); checks++; };
        Func<string, string, string, int, QuestionData> question = (id, language, topic, tier) => {
            var q = ScriptableObject.CreateInstance<MultipleChoiceQuestionData>(); assets.Add(q);
            q.name = q.questionId = id; q.language = language; q.topic = topic;
            q.difficulty = QuestionCurriculum.Difficulty(tier); q.minLevel = tier == 1 ? 1 : tier == 2 ? 6 : 16;
            q.maxLevel = tier == 1 ? 5 : tier == 2 ? 15 : 0;
            return q;
        };
        Func<string, string, int, bool, int, QuestionAttempt> attempt = (id, topic, tier, correct, index) => new QuestionAttempt {
            question_id = id, language = "java", topic = topic, difficulty = QuestionCurriculum.Difficulty(tier),
            outcome = correct ? "correct" : "incorrect", slot_index = 0,
            presented_at = new DateTime(2026, 1, 1).AddSeconds(index).ToString("o")
        };
        try
        {
            check(QuestionCurriculum.Topic("Counting with for loops") == "loops", "Legacy lesson titles must share battle topic keys.");
            check(QuestionCurriculum.Topic("old title", "lesson-python-08-almanac") == "loops", "Legacy practice IDs must migrate.");
            check(QuestionCurriculum.Topic("general", "java-h02") == "algorithms", "Seed question IDs must preserve their topic.");
            check(QuestionCurriculum.Topic("My Custom Topic") == "my_custom_topic", "Custom topic tags must be preserved.");
            check(QuestionCurriculum.Language("C#") == "csharp", "C# aliases must match.");
            var repeated = Enumerable.Range(0, 10).Select(i => attempt("one-question", "loops", 1, true, i)).ToList();
            check(!AdaptiveQuestionSelector.Summarize(repeated).Mastered, "One memorized example cannot establish mastery.");
            var successes = Enumerable.Range(0, 5).Select(i => attempt("easy-" + i, "loops", 1, true, i)).ToList();
            check(AdaptiveQuestionSelector.Summarize(successes).Mastered, "Five varied correct answers should establish mastery.");
            check(AdaptiveQuestionSelector.LearningTier(successes, 1) == 1, "Performance cannot bypass level unlocks.");
            check(AdaptiveQuestionSelector.LearningTier(successes, 6) == 2, "Sustained easy success should unlock intermediate pace.");
            var intermediate = Enumerable.Range(0, 5).Select(i => attempt("medium-" + i, "loops", 2, true, 10 + i)).ToList();
            var advancedHistory = successes.Concat(intermediate).ToList();
            check(AdaptiveQuestionSelector.LearningTier(advancedHistory, 16) == 3, "Advanced pace requires success in the earlier tiers.");
            check(AdaptiveQuestionSelector.LearningTier(new QuestionAttempt[0], 16) == 1, "Cold starts must not assume proficiency.");
            advancedHistory.AddRange(Enumerable.Range(0, 3).Select(i => attempt("hard-" + i, "loops", 3, false, 20 + i)));
            check(AdaptiveQuestionSelector.LearningTier(advancedHistory, 16) == 2, "Repeated mistakes should lower the review pace.");
            var pool = new[] { question("j1", "java", "loops", 1), question("j2", "java", "loops", 1),
                question("j3", "java", "strings", 1), question("jm", "java", "loops", 2),
                question("jh", "java", "loops", 3), question("py", "python", "loops", 1) };
            for (int i = 0; i <= 20; i++)
            {
                var selected = AdaptiveQuestionSelector.Select(pool, "java", 1, successes, i / 20f, "j1");
                check(selected != null && selected.language == "java" && selected.minLevel <= 1 && selected.questionId != "j1", "Language, level and immediate-repeat guards must always hold.");
            }
            var recent = new[] { attempt("j1", "loops", 1, false, 1), attempt("j2", "loops", 1, false, 2) };
            check(AdaptiveQuestionSelector.Select(pool, "java", 1, recent, .5f).questionId == "j3", "Fresh questions should be preferred over the last three.");
            check(AdaptiveQuestionSelector.Select(pool.Where(q => q.topic == "loops"), "java", 6, successes, .5f).questionId == "jm", "Promoted pace should select an intermediate question when the topic has one.");
            check(AdaptiveQuestionSelector.Select(pool, "java", 6, successes, .5f, minimumTier: 1, maximumTier: 1).difficulty == "easy", "A card's upper bound must be honored.");
            check(AdaptiveQuestionSelector.Select(pool, "java", 1, successes, .5f, minimumTier: 2) == null, "A card's lower bound cannot bypass hero unlocks.");
            check(AdaptiveQuestionSelector.Select(new[] { pool[0] }, "java", 1, recent, .5f, "j1") == pool[0], "A single legal question must not deadlock selection.");
            var failures = Enumerable.Range(0, 3).Select(i => attempt("old" + i, "loops", 1, false, i)).ToList();
            var weightedPool = new[] { question("review", "java", "loops", 1), question("new", "java", "strings", 1) };
            int reviews = Enumerable.Range(0, 1000).Count(i => AdaptiveQuestionSelector.Select(weightedPool, "java", 1, failures, i / 1000f).topic == "loops");
            check(reviews > 600 && reviews < 750, "Weak topics should receive extra practice without excluding other topics.");
            var abandoned = attempt("abandoned", "loops", 1, true, 100); abandoned.outcome = "abandoned";
            check(AdaptiveQuestionSelector.Summarize(new[] { abandoned }).Responses == 0, "Abandonment is not a wrong answer or evidence of mastery.");
            return "PASS: " + checks + " adaptive learning checks; no player data changed.";
        }
        finally { foreach (var q in assets) UnityEngine.Object.DestroyImmediate(q); }
    }
}
