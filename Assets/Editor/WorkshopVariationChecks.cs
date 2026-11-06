using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Checks authored workshop tiers without touching player saves or quiz history.</summary>
public static class WorkshopVariationChecks
{
    [MenuItem("Tools/Code Escapade/Check Workshop Variations")]
    public static void CheckFromMenu() => Debug.Log(Run());

    public static string Run()
    {
        int checks = 0;
        Action<bool, string> check = (ok, message) => {
            checks++;
            if (!ok) throw new InvalidOperationException(message);
        };
        // Independently specified console results for the authored demonstrations.
        string[][] expected = {
            new[] { "Welcome, hero!\nBegin your quest.", "7", "8\nLantern ready" },
            new[] { "5", "6", "5" },
            new[] { "Open", "Open", "Locked" },
            new[] { "0\n1\n2", "6", "3" },
            new[] { "7", "11", "3" },
            new[] { "5", "11", "6" }
        };
        string[][] practice = {
            new[] { "Torch lit\nGate open", "5", "10\nQuest ready" },
            new[] { "6", "8", "5" },
            new[] { "Locked", "Open", "Locked" },
            new[] { "Step\nStep", "6", "6" },
            new[] { "3", "11", "5" },
            new[] { "8", "9", "7" }
        };
        foreach (string language in new[] { "java", "csharp", "python" })
        for (int topic = 0; topic < 6; topic++)
        {
            var examples = new HashSet<string>();
            var predictions = new HashSet<string>();
            for (int tier = 0; tier < 3; tier++)
            {
                string label = language + " / " + topic + " / " + tier;
                var lesson = WorkshopVariations.Get(language, topic, tier);
                check(examples.Add(lesson.code), label + ": repeated demonstration from an earlier tier.");
                check(predictions.Add(lesson.challenge), label + ": repeated practice question from an earlier tier.");
                check(lesson.code != lesson.challenge, label + ": demonstration gives away the identical practice code.");
                check(lesson.steps.Length > 1 && lesson.steps[0].line == -1 && lesson.steps[0].output == "",
                    label + ": replay must start with a fresh console.");
                check(lesson.steps.Last().output == expected[topic][tier], label + ": incorrect final console output.");
                check(lesson.answers[lesson.correct] == practice[topic][tier], label + ": incorrect practice answer.");
                int lines = lesson.code.Split('\n').Length;
                foreach (var step in lesson.steps)
                {
                    check(step.line >= -1 && step.line < lines, label + ": highlighted line outside the selected code.");
                    check(step.cells != null && step.cells.Length > 0 && step.cells.Length <= 3,
                        label + ": invalid visual-state cells.");
                    check(step.activeCell >= -1 && step.activeCell < step.cells.Length,
                        label + ": active cell outside the visual state.");
                    if (step.line >= 0 && !string.IsNullOrEmpty(step.output))
                    {
                        string line = lesson.code.Split('\n')[step.line];
                        string earlier = lesson.steps.Take(Array.IndexOf(lesson.steps, step)).LastOrDefault()?.output ?? "";
                        check(step.output == earlier || line.Contains(language == "python" ? "print(" : language == "csharp" ? "Console.WriteLine(" : "System.out.println("),
                            label + ": console changed on a line that does not print.");
                    }
                }
                string both = lesson.code + lesson.challenge;
                check(language == "python" ? !both.Contains("System.out") && !both.Contains("Console.WriteLine")
                    : language == "csharp" ? !both.Contains("System.out") : !both.Contains("Console.WriteLine"),
                    label + ": another language's output function leaked into the lesson.");
            }
        }
        // Changing a foundation experiment must not contaminate a later level or replay.
        foreach (string language in new[] { "java", "csharp", "python" })
        for (int topic = 0; topic < 2; topic++)
        {
            var before = WorkshopVariations.Get(language, topic, 1);
            BeginnerWorkshopExperiment.Create(language, topic, 2, 6);
            var after = WorkshopVariations.Get(language, topic, 1);
            check(before.code == after.code && before.steps.Last().output == after.steps.Last().output,
                language + ": foundation experiment leaked into Applied.");
            before.steps[0].output = "stale console";
            check(after.steps[0].output == "", language + ": lessons share mutable console state.");
        }
        return "PASS: " + checks + " checks across 54 workshop lessons (3 languages, 6 topics, 3 levels); no player data changed.";
    }
}
