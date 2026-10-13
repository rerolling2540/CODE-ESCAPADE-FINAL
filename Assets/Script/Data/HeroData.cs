using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="Data/Hero")]
public class HeroData : ScriptableObject
{
    [field: SerializeField] public Sprite Image {get; private set; }
    [field: SerializeField] public int Health {get; private set; }
    [field: SerializeField] public List<CardData> Deck {get; private set;}
    [Header("Battle Animation")]
    public Sprite[] idleFrames;
    public Sprite[] attackFrames;
    [Min(1f)] public float battleFramesPerSecond = 12f;

}
