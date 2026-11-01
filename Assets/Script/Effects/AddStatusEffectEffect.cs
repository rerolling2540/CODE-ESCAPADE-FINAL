using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AddStatusEffectEffect : Effect
{
    [SerializeField] private StatusEffectType statusEffectType;
    [Tooltip("Stacks applied for statuses other than Nature. Bleed deals this much damage per stack at the end of the affected combatant's turn.")]
    [SerializeField] private int stackCount = 1;
    [Tooltip("Nature only: number of player turns until the heal triggers.")]
    [SerializeField, Min(1)] private int natureTurnsUntilHeal = 3;
    [SerializeField, Min(1)] private int natureHealAmount = 10;
    public StatusEffectType Kind => statusEffectType;
    public int BaseStacks => statusEffectType == StatusEffectType.NATURE ? natureTurnsUntilHeal : stackCount;
    public int NatureTurnsUntilHeal => natureTurnsUntilHeal;
    public int NatureHealAmount => natureHealAmount;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster, CardData cardSource = null)
    {
        int amount = statusEffectType == StatusEffectType.NATURE ? natureTurnsUntilHeal : stackCount;
        return new AddStatusEffectGA(statusEffectType, amount, targets, natureHealAmount);
    }
}
