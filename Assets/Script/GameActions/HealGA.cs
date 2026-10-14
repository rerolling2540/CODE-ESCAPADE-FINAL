using System.Collections.Generic;

public class HealGA : GameAction
{
    public int Amount { get; }
    public List<CombatantView> Targets { get; }

    public HealGA(int amount, List<CombatantView> targets)
    {
        Amount = amount;
        Targets = targets;
    }
}
