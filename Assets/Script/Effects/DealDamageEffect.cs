using System.Collections.Generic;
using UnityEngine;

public class DealDamageEffect : Effect
{
    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster, CardData cardSource = null)
    {
        return new DealDamageGA(cardSource, targets, caster);
    }
}