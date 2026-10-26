using System.Collections;
using UnityEngine;

public class DamageSystem : MonoBehaviour
{
   [SerializeField] private GameObject damageVFX;

    void OnEnable()
    {
        ActionSystem.AttachPerformer<DealDamageGA>(DealDamagePerformer);
    }
    void OnDisable()
    {
        ActionSystem.DetachPerformer<DealDamageGA>();
    }

    private IEnumerator DealDamagePerformer(DealDamageGA dealDamageGA)
    {
        yield return ApplyDamageDirect(dealDamageGA);
    }

    // Shared damage application for effects that are already running inside a
    // card/enemy action. This avoids queuing a second action from inside the
    // current reaction and guarantees health changes before the turn continues.
    public IEnumerator ApplyDamageDirect(DealDamageGA dealDamageGA)
    {
        if (dealDamageGA == null || dealDamageGA.Targets == null) yield break;

        foreach (CombatantView target in dealDamageGA.Targets)
        {
            if (target == null || target.CurrentHealth <= 0) continue;
            int previousHealth = target.CurrentHealth;
            target.Damage(dealDamageGA.GetDamageForTarget(target));
            if (target.CurrentHealth < previousHealth) BattleBloodEffect.Play(target);
            BattleAudio.Impact(dealDamageGA, target, previousHealth);
            if (damageVFX != null) Instantiate(damageVFX, target.transform.position, Quaternion.identity);
            yield return new WaitForSeconds(0.15f);
        }
    }
}
