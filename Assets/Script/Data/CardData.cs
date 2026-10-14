using System.Collections.Generic;
using System.Runtime.InteropServices;
using SerializeReferenceEditor;
using UnityEngine;

public enum DamageType
{
    Flat = 0,
    AttackPercent = 1,
    [InspectorName("Attack Percent (legacy card data)")]
    AttackPercentLegacy = 2
}

[CreateAssetMenu(menuName = "Data/Card")]
public class CardData : ScriptableObject
{
    [field: SerializeField] public string Description { get; private set; }
    [field: SerializeField] public int Mana { get; private set; }
    [field: SerializeField] public Sprite Image { get; private set; }
    [field: SerializeReference, SR] public Effect ManualTargetEffect { get; private set; } = null;
    [field: SerializeField] public List<AutoTargetEffect> OtherEffects { get; private set; }

    [Header("Damage Scaling Type")]
    public DamageType damageType = DamageType.Flat;

    [Header("Flat Damage Settings")]
    public int baseDamage = 10;

    [Header("Percent Damage Settings")]
    [Tooltip("Percentage of the caster's Attack Power (e.g., 120 = 120% of Attack Power)")]
    [Range(0f, 500f)]
    public float percentDamage = 10f;

    [Header("Multipliers")]
    [Tooltip("1.0 = 100%, 1.2 = 120%, 1.5 = 150%")]
    public float damageMultiplier = 1.0f;

    public int GetCalculatedDamage(CombatantView caster, CombatantView target = null, int cardLevel = 1)
    {
        float calculatedBase = 0f;

        switch (damageType)
        {
            case DamageType.Flat:
                calculatedBase = baseDamage;
                break;

            case DamageType.AttackPercent:
            case DamageType.AttackPercentLegacy:
                if (caster != null)
                {
                    calculatedBase = caster.AttackPower * (percentDamage / 100f);
                }
                break;
        }

        float levelBonus = damageType == DamageType.Flat ? (cardLevel - 1) * 2f : 0f;
        float rawDamage = (calculatedBase + levelBonus) * damageMultiplier;

        return Mathf.RoundToInt(rawDamage);
    }
}
