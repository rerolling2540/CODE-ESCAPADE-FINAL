using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HealEffect : Effect
{
    [SerializeField, Min(1)] private int healAmount = 10;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster, CardData cardSource = null)
    {
        return new HealGA(healAmount, targets);
    }
}
