using UnityEngine;
public sealed class CharacterInfoData : ScriptableObject
{
 public HeroClassData battleClass;
 public int battleLevel=1;
 public HeroData[] heroes;
 public Texture2D[] portraits;
}
