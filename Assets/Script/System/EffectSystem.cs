using System.Collections;
using UnityEngine;

public class EffectSystem : MonoBehaviour
{
    void OnEnable()
    {
        ActionSystem.AttachPerformer<PerformEffectGA>(PerformEffectPerformer);
        ActionSystem.AttachPerformer<DealDamageGA>(DealDamagePerformer);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<PerformEffectGA>();
        ActionSystem.DetachPerformer<DealDamageGA>();
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

    private IEnumerator DealDamagePerformer(DealDamageGA dealDamageGA)
    {
        foreach (var target in dealDamageGA.Targets)
        {
            if (target == null) continue;

            // Calculates scaled damage using the target's HP values
            int damageToDeal = dealDamageGA.GetDamageForTarget(target);

            Debug.Log($"Dealing {damageToDeal} damage to {target.name}");

            // Directly call Damage method on target CombatantView
            target.Damage(damageToDeal);

            yield return new WaitForSeconds(0.1f);
        }
    }
}