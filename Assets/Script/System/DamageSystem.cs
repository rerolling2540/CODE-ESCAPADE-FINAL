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
        foreach (var target in dealDamageGA.Targets)
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
