using System.Collections.Generic;
using UnityEngine;

public class PerformEffectGA : GameAction
{
    public Effect Effect { get; private set; }
    public List<CombatantView> Targets { get; private set; }
    public CardData CardSource { get; private set; }

    public PerformEffectGA(Effect effect, List<CombatantView> targets, CardData cardSource = null)
    {
        Effect = effect;
        Targets = targets;
        CardSource = cardSource;
    }
}