using System.Collections.Generic;
using System.Runtime.InteropServices;
using SerializeReferenceEditor;
using UnityEngine;

public enum DamageType
{
    Flat = 0,
    AttackPercent = 1,
    [InspectorName("Attack Percent (legacy card data)")]
    AttackPercentLegacy = 2,
    [InspectorName("Target Maximum Health Percent")]
    TargetHealthPercent = 3
}

public enum CardAttackVfxType
{
    Auto = 0,
    ArrowRain = 1,
    Fireball = 2,
    Arrow = 3,
    ArcaneBolt = 4,
    MeleeSlash = 5,
    Impact = 6
}

[CreateAssetMenu(menuName = "Data/Card")]
public class CardData : ScriptableObject
{
    [Header("Question difficulty bounds (1 beginner, 2 intermediate, 3 advanced)")]
    [Range(1, 3)] public int minimumQuestionTier = 1;
    [Range(1, 3)] public int maximumQuestionTier = 3;

    [field: SerializeField] public string Description { get; private set; }
    [field: SerializeField] public int Mana { get; private set; }
    [field: SerializeField] public Sprite Image { get; private set; }
    [field: SerializeReference, SR] public Effect ManualTargetEffect { get; private set; } = null;
    [field: SerializeField] public List<AutoTargetEffect> OtherEffects { get; private set; }

    [Header("Attack Visual Effect")]
    [Tooltip("Chooses the projectile or strike effect played when this attack card resolves. Auto picks one from the card name and selected hero.")]
    public CardAttackVfxType attackVfxType = CardAttackVfxType.Auto;
    [Tooltip("Optional imported attack animation played on each affected enemy.")]
    public Sprite[] attackImpactFrames;
    [Range(8f,40f)] public float attackImpactFramesPerSecond = 24f;
    [Range(.5f,2f)] public float attackImpactScale = 1f;

    [Header("Damage Scaling Type")]
    public DamageType damageType = DamageType.Flat;

    [Header("Flat Damage Settings")]
    public int baseDamage = 10;

    [Header("Percent Damage Settings")]
    [Tooltip("Used as a percentage of Attack Power for AttackPercent, or a percentage of the target's maximum Health for TargetHealthPercent.")]
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

            case DamageType.TargetHealthPercent:
                if (target != null)
                {
                    calculatedBase = target.MaxHealth * (percentDamage / 100f);
                }
                break;
        }

        float rawDamage = calculatedBase * damageMultiplier * CardRefinementRules.Multiplier(cardLevel);

        return Mathf.RoundToInt(rawDamage);
    }
}
