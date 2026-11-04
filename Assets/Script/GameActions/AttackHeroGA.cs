using UnityEngine;

public class AttackHeroGA : GameAction, IHaveCaster
{
    public EnemyView Attacker {get; private set;}

    public CombatantView Caster {get; private set;}
    public bool IsCounterattack {get; private set;}
    public bool CanAttack => Attacker != null && Attacker.CurrentHealth > 0 && !Attacker.IsBroken &&
        (IsCounterattack || Attacker.OpeningActionResolved);
    public AttackHeroGA(EnemyView attacker, bool isCounterattack = false)
    {
        Attacker = attacker;
        Caster = Attacker;
        IsCounterattack = isCounterattack;
    }

}
