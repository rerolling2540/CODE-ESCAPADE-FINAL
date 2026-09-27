using System.Collections.Generic;
using UnityEngine;

public class DealDamageGA : GameAction, IHaveCaster
{
    public CardData CardSource { get; set; }
    public int CardLevel { get; set; }
    public int Amount { get; set; }
    public List<CombatantView> Targets { get; set; }
    public CombatantView Caster { get; private set; }

    // Constructor for Card-based damage (Dynamic scaling per target)
    public DealDamageGA(CardData cardSource, List<CombatantView> targets, CombatantView caster, int cardLevel = 1)
    {
        CardSource = cardSource;
        Targets = new List<CombatantView>(targets);
        Caster = caster;
        CardLevel = cardLevel;

        if (cardSource != null && targets != null && targets.Count > 0)
        {
            Amount = cardSource.GetCalculatedDamage(targets[0], cardLevel);
        }
    }

    // Constructor for direct int damage
    public DealDamageGA(int amount, List<CombatantView> targets, CombatantView caster)
    {
        Amount = amount;
        Targets = new List<CombatantView>(targets);
        Caster = caster;
        CardSource = null;
        CardLevel = 1;
    }

    public int GetDamageForTarget(CombatantView target)
    {
        if (CardSource != null && target != null)
        {
            return CardSource.GetCalculatedDamage(target, CardLevel);
        }

        return Amount;
    }
}