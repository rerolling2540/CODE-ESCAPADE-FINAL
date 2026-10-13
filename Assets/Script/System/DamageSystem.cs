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
            target.Damage(dealDamageGA.GetDamageForTarget(target));
            if (damageVFX != null) Instantiate(damageVFX, target.transform.position, Quaternion.identity);
            yield return new WaitForSeconds(0.15f);
        }
    }
}
