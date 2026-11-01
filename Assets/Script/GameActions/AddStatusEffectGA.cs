using System.Collections.Generic;
using UnityEngine;

public class AddStatusEffectGA : GameAction
{
    public StatusEffectType StatusEffectType { get; private set; }
    public int StackCount { get; set; }
    public int NatureHealAmount { get; set; }
    public List<CombatantView> Targets { get; private set; }

    public AddStatusEffectGA(StatusEffectType statusEffectType, int stackCount, List<CombatantView> targets, int natureHealAmount = 10)
    {
        StatusEffectType = statusEffectType;
        StackCount = stackCount;
        Targets = targets;
        NatureHealAmount = natureHealAmount;
    }
}
