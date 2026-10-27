using System.Collections;
using UnityEngine;

public class EffectSystem : MonoBehaviour
{
    void OnEnable()
    {
        ActionSystem.AttachPerformer<PerformEffectGA>(PerformEffectPerformer);
        ActionSystem.AttachPerformer<HealGA>(HealPerformer);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<PerformEffectGA>();
        ActionSystem.DetachPerformer<HealGA>();
    }

    private IEnumerator PerformEffectPerformer(PerformEffectGA performEffectGA)
    {
        // Passed 'performEffectGA.CardSource' so DealDamageEffect gets the CardData
        GameAction effectAction = performEffectGA.Effect.GetGameAction(
            performEffectGA.Targets, 
            HeroSystem.Instance.HeroView, 
            performEffectGA.CardSource
        );

        ActionSystem.Instance.AddReaction(CardRefinementRules.RefineAction(effectAction, performEffectGA.CardLevel));
        yield return null;
    }

    private IEnumerator HealPerformer(HealGA healGA)
    {
        if (healGA.Targets == null) yield break;

        foreach (CombatantView target in healGA.Targets)
        {
            if (target != null)
                target.Heal(healGA.Amount);
            yield return null;
        }
    }

}
