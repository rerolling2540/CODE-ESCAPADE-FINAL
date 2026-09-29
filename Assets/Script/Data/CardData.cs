using System.Collections.Generic;
using System.Runtime.InteropServices;
using SerializeReferenceEditor;
using UnityEngine;

public enum DamageType { Flat, MaxHealthPercent, CurrentHealthPercent }

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
    [Tooltip("Percentage value (e.g., 25.5 = 25.5% of target HP)")]
    [Range(0f, 100f)] 
    public float percentDamage = 10f;

    [Header("Multipliers")]
    [Tooltip("1.0 = 100%, 1.2 = 120%, 1.5 = 150%")]
    public float damageMultiplier = 1.0f;

    public int GetCalculatedDamage(CombatantView target = null, int cardLevel = 1)
    {
        float calculatedBase = 0f;

        switch (damageType)
        {
            case DamageType.Flat:
                calculatedBase = baseDamage;
                break;

            case DamageType.MaxHealthPercent:
                if (target != null)
                {
                    calculatedBase = target.MaxHealth * (percentDamage / 100f);
                }
                break;

            case DamageType.CurrentHealthPercent:
                if (target != null)
                {
                    calculatedBase = target.CurrentHealth * (percentDamage / 100f);
                }
                break;
        }

        float levelBonus = (cardLevel - 1) * 2f;
        float rawDamage = (calculatedBase + levelBonus) * damageMultiplier;

        return Mathf.RoundToInt(rawDamage);
    }
}