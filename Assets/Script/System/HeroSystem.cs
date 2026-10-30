using System;
using UnityEngine;


public class HeroSystem : Singleton<HeroSystem>
{
    [field: SerializeField] public HeroView HeroView {get; private set;}
    void OnEnable()
    {
        ActionSystem.SubscribedReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.SubscribedReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
    }
    void OnDisable()
    {
        ActionSystem.UnSubscribedReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.UnSubscribedReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
    }
    public void SetUp(HeroData heroData)
    {
        HeroView.SetUp(heroData);
    }
    // Reactions
        private void EnemyTurnPreReaction(EnemyTurnGA enemyTurnGA)
    {
        DiscardAllCardsGA discardAllCardsGA = new();
        ActionSystem.Instance.AddReaction(discardAllCardsGA);

        // EnemyTurnGA begins when the player's turn ends. Resolve timed effects
        // before enemies act, then start the enemy phase.
        if (HeroView == null || HeroView.CurrentHealth <= 0) return;
        int burnStacks = HeroView.GetStatusEffectStacks(StatusEffectType.BURN);
        if (burnStacks > 0)
            ActionSystem.Instance.AddReaction(new ApplyBurnGA(burnStacks, HeroView));
        QueueStatusTick(StatusEffectType.POISON, removeAllStacks: false);
        QueueStatusTick(StatusEffectType.BLEED, removeAllStacks: true);
        if (HeroView.GetStatusEffectStacks(StatusEffectType.NATURE) > 0)
            ActionSystem.Instance.AddReaction(new NatureTurnGA(HeroView));
    }

    private void EnemyTurnPostReaction(EnemyTurnGA enemyTurnGA)
    {
        Debug.Log("EnemyTurnPostReaction");
        DrawCardsGA drawCardsGA = new(5);
        ActionSystem.Instance.AddReaction(drawCardsGA);
    }

    private void QueueStatusTick(StatusEffectType type, bool removeAllStacks)
    {
        int stacks = HeroView.GetStatusEffectStacks(type);
        if (stacks <= 0) return;
        int stacksToRemove = removeAllStacks ? stacks : 1;
        ActionSystem.Instance.AddReaction(new TickStatusEffectGA(type, HeroView, stacks, stacksToRemove));
    }

}
