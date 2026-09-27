using System.Collections.Generic;
using UnityEngine;

public abstract class Effect
{
    // Added 'CardData cardSource = null' to support percentage calculations
    public abstract GameAction GetGameAction(List<CombatantView> targets, CombatantView caster, CardData cardSource = null);
}
