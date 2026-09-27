using System;
using UnityEngine;

[Serializable]
public class OnEnemyAttackCondition : PerkCondition
{
    public override bool SubConditionIsMet(GameAction gameAction)
    {
        // if attacker is above x health or something
        return true;
    }

    public override void SubscribeCondtion(Action<GameAction> reaction)
    {
        
        ActionSystem.SubscribedReaction<AttackHeroGA>(reaction, reactionTiming);
    }

    public override void UnSubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribedReaction<AttackHeroGA>(reaction, reactionTiming);
    }
}
