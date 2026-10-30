public sealed class NatureTurnGA : GameAction
{
    public CombatantView Target { get; }

    public NatureTurnGA(CombatantView target)
    {
        Target = target;
    }
}
