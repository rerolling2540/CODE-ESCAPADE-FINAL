using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Pure selection rules. History is scoped by the caller; no saves or cloud calls here.</summary>
public static class AdaptiveQuestionSelector
{
    public sealed class TopicProgress
    {
        public int Responses, Correct, DifferentQuestions, RecentMistakes;
        public float Accuracy => Responses == 0 ? 0 : (float)Correct / Responses;
        public bool Mastered => Responses >= 5 && DifferentQuestions >= 3 && Accuracy >= .8f;
        public bool NeedsReview => Responses >= 3 && Accuracy < .6f;
    }

    public static bool Resolved(QuestionAttempt a) => a != null &&
        (a.outcome == "correct" || a.outcome == "incorrect" || a.outcome == "timed_out");

    public static TopicProgress Summarize(IEnumerable<QuestionAttempt> records)
    {
        var recent = (records ?? Enumerable.Empty<QuestionAttempt>()).Where(Resolved)
            .OrderByDescending(a => a.presented_at, StringComparer.Ordinal).Take(10).ToList();
        return new TopicProgress {
            Responses = recent.Count, Correct = recent.Count(a => a.outcome == "correct"),
            DifferentQuestions = recent.Select(a => a.question_id).Where(id => !string.IsNullOrEmpty(id)).Distinct().Count(),
            RecentMistakes = recent.Take(3).Count(a => a.outcome != "correct")
        };
    }

    public static int LearningTier(IEnumerable<QuestionAttempt> history, int level)
    {
        var records = (history ?? Enumerable.Empty<QuestionAttempt>()).Where(Resolved).ToList();
        int tier = 1, cap = QuestionCurriculum.LevelTier(level);
        while (tier < cap && Summarize(records.Where(a => QuestionCurriculum.Tier(a.difficulty) == tier)).Mastered) tier++;
        var latest = records.OrderByDescending(a => a.presented_at, StringComparer.Ordinal).Take(5).ToList();
        if (latest.Count >= 3 && latest.Count(a => a.outcome == "correct") < latest.Count * .6f) tier = Math.Max(1, tier - 1);
        return tier;
    }

    public static QuestionData Select(IEnumerable<QuestionData> pool, string language, int level,
        IEnumerable<QuestionAttempt> history, float randomValue, string avoidId = null, int minimumTier = 1, int maximumTier = 3)
    {
        language = QuestionCurriculum.Language(language);
        var records = (history ?? Enumerable.Empty<QuestionAttempt>()).Where(a => a != null &&
            QuestionCurriculum.Language(a.language) == language).ToList();
        int cap = Math.Min(QuestionCurriculum.LevelTier(level), Mathf.Clamp(maximumTier, 1, 3));
        int floor = Mathf.Clamp(minimumTier, 1, 3);
        if (floor > cap) return null; // An authored card cannot override the character's unlocks.
        int learningTier = Mathf.Clamp(LearningTier(records, level), floor, cap);
        var candidates = (pool ?? Enumerable.Empty<QuestionData>()).Where(q => q != null &&
            QuestionCurriculum.Language(q) == language && q.minLevel <= level &&
            QuestionCurriculum.Tier(q.difficulty) >= floor && QuestionCurriculum.Tier(q.difficulty) <= cap)
            .GroupBy(QuestionCurriculum.Id).Select(g => g.First()).ToList();
        if (candidates.Count == 0) return null;

        // maxLevel marks normal level bands. Earlier bands remain available for review.
        var groups = candidates.GroupBy(QuestionCurriculum.Topic).ToList();
        var weighted = new List<KeyValuePair<QuestionData, float>>();
        foreach (var group in groups)
        {
            var progress = Summarize(records.Where(a => QuestionCurriculum.Topic(a.topic, a.question_id) == group.Key));
            int desired = progress.NeedsReview ? Math.Max(floor, learningTier - 1) : learningTier;
            int nearest = group.Min(q => Math.Abs(QuestionCurriculum.Tier(q.difficulty) - desired));
            var choices = group.Where(q => Math.Abs(QuestionCurriculum.Tier(q.difficulty) - desired) == nearest).ToList();
            float topicWeight = progress.NeedsReview ? 3f : progress.Responses == 0 ? 1.5f : progress.Mastered ? .65f : 1f;
            foreach (var q in choices) weighted.Add(new KeyValuePair<QuestionData, float>(q, topicWeight / choices.Count));
        }
        var recentIds = new HashSet<string>(records.OrderByDescending(a => a.presented_at, StringComparer.Ordinal)
            .Take(3).Select(a => a.question_id));
        if (!string.IsNullOrEmpty(avoidId)) recentIds.Add(avoidId);
        var fresh = weighted.Where(p => !recentIds.Contains(QuestionCurriculum.Id(p.Key))).ToList();
        if (fresh.Count == 0)
        {
            string last = avoidId ?? records.OrderByDescending(a => a.presented_at, StringComparer.Ordinal).FirstOrDefault()?.question_id;
            fresh = weighted.Where(p => QuestionCurriculum.Id(p.Key) != last).ToList();
        }
        // If a topic's preferred tier has only the last question, use another legal tier before repeating it.
        if (fresh.Count == 0 && candidates.Count > 1)
        {
            string last = avoidId ?? records.OrderByDescending(a => a.presented_at, StringComparer.Ordinal).FirstOrDefault()?.question_id;
            fresh = candidates.Where(q => QuestionCurriculum.Id(q) != last).Select(q => new KeyValuePair<QuestionData, float>(q, 1)).ToList();
        }
        if (fresh.Count > 0) weighted = fresh;
        float target = Mathf.Clamp01(randomValue) * weighted.Sum(p => p.Value);
        foreach (var pair in weighted) { target -= pair.Value; if (target <= 0) return pair.Key; }
        return weighted[weighted.Count - 1].Key;
    }
}
