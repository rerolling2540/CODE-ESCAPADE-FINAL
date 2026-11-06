using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Checks report isolation and calculations using only temporary in-memory history.</summary>
public static class CharacterProgressReportChecks
{
    [MenuItem("Tools/Code Escapade/Check Character Progress Reports")]
    public static void CheckFromMenu() => Debug.Log(Run());

    public static string Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Run this audit outside Play mode.");
        var field = typeof(QuestionPerformanceTracker).GetField("attempts", BindingFlags.Static | BindingFlags.NonPublic);
        object original = field.GetValue(null);
        var history = new List<LocalQuestionAttempt>();
        int checks = 0;
        Action<bool, string> check = (ok, message) => {
            if (!ok) throw new InvalidOperationException(message);
            checks++;
        };
        const string owner = "character-report-audit";
        const int slot = 7;
        Action<string, string, string, int, int, string> add = (language, outcome, id, index, saveSlot, user) =>
            history.Add(new LocalQuestionAttempt {
                owner_user_id = user,
                attempt = new QuestionAttempt {
                    attempt_id = language + "-" + index, question_id = id, language = language,
                    topic = "variables", difficulty = "easy", player_level = 5,
                    outcome = outcome, slot_index = saveSlot,
                    presented_at = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(index).ToString("o")
                }
            });
        try
        {
            field.SetValue(null, history);
            string[] languages = { "java", "csharp", "python" };
            string[] displays = { "Java", "C#", "Python" };
            for (int hero = 0; hero < 3; hero++)
            {
                string language = languages[hero];
                check(CharacterAlmanac.LanguageForCharacter(hero) == language, "Incorrect hero/language mapping.");
                add(language, "correct", "a", 1, slot, owner);
                add(language, "incorrect", "b", 2, slot, owner);
                add(language, "timed_out", "c", 3, slot, owner);
                add(language, "abandoned", "d", 4, slot, owner);
                add(language, "in_progress", "e", 5, slot, owner);
                add(language, "correct", "other-slot", 6, slot + 1, owner);
                add(language, "correct", "other-account", 7, slot, "different-account");
            }
            for (int hero = 0; hero < 3; hero++)
            {
                string report = QuestionPerformanceTracker.BuildReport(owner, languages[hero], slot);
                check(report.Contains("Attempts shown: 4"), "Report mixed accounts, slots, languages or unfinished attempts.");
                check(report.Contains("Correct: 1    Incorrect: 1    Timed out: 1    Left: 1"), "Outcome totals are incorrect.");
                check(report.Contains("Accuracy: " + (100f / 3f).ToString("0.#") + "%    Completion: 75%"), "Accuracy/completion denominator is incorrect.");
                check(report.Contains(displays[hero].ToUpperInvariant() + " / SAVE SLOT 8"), "Wrong report scope label.");
                check(report.Contains(displays[hero] + " · Variables"), "Missing current language's topic progress.");
                foreach (int other in Enumerable.Range(0, 3).Where(i => i != hero))
                    check(!report.Contains(displays[other] + " · Variables"), "Another character's topic leaked into the report.");
                check(QuestionPerformanceTracker.BuildReport(owner, displays[hero], slot).Contains("Attempts shown: 4"), "Language display aliases do not normalize.");
            }
            history.Clear();
            string empty = QuestionPerformanceTracker.BuildReport(owner, "java", slot);
            check(empty.Contains("Accuracy: N/A    Completion: N/A"), "An empty report should not imply failed answers.");
            foreach (string language in languages)
            {
                history.Clear();
                for (int i = 0; i < 10; i++) add(language, "correct", "same-question", i, slot, owner);
                check(!QuestionPerformanceTracker.BuildReport(owner, language, slot).Contains("MASTERED"), "Repeating one question incorrectly establishes mastery.");
                history.Clear();
                for (int i = 0; i < 5; i++) add(language, "correct", "varied-" + i, i, slot, owner);
                for (int i = 5; i < 20; i++) add(language, "abandoned", "left-" + i, i, slot, owner);
                check(QuestionPerformanceTracker.BuildReport(owner, language, slot).Contains("MASTERED"), "Abandoned questions should not erase demonstrated mastery.");
                history.Clear();
                for (int i = 0; i < 3; i++) add(language, "incorrect", "wrong-" + i, i, slot, owner);
                check(QuestionPerformanceTracker.BuildReport(owner, language, slot).Contains("Study suggestion: review Variables"), "Review suggestions use the wrong account or language.");
            }
            var apply = typeof(PlayerProgression).GetMethod("ApplyExperience", BindingFlags.Static | BindingFlags.NonPublic);
            var saved = new SaveSlots.Record { level = 5, experience = 299, maxHealth = 200, currentHealth = 120 };
            apply.Invoke(null, new object[] { saved, 1 });
            check(saved.level == 6 && saved.experience == 0, "Level-up threshold or leftover XP is incorrect.");
            saved = new SaveSlots.Record { level = 5, experience = 0, maxHealth = 200, currentHealth = 120 };
            apply.Invoke(null, new object[] { saved, 1100 });
            check(saved.level == 8 && saved.experience == 50, "Multiple level-ups lose XP.");
            var info = Resources.Load<CharacterInfoData>("UI/CharacterInfo");
            check(info != null && info.heroes.Length >= 3, "Missing character information data.");
            for (int hero = 0; hero < 3; hero++)
                check(info.heroes[hero] != null && info.GetPortraitTexture(hero) != null, "Missing hero or portrait.");
            return checks + " character progression/report checks passed for Warrior, Archer and Mage. Player saves and history were not modified.";
        }
        finally { field.SetValue(null, original); }
    }
}
