using UnityEngine;

public enum HeroLanguageClass { Java, CSharp, Python }

[CreateAssetMenu(menuName = "Data/Hero Class Data")]
public class HeroClassData : ScriptableObject
{
    [field: SerializeField] public HeroLanguageClass LanguageClass { get; private set; }
    [field: SerializeField] public int BaseMaxHP { get; private set; } = 100;
    [field: SerializeField] public int HPGainPerLevel { get; private set; } = 15;

    public int GetHealthForLevel(int level)
    {
        return BaseMaxHP + (HPGainPerLevel * (level - 1));
    }
}