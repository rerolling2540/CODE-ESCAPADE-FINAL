using System;
using UnityEngine;


public class HeroSystem : Singleton<HeroSystem>
{
    [field: SerializeField] public HeroView HeroView {get; private set;}
    void OnEnable()
    {
        ActionSystem.SubscribedReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.SubscribedReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
        ActionSystem.SubscribedReaction<PlayCardGA>(PlayerCardPostReaction, ReactionTiming.POST);
    }
    void OnDisable()
    {
        ActionSystem.UnSubscribedReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.UnSubscribedReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
        ActionSystem.UnSubscribedReaction<PlayCardGA>(PlayerCardPostReaction, ReactionTiming.POST);
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

        // Nature healing remains turn-based. Harmful player statuses now resolve
        // after each card play, rather than once at the end of the turn.
        if (HeroView == null || HeroView.CurrentHealth <= 0) return;
        if (HeroView.GetStatusEffectStacks(StatusEffectType.NATURE) > 0)
            ActionSystem.Instance.AddReaction(new NatureTurnGA(HeroView));
    }

    private void PlayerCardPostReaction(PlayCardGA playCardGA)
    {
        if (HeroView == null || HeroView.CurrentHealth <= 0) return;

        int burnStacks = HeroView.GetStatusEffectStacks(StatusEffectType.BURN);
        if (burnStacks > 0)
            ActionSystem.Instance.AddReaction(new ApplyBurnGA(burnStacks, HeroView));
        QueueStatusTick(StatusEffectType.POISON, removeAllStacks: false);
        QueueStatusTick(StatusEffectType.BLEED, removeAllStacks: true);
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
