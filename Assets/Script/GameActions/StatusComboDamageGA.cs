using System.Collections.Generic;

public sealed class StatusComboDamageGA : GameAction, IHaveCaster
{
    public int BaseDamage { get; set; }
    public StatusEffectType BonusStatus { get; }
    public int DamagePerStack { get; set; }
    public bool ConsumeStatusStacks { get; }
    public List<CombatantView> Targets { get; }
    public CombatantView Caster { get; }

    public StatusComboDamageGA(int baseDamage, StatusEffectType bonusStatus, int damagePerStack,
        bool consumeStatusStacks, List<CombatantView> targets, CombatantView caster)
    {
        BaseDamage = baseDamage;
        BonusStatus = bonusStatus;
        DamagePerStack = damagePerStack;
        ConsumeStatusStacks = consumeStatusStacks;
        Targets = targets != null ? new List<CombatantView>(targets) : new List<CombatantView>();
        Caster = caster;
    }
}
