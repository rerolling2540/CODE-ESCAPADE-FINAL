public sealed class TickStatusEffectGA : GameAction
{
    public StatusEffectType Type { get; }
    public CombatantView Target { get; }
    public int Damage { get; }
    public int StacksToRemove { get; }

    public TickStatusEffectGA(StatusEffectType type, CombatantView target, int damage, int stacksToRemove)
    {
        Type = type;
        Target = target;
        Damage = damage;
        StacksToRemove = stacksToRemove;
    }
}
