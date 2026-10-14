using UnityEngine;

public enum HeroLanguageClass { Java, CSharp, Python }

[CreateAssetMenu(menuName = "Data/Hero Class Data")]
public class HeroClassData : ScriptableObject
{
    [field: SerializeField] public HeroLanguageClass LanguageClass { get; private set; }
    [field: SerializeField] public int BaseMaxHP { get; private set; } = 100;
    [field: SerializeField] public int HPGainPerLevel { get; private set; } = 15;
    [field: SerializeField, Min(0)] public int BaseAttackBonus { get; private set; } = 0;
    [field: SerializeField, Min(0)] public int AttackBonusGainPerLevel { get; private set; } = 0;
    [field: SerializeField, Min(0)] public int BaseAttackPower { get; private set; } = 30;
    [field: SerializeField, Min(0)] public int AttackPowerGainPerLevel { get; private set; } = 4;
    [field: SerializeField, Min(0)] public int BaseDefense { get; private set; } = 0;
    [field: SerializeField, Min(0)] public int DefenseGainPerLevel { get; private set; } = 0;

    public int GetHealthForLevel(int level)
    {
        return GetScaledStat(BaseMaxHP, HPGainPerLevel, level);
    }

    public int GetAttackBonusForLevel(int level) => GetScaledStat(BaseAttackBonus, AttackBonusGainPerLevel, level);

    public int GetAttackPowerForLevel(int level) => GetScaledStat(BaseAttackPower, AttackPowerGainPerLevel, level);

    public int GetDefenseForLevel(int level) => GetScaledStat(BaseDefense, DefenseGainPerLevel, level);

    private static int GetScaledStat(int baseValue, int gainPerLevel, int level)
    {
        return Mathf.Max(0, baseValue) + (Mathf.Max(0, gainPerLevel) * (Mathf.Max(1, level) - 1));
    }
}
