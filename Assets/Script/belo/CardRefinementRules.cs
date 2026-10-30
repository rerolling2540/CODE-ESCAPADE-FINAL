using System.Collections.Generic;
using UnityEngine;

/// <summary>Shared forge prices and effect scaling. Card assets are never modified by refinement.</summary>
public static class CardRefinementRules
{
    public const int MaxLevel = 5;
    public static int Level(int value) => Mathf.Clamp(value, 1, MaxLevel);
    public static int DuplicateCost(int level) => Level(level) < MaxLevel ? Level(level) : 0;
    public static int CoinCost(int level) => DuplicateCost(level) * 50;
    public static float Multiplier(int level) => 1f + .2f * (Level(level) - 1);
    public static int ScalePower(int amount, int level) => amount <= 0 ? amount : Mathf.RoundToInt(amount * Multiplier(level));
    public static int ScaleUtility(int amount, int level) => amount <= 0 ? amount : amount + Level(level) - 1;

    public static bool CanRefine(CardData card)
    {
        if (card == null) return false;
        if (Supported(card.ManualTargetEffect)) return true;
        return card.OtherEffects != null && card.OtherEffects.Exists(e => e != null && Supported(e.Effect));
    }

    static bool Supported(Effect effect) => effect is DealDamageEffect || effect is HealEffect ||
        effect is AddStatusEffectEffect || effect is DrawCardsEffect || effect is GainManaEffect ||
        effect is StatusComboDamageEffect;

    public static bool CanAfford(CardData card, int level, int quantity, int coins, out string reason)
    {
        if (!CanRefine(card)) { reason = "This card has no refinable effects."; return false; }
        if (quantity < 1) { reason = "You do not own this card."; return false; }
        if (Level(level) >= MaxLevel) { reason = "This card is already at the maximum level."; return false; }
        int copies = DuplicateCost(level);
        if (quantity <= copies) { reason = "Requires " + copies + " spare " + (copies == 1 ? "copy" : "copies") + ". Keep one copy to use."; return false; }
        if (coins < CoinCost(level)) { reason = "Requires " + CoinCost(level) + " coins."; return false; }
        reason = "Ready to refine. One copy is always kept.";
        return true;
    }

    public static GameAction RefineAction(GameAction action, int level)
    {
        level = Level(level);
        if (level == 1 || action == null) return action;
        if (action is DealDamageGA damage)
        {
            damage.CardLevel = level;
            if (damage.CardSource != null && damage.Targets != null && damage.Targets.Count > 0)
                damage.Amount = damage.CardSource.GetCalculatedDamage(damage.Caster, damage.Targets[0], level);
        }
        else if (action is HealGA heal) return new HealGA(ScalePower(heal.Amount, level), heal.Targets);
        else if (action is AddStatusEffectGA status)
        {
            if (status.StatusEffectType == StatusEffectType.NATURE)
                return new AddStatusEffectGA(status.StatusEffectType, status.StackCount, status.Targets,
                    ScalePower(status.NatureHealAmount, level));
            return new AddStatusEffectGA(status.StatusEffectType, ScaleUtility(status.StackCount, level),
                status.Targets, status.NatureHealAmount);
        }
        else if (action is DrawCardsGA draw) return new DrawCardsGA(ScaleUtility(draw.Amount, level));
        else if (action is GainManaGA gain) return new GainManaGA(ScaleUtility(gain.Amount, level));
        else if (action is StatusComboDamageGA combo)
        {
            combo.BaseDamage = ScalePower(combo.BaseDamage, level);
            combo.DamagePerStack = ScalePower(combo.DamagePerStack, level);
            return combo;
        }
        return action;
    }

    public static string Describe(CardData card, int level)
    {
        if (card == null) return "Unavailable card";
        var lines = new List<string>();
        DescribeEffect(card.ManualTargetEffect, card, level, lines);
        if (card.OtherEffects != null)
            foreach (var effect in card.OtherEffects)
                if (effect != null) DescribeEffect(effect.Effect, card, level, lines);
        return lines.Count > 0 ? string.Join("\n", lines) : "No refinable effects";
    }

    static void DescribeEffect(Effect effect, CardData card, int level, List<string> lines)
    {
        if (effect is DealDamageEffect)
        {
            string value = card.damageType == DamageType.Flat
                ? card.GetCalculatedDamage(null, null, level).ToString()
                : (card.percentDamage * card.damageMultiplier * Multiplier(level)).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "% ATK";
            lines.Add("Damage: " + value);
        }
        else if (effect is HealEffect heal) lines.Add("Healing: " + ScalePower(heal.BaseAmount, level));
        else if (effect is AddStatusEffectEffect status)
        {
            if (status.Kind == StatusEffectType.NATURE)
                lines.Add("Nature: heal " + ScalePower(status.NatureHealAmount, level) + " after " + status.BaseStacks + " turns");
            else if (status.Kind == StatusEffectType.BLEED)
                lines.Add("Bleed: " + ScaleUtility(status.BaseStacks, level) + " stacks; deal that much damage at turn end, then clear");
            else if (status.Kind == StatusEffectType.FROST)
                lines.Add("Frost: " + ScaleUtility(status.BaseStacks, level) + " stacks; reduce the next attack by that amount, then clear");
            else if (status.Kind == StatusEffectType.POISON)
                lines.Add("Poison: " + ScaleUtility(status.BaseStacks, level) + " stacks; deal current stacks as damage each turn, lose 1 stack");
            else
                lines.Add(status.Kind + " stacks: " + ScaleUtility(status.BaseStacks, level));
        }
        else if (effect is DrawCardsEffect draw) lines.Add("Cards drawn: " + ScaleUtility(draw.BaseAmount, level));
        else if (effect is GainManaEffect gain) lines.Add("Mana gained: +" + ScaleUtility(gain.Amount, level) + " (temporary; resets to 5 after the enemy turn)");
        else if (effect is StatusComboDamageEffect combo)
            lines.Add("Combo damage: " + ScalePower(combo.BaseDamage, level) + " + " +
                ScalePower(combo.DamagePerStack, level) + " per " + combo.BonusStatus + " stack" +
                (combo.ConsumeStatusStacks ? " (consumes stacks)" : ""));
    }
}
