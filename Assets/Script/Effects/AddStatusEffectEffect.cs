using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AddStatusEffectEffect : Effect
{
    [SerializeField] private StatusEffectType statusEffectType;
    [SerializeField] private int stackCount = 1;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster, CardData cardSource = null)
    {
        return new AddStatusEffectGA(statusEffectType, stackCount, targets);
    }
}