using System.Collections;
using UnityEngine;

public class EffectSystem : MonoBehaviour
{
    void OnEnable()
    {
        ActionSystem.AttachPerformer<PerformEffectGA>(PerformEffectPerformer);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<PerformEffectGA>();
    }

    private IEnumerator PerformEffectPerformer(PerformEffectGA performEffectGA)
    {
        // Passed 'performEffectGA.CardSource' so DealDamageEffect gets the CardData
        GameAction effectAction = performEffectGA.Effect.GetGameAction(
            performEffectGA.Targets, 
            HeroSystem.Instance.HeroView, 
            performEffectGA.CardSource
        );

        ActionSystem.Instance.AddReaction(effectAction);
        yield return null;
    }

}
