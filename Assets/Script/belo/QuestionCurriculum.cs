using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>Stable curriculum keys shared by authored lessons, quizzes and old attempt records.</summary>
public static class QuestionCurriculum
{
    [Serializable] private sealed class Lessons { public Lesson[] items; }
    [Serializable] private sealed class Lesson { public string id, title, topic; }
    private static Dictionary<string, string> lessonTopics;

    public static string Language(string value)
    {
        string key = (value ?? "").Trim().ToLowerInvariant();
        return key == "c#" || key == "c_sharp" || key == "c-sharp" ? "csharp" : key;
    }

    public static string Topic(string value, string questionId = null)
    {
        if (lessonTopics == null)
        {
            lessonTopics = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var asset = Resources.Load<TextAsset>("LearningAlmanac/OriginalLessons");
            var bank = asset != null ? JsonUtility.FromJson<Lessons>(asset.text) : null;
            if (bank?.items != null)
                foreach (var lesson in bank.items)
                {
                    if (string.IsNullOrEmpty(lesson.topic)) continue;
                    lessonTopics[lesson.id] = lesson.topic;
                    lessonTopics[lesson.id + "-almanac"] = lesson.topic;
                    lessonTopics[lesson.title] = lesson.topic;
                }
        }
        string result;
        if (!string.IsNullOrEmpty(questionId) && lessonTopics.TryGetValue(questionId, out result)) return result;
        if (!string.IsNullOrEmpty(value) && lessonTopics.TryGetValue(value, out result)) return result;
        string key = Regex.Replace((value ?? "").Trim().ToLowerInvariant(), @"[^a-z0-9]+", "_").Trim('_');
        switch (key)
        {
            case "function": case "functions": case "method": return "methods";
            case "loop": case "iteration": return "loops";
            case "conditional": case "conditions": case "if_else": return "conditionals";
            case "array": case "indexing": return "arrays";
            case "variable": return "variables";
            case "string": return "strings";
            case "printing": case "print": return "output";
        }
        if (!string.IsNullOrEmpty(key) && key != "general") return key;
        var match = Regex.Match(questionId ?? "", @"-(e|m|h)(\d\d)$");
        if (match.Success)
        {
            string[] topics = match.Groups[1].Value == "e"
                ? new[] { "output", "variables", "strings", "conditionals", "arrays", "methods", "loops", "syntax", "comparisons", "types" }
                : match.Groups[1].Value == "m"
                ? new[] { "loops", "strings", "arrays", "operators", "strings", "arrays", "methods", "conditionals", "loops", "arrays" }
                : new[] { "recursion", "algorithms", "collections", "objects", "exceptions", "collections", "operators", "collections", "loops", "recursion" };
            int index = int.Parse(match.Groups[2].Value) - 1;
            if (index >= 0 && index < topics.Length) return topics[index];
        }
        return "general";
    }

    public static string Topic(QuestionData question)
    {
        string topic = Topic(question.topic, Id(question));
        if (topic != "general") return topic;
        string code = question is MultipleChoiceQuestionData mc ? mc.codeSnippet : question is FillInBlankQuestionData blank ? blank.codeWithBlank : "";
        string content = ((question.questionText ?? "") + " " + code).ToLowerInvariant();
        if (content.Contains("recurs")) return "recursion";
        if (content.Contains("loop") || content.Contains("for (") || content.Contains("for ")) return "loops";
        if (content.Contains("array") || content.Contains("index")) return "arrays";
        if (content.Contains("string")) return "strings";
        if (content.Contains("method") || content.Contains("function")) return "methods";
        if (content.Contains("variable") || content.Contains("declare")) return "variables";
        return topic;
    }

    public static string Language(QuestionData question)
    {
        if (!string.IsNullOrWhiteSpace(question.language)) return Language(question.language);
        string code = question is MultipleChoiceQuestionData mc ? mc.codeSnippet : question is FillInBlankQuestionData blank ? blank.codeWithBlank : "";
        string content = ((question.questionText ?? "") + " " + code).ToLowerInvariant();
        if (content.Contains("console.") || content.Contains("c#")) return "csharp";
        if (content.Contains("python") || content.Contains("print(") || content.Contains("def ")) return "python";
        // Untagged legacy questions were authored for the original Java warrior.
        return "java";
    }
    public static string Id(QuestionData question)
    {
        if (!string.IsNullOrEmpty(question.questionId)) return question.questionId;
        // A stable identity for legacy assets; never Unity's process-dependent instance ID.
        return !string.IsNullOrEmpty(question.name) ? question.name : question.questionText;
    }
    public static int Tier(string difficulty) => difficulty == "hard" ? 3 : difficulty == "medium" ? 2 : 1;
    public static int LevelTier(int level) => level <= 5 ? 1 : level <= 15 ? 2 : 3;
    public static string Difficulty(int tier) => tier >= 3 ? "hard" : tier == 2 ? "medium" : "easy";
}
