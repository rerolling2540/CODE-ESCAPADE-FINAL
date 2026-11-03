using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Per-save adventure totals. Answer totals survive history pruning and cloud saves.</summary>
public static class PlayerAdventureStats
{
    [Serializable] public sealed class AnswerGroup
    {
        public string language, topic;
        public int correct, attempts;
    }

    [Serializable] public sealed class AnswerTotals
    {
        public int correct, attempts, currentStreak, bestStreak;
        public List<AnswerGroup> groups = new List<AnswerGroup>();
    }

    public static bool Resolved(QuestionAttempt answer) => answer != null &&
        (answer.outcome == "correct" || answer.outcome == "incorrect" || answer.outcome == "timed_out");

    public static void AddAnswer(AnswerTotals totals, QuestionAttempt answer)
    {
        if (!Resolved(answer)) return;
        bool correct = answer.outcome == "correct";
        totals.attempts++;
        if (correct) totals.correct++;
        totals.currentStreak = correct ? totals.currentStreak + 1 : 0;
        totals.bestStreak = Mathf.Max(totals.bestStreak, totals.currentStreak);
        if (totals.groups == null) totals.groups = new List<AnswerGroup>();
        string language = QuestionCurriculum.Language(answer.language);
        string topic = QuestionCurriculum.Topic(answer.topic, answer.question_id);
        var group = totals.groups.Find(g => g.language == language && g.topic == topic);
        if (group == null)
        {
            group = new AnswerGroup { language = language, topic = topic };
            totals.groups.Add(group);
        }
        group.attempts++;
        if (correct) group.correct++;
    }

    public static AnswerTotals Answers(SaveSlots.Record save)
    {
        if (save?.answerStats != null) return save.answerStats;
        var totals = new AnswerTotals();
        // Older saves can recover the question history still available on this device.
        foreach (var answer in QuestionPerformanceTracker.GetAttemptsForContext(MainMenuSaveSlots.CurrentUserId)
            .Where(a => a.slot_index == SaveSlots.Active && Resolved(a))
            .OrderBy(a => a.answered_at, StringComparer.Ordinal)) AddAnswer(totals, answer);
        if (save != null)
        {
            save.answerStats = totals;
            SaveSlots.Write(save);
        }
        return totals;
    }

    public static string Accuracy(int correct, int attempts) => attempts == 0
        ? "No answers yet" : (100f * correct / attempts).ToString("0.#") + "% (" + correct + " / " + attempts + ")";

    public static int BattlesWon(SaveSlots.Record save) => Mathf.Max(save?.battlesWon ?? 0,
        save?.defeatedEncounters?.Where(id => !string.IsNullOrEmpty(id)).Distinct().Count() ?? 0);

    public static void RecordVictory()
    {
        var save = SaveSlots.Read(SaveSlots.Active);
        if (save == null) return;
        save.battlesWon = BattlesWon(save) + 1;
        SaveSlots.Write(save);
    }

    // Three cave quest stages and four learning quests for the selected hero's language.
    public const int AvailableQuests = 7;
    public static int CompletedQuests(SaveSlots.Record save)
    {
        int count = Mathf.Clamp((save?.elfQuestStage ?? 0) - 1, 0, 3);
        if (ElfQuestProgress.Ready(save)) count++;
        for (int giver = -1; giver < 3; giver++)
            if (LearningQuests.Claimed(save, giver) ||
                LearningQuests.Accepted(save, giver) && LearningQuests.Completed(save, giver) == 2) count++;
        return count;
    }

    public static string CardsCollected()
    {
        var cards = new Dictionary<string, CardData>();
        var catalog = Resources.Load<InventoryCardCatalog>("InventoryCardCatalog");
        if (catalog != null)
            foreach (var entry in catalog.cards)
                if (entry?.card != null && !string.IsNullOrEmpty(entry.id)) cards[entry.id] = entry.card;
        foreach (var card in Resources.LoadAll<CardData>("ChestCards")) cards["chest." + card.name] = card;
        var merchant = SyntaxMerchantCatalog.Load();
        if (merchant != null)
            foreach (var offer in merchant.offers)
                if (offer != null && offer.kind == MerchantItemKind.Card && offer.card != null && !string.IsNullOrEmpty(offer.cardId))
                    cards[offer.cardId] = offer.card;
        var owned = new HashSet<CardData>();
        foreach (var stack in CharacterInventory.ReadSavedCards(SaveSlots.Active))
            if (stack.quantity > 0 && cards.TryGetValue(stack.card_key, out var card)) owned.Add(card);
        return owned.Count + " / " + cards.Values.Distinct().Count();
    }
}
