using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class QuestionAttempt
{
    public string attempt_id;
    public string user_id;
    public int slot_index;
    public string question_id;
    public string topic;
    public string language;
    public string difficulty;
    public string source;
    public int player_level;
    public string outcome;
    public string presented_at;
    public string answered_at;
    public float duration_seconds;
}

[Serializable]
public sealed class LocalQuestionAttempt
{
    public QuestionAttempt attempt;
    public string owner_user_id;
    public bool synced;
}

/// <summary>Local-first question history with an idempotent Supabase sync queue.</summary>
public static class QuestionPerformanceTracker
{
    private const int MaxLocalAttempts = 3000;
    private static readonly Dictionary<string, float> StartedAt = new Dictionary<string, float>();
    private static List<LocalQuestionAttempt> attempts;
    private static string syncStatus = "Attempts are saved on this device.";
    public static event Action HistoryChanged;
    public static event Action PendingAttemptsChanged;
    public static string SyncStatus => syncStatus;

    [Serializable] private sealed class AttemptFile { public LocalQuestionAttempt[] items; }

    public static string Begin(QuestionData question, string source, string languageFallback = null, string topicFallback = null)
    {
        EnsureLoaded();
        if (question == null) return "";

        string userId = MainMenuSaveSlots.CurrentUserId ?? "";
        string language = NonEmpty(question.language, languageFallback);
        if (string.IsNullOrEmpty(language)) language = CharacterAlmanac.LanguageForCharacter(PlayerPrefs.GetInt("SelectedCharacter", 0));
        string questionId = NonEmpty(question.questionId, question.name);
        string topic = NonEmpty(question.topic, topicFallback);
        if (string.IsNullOrEmpty(topic) || topic == "general") topic = QuestionCurriculum.Topic(question);
        int currentLevel = GetCurrentPlayerLevel();

        var attempt = new QuestionAttempt
        {
            attempt_id = Guid.NewGuid().ToString(),
            user_id = userId,
            slot_index = SaveSlots.Active,
            question_id = questionId,
            topic = QuestionCurriculum.Topic(topic, questionId),
            language = NormalizeLanguage(language),
            difficulty = NonEmpty(question.difficulty, DifficultyForLevel(currentLevel)),
            source = NormalizeSource(source, "battle"),
            player_level = currentLevel,
            outcome = "in_progress",
            presented_at = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            answered_at = "",
            duration_seconds = 0f
        };

        attempts.Add(new LocalQuestionAttempt { attempt = attempt, owner_user_id = userId, synced = false });
        StartedAt[attempt.attempt_id] = Time.realtimeSinceStartup;
        TrimAndSave();
        HistoryChanged?.Invoke();
        return attempt.attempt_id;
    }

    public static string Begin(string questionId, string topic, string language, string difficulty,
        int playerLevel, string source)
    {
        EnsureLoaded();
        string userId = MainMenuSaveSlots.CurrentUserId ?? "";
        var attempt = new QuestionAttempt
        {
            attempt_id = Guid.NewGuid().ToString(),
            user_id = userId,
            slot_index = SaveSlots.Active,
            question_id = string.IsNullOrWhiteSpace(questionId) ? "almanac-practice" : questionId,
            topic = QuestionCurriculum.Topic(topic, questionId),
            language = NormalizeLanguage(language),
            difficulty = string.IsNullOrWhiteSpace(difficulty) ? DifficultyForLevel(playerLevel) : difficulty,
            source = NormalizeSource(source, "almanac"),
            player_level = Mathf.Max(1, playerLevel),
            outcome = "in_progress",
            presented_at = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            answered_at = "",
            duration_seconds = 0f
        };
        attempts.Add(new LocalQuestionAttempt { attempt = attempt, owner_user_id = userId, synced = false });
        StartedAt[attempt.attempt_id] = Time.realtimeSinceStartup;
        TrimAndSave();
        HistoryChanged?.Invoke();
        return attempt.attempt_id;
    }

    public static void Complete(string attemptId, bool correct) => Resolve(attemptId, correct ? "correct" : "incorrect");
    public static void TimeOut(string attemptId) => Resolve(attemptId, "timed_out");
    public static void Abandon(string attemptId) => Resolve(attemptId, "abandoned");

    private static void Resolve(string attemptId, string outcome)
    {
        if (string.IsNullOrEmpty(attemptId)) return;
        EnsureLoaded();
        LocalQuestionAttempt local = attempts.FirstOrDefault(item => item?.attempt?.attempt_id == attemptId);
        if (local?.attempt == null || local.attempt.outcome != "in_progress") return;
        local.attempt.outcome = outcome;
        local.attempt.answered_at = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        if (StartedAt.TryGetValue(attemptId, out float start))
        {
            local.attempt.duration_seconds = Mathf.Max(0f, Time.realtimeSinceStartup - start);
            StartedAt.Remove(attemptId);
        }
        TrimAndSave();
        HistoryChanged?.Invoke();
        PendingAttemptsChanged?.Invoke();
    }

    public static List<QuestionAttempt> PreparePendingForUser(string userId)
    {
        EnsureLoaded();
        if (string.IsNullOrWhiteSpace(userId)) return new List<QuestionAttempt>();
        foreach (var item in attempts)
        {
            if (item?.attempt == null) continue;
            if (string.IsNullOrEmpty(item.owner_user_id))
            {
                item.owner_user_id = userId;
                item.attempt.user_id = userId;
            }
        }
        Save();
        return attempts.Where(item => item?.attempt != null && !item.synced &&
                item.attempt.outcome != "in_progress" && item.owner_user_id == userId)
            .Select(item => item.attempt).ToList();
    }

    public static void MarkSynced(string userId, IEnumerable<string> ids)
    {
        EnsureLoaded();
        var idSet = new HashSet<string>(ids ?? Enumerable.Empty<string>());
        foreach (var item in attempts)
            if (item?.attempt != null && item.owner_user_id == userId && idSet.Contains(item.attempt.attempt_id))
                item.synced = true;
        syncStatus = "Question history synced to your account.";
        Save();
        HistoryChanged?.Invoke();
    }

    public static void MergeRemote(string userId, IEnumerable<QuestionAttempt> remoteAttempts)
    {
        EnsureLoaded();
        var localById = new Dictionary<string, LocalQuestionAttempt>();
        foreach (var item in attempts)
            if (item?.attempt != null && !string.IsNullOrEmpty(item.attempt.attempt_id))
                localById[item.attempt.attempt_id] = item;
        foreach (var remote in remoteAttempts ?? Enumerable.Empty<QuestionAttempt>())
        {
            if (remote == null || string.IsNullOrEmpty(remote.attempt_id)) continue;
            remote.user_id = userId;
            bool canonicalized = Canonicalize(remote);
            if (localById.TryGetValue(remote.attempt_id, out LocalQuestionAttempt local))
            {
                local.attempt = remote;
                local.owner_user_id = userId;
                local.synced = !canonicalized;
            }
            else
            {
                var item = new LocalQuestionAttempt { attempt = remote, owner_user_id = userId, synced = !canonicalized };
                attempts.Add(item);
                localById[remote.attempt_id] = item;
            }
        }
        syncStatus = "Question history synced to your account.";
        TrimAndSave();
        HistoryChanged?.Invoke();
    }

    public static void SetSyncStatus(string value)
    {
        syncStatus = string.IsNullOrWhiteSpace(value) ? "Attempts are saved on this device." : value;
        HistoryChanged?.Invoke();
    }

    public static List<QuestionAttempt> GetAttemptsForContext(string userId)
    {
        EnsureLoaded();
        return attempts.Where(item => item?.attempt != null &&
             item.owner_user_id == (userId ?? "") && item.attempt.outcome != "in_progress")
            .Select(item => item.attempt).ToList();
    }

    public static List<QuestionAttempt> GetLearningHistory(string language, int slot)
    {
        string key = NormalizeLanguage(language);
        return GetAttemptsForContext(MainMenuSaveSlots.CurrentUserId).Where(a =>
            a.slot_index == slot && NormalizeLanguage(a.language) == key).ToList();
    }

    public static string RecommendedTopic(string language, int slot)
    {
        return GetLearningHistory(language, slot).Where(AdaptiveQuestionSelector.Resolved)
            .GroupBy(a => QuestionCurriculum.Topic(a.topic, a.question_id))
            .Select(g => new { Topic = g.Key, Progress = AdaptiveQuestionSelector.Summarize(g) })
            .Where(g => g.Progress.NeedsReview && g.Progress.RecentMistakes >= 2)
            .OrderBy(g => g.Progress.Accuracy).ThenBy(g => g.Topic, StringComparer.Ordinal)
            .Select(g => g.Topic).FirstOrDefault();
    }

    public static string BuildReport(string userId)
    {
        List<QuestionAttempt> records = GetAttemptsForContext(userId)
            .OrderByDescending(item => ParseTime(item.presented_at)).ToList();
        int total = records.Count;
        int correct = records.Count(item => item.outcome == "correct");
        int wrong = records.Count(item => item.outcome == "incorrect");
        int timedOut = records.Count(item => item.outcome == "timed_out");
        int abandoned = records.Count(item => item.outcome == "abandoned");
        int resolved = correct + wrong + timedOut;
        float accuracy = resolved == 0 ? 0f : 100f * correct / resolved;
        float completion = total == 0 ? 0f : 100f * resolved / total;

        var lines = new List<string>
        {
            "QUESTION PERFORMANCE",
            "",
            "Attempts shown: " + total,
            "Answered or timed out: " + resolved,
            "Correct: " + correct + "    Incorrect: " + wrong + "    Timed out: " + timedOut + "    Left: " + abandoned,
            "Accuracy: " + accuracy.ToString("0.#") + "%    Completion: " + completion.ToString("0.#") + "%",
            "",
            "TOPIC MASTERY",
            "Mastery = 5+ of the latest 10 responses, 3 different questions, and 80% accuracy.",
            "Repeated answers alone do not demonstrate topic mastery."
        };

        var groups = records.Where(item => item.outcome != "abandoned")
            .GroupBy(item => NormalizeLanguage(item.language) + "|" + QuestionCurriculum.Topic(item.topic, item.question_id))
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);
        bool any = false;
        foreach (var group in groups)
        {
            var latest = group.OrderByDescending(item => ParseTime(item.presented_at)).Take(10).ToList();
            int topicCorrect = latest.Count(item => item.outcome == "correct");
            int topicResolved = latest.Count(item => item.outcome == "correct" || item.outcome == "incorrect" || item.outcome == "timed_out");
            if (topicResolved == 0) continue;
            any = true;
            float topicAccuracy = 100f * topicCorrect / topicResolved;
            var progress = AdaptiveQuestionSelector.Summarize(latest);
            bool mastered = progress.Mastered;
            string[] keyParts = group.Key.Split('|');
            lines.Add("");
            lines.Add(LanguageDisplay(keyParts[0]) + " · " + TitleCase(keyParts.Length > 1 ? keyParts[1] : "general") +
                "  ·  " + topicResolved + " responses / " + progress.DifferentQuestions + " different questions  ·  " + topicAccuracy.ToString("0.#") + "%  ·  " + (mastered ? "MASTERED" : progress.NeedsReview ? "Review recommended" : "Keep practicing"));
            lines.Add("  " + string.Join(" / ", latest.Take(5).Select(item => "Lv " + item.player_level + " · " + TitleCase(item.outcome))));
        }
        if (!any) lines.Add("\nAnswer questions in battles, at doors, or in Almanac practice to build your report.");
        string language = CharacterAlmanac.LanguageForCharacter(PlayerPrefs.GetInt("SelectedCharacter", 0));
        string recommended = RecommendedTopic(language, SaveSlots.Active);
        int learningTier = AdaptiveQuestionSelector.LearningTier(GetLearningHistory(language, SaveSlots.Active), GetCurrentPlayerLevel());
        lines.Add("\nCURRENT HERO / SAVE SLOT " + (SaveSlots.Active + 1));
        lines.Add("Learning pace: " + TitleCase(QuestionCurriculum.Difficulty(learningTier)) + ". Earlier topics may appear for review.");
        if (recommended != null) lines.Add("Study suggestion: review " + TitleCase(recommended) + " using Recommended book.");
        lines.Add("The report above combines account history; question adaptation uses this hero's language and save slot.");
        lines.Add("\n" + syncStatus);
        if (string.IsNullOrEmpty(userId)) lines.Add("\nGuest history stays on this device until you sign in.");
        return string.Join("\n", lines);
    }

    public static int GetCurrentPlayerLevel()
    {
        PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        return Mathf.Max(1, player != null ? player.CurrentLevel : PlayerPrefs.GetInt("PlayerLevel", 1));
    }

    private static string DifficultyForLevel(int level) => level <= 5 ? "easy" : level <= 15 ? "medium" : "hard";
    private static string NormalizeLanguage(string value) => string.IsNullOrWhiteSpace(value) ? "java" : QuestionCurriculum.Language(value);
    // Portal and trap puzzles share the existing door bucket so deployed schemas need no migration.
    private static string NormalizeSource(string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback :
        value.Trim().ToLowerInvariant() == "portal" || value.Trim().ToLowerInvariant() == "trap" ? "door" : value.Trim().ToLowerInvariant();
    private static string LanguageDisplay(string value) => value == "csharp" ? "C#" : value == "python" ? "Python" : "Java";
    private static string NonEmpty(string first, string second) => !string.IsNullOrWhiteSpace(first) ? first.Trim() : (second ?? "").Trim();
    private static string TitleCase(string value) => CultureInfo.InvariantCulture.TextInfo.ToTitleCase((value ?? "").Replace('_', ' '));
    private static DateTime ParseTime(string value) => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var time) ? time.ToUniversalTime() : DateTime.MinValue;

    private static void EnsureLoaded()
    {
        if (attempts != null) return;
        attempts = new List<LocalQuestionAttempt>();
        string path = FilePath();
        try
        {
            if (!File.Exists(path)) return;
            AttemptFile file = JsonUtility.FromJson<AttemptFile>(File.ReadAllText(path));
            if (file?.items != null) attempts.AddRange(file.items.Where(item => item?.attempt != null));
            bool changed = false;
            foreach (var item in attempts)
            {
                if (!Canonicalize(item.attempt)) continue;
                item.synced = false;
                changed = true;
            }
            foreach (var item in attempts.Where(item => item.attempt.outcome == "in_progress"))
            {
                item.attempt.outcome = "abandoned";
                item.attempt.answered_at = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                item.synced = false;
                changed = true;
            }
            if (changed) Save();
        }
        catch (Exception exception) { Debug.LogWarning("Could not read local question history: " + exception.Message); }
    }

    private static bool Canonicalize(QuestionAttempt attempt)
    {
        string topic = QuestionCurriculum.Topic(attempt.topic, attempt.question_id);
        string language = NormalizeLanguage(attempt.language);
        string source = NormalizeSource(attempt.source, "battle");
        bool changed = topic != attempt.topic || language != attempt.language || source != attempt.source;
        attempt.topic = topic;
        attempt.language = language;
        attempt.source = source;
        return changed;
    }

    private static void TrimAndSave()
    {
        if (attempts.Count > MaxLocalAttempts)
        {
            List<LocalQuestionAttempt> pending = attempts.Where(item => !item.synced).ToList();
            int syncedBudget = Mathf.Max(0, MaxLocalAttempts - pending.Count);
            attempts = pending.Concat(attempts.Where(item => item.synced)
                .OrderByDescending(item => ParseTime(item.attempt.presented_at)).Take(syncedBudget)).ToList();
        }
        Save();
    }

    private static void Save()
    {
        try
        {
            string path = FilePath();
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(path, JsonUtility.ToJson(new AttemptFile { items = attempts.ToArray() }));
        }
        catch (Exception exception) { Debug.LogWarning("Could not save local question history: " + exception.Message); }
    }

    private static string FilePath() => Path.Combine(Application.persistentDataPath, "QuestionPerformance.json");
}
