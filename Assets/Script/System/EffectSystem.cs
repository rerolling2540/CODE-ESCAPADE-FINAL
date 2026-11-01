using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectSystem : MonoBehaviour
{
    void OnEnable()
    {
        ActionSystem.AttachPerformer<PerformEffectGA>(PerformEffectPerformer);
        ActionSystem.AttachPerformer<HealGA>(HealPerformer);
        ActionSystem.AttachPerformer<StatusComboDamageGA>(StatusComboDamagePerformer);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<PerformEffectGA>();
        ActionSystem.DetachPerformer<HealGA>();
        ActionSystem.DetachPerformer<StatusComboDamageGA>();
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
            {
                target.Heal(healGA.Amount);
                StatusEffectSystem.PlayHealAnimation(target);
            }
            yield return null;
        }
    }

    private IEnumerator StatusComboDamagePerformer(StatusComboDamageGA combo)
    {
        if (combo == null || combo.Targets == null) yield break;

        foreach (CombatantView target in combo.Targets)
        {
            if (target == null || target.CurrentHealth <= 0) continue;

            int statusStacks = target.GetStatusEffectStacks(combo.BonusStatus);
            long rawDamage = (long)combo.BaseDamage + (long)statusStacks * combo.DamagePerStack;
            int damage = (int)System.Math.Min(int.MaxValue, rawDamage);
            if (damage > 0)
            {
                DealDamageGA damageAction = new(damage, new List<CombatantView> { target }, combo.Caster);
                DamageSystem damageSystem = FindFirstObjectByType<DamageSystem>();
                if (damageSystem != null)
                    yield return damageSystem.ApplyDamageDirect(damageAction);
                else
                    target.Damage(damage);
            }

            if (combo.ConsumeStatusStacks && statusStacks > 0)
                target.RemoveStatusEffect(combo.BonusStatus, statusStacks);
        }
    }

}
