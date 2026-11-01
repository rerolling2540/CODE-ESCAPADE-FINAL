using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class GainManaEffect : Effect
{
    [Tooltip("Added after this card's cost is paid. Mana above 5 remains until the enemy turn ends, then resets to 5.")]
    [SerializeField, Min(1)] private int amount = 2;
    public int Amount => amount;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster, CardData cardSource = null)
    {
        return new GainManaGA(amount);
    }
}
