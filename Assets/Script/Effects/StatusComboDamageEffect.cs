using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class StatusComboDamageEffect : Effect
{
    [Tooltip("Flat damage before the bonus from matching status stacks.")]
    [SerializeField, Min(0)] private int baseDamage = 0;
    [Tooltip("The chosen target takes Damage Per Stack for every stack of this status effect.")]
    [SerializeField] private StatusEffectType bonusStatus = StatusEffectType.BLEED;
    [SerializeField, Min(1)] private int damagePerStack = 3;
    [SerializeField] private bool consumeStatusStacks;

    public int BaseDamage => baseDamage;
    public StatusEffectType BonusStatus => bonusStatus;
    public int DamagePerStack => damagePerStack;
    public bool ConsumeStatusStacks => consumeStatusStacks;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster, CardData cardSource = null)
    {
        return new StatusComboDamageGA(baseDamage, bonusStatus, damagePerStack, consumeStatusStacks, targets, caster);
    }
}
