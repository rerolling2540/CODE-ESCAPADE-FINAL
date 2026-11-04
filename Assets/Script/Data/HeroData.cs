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
    [Tooltip("Weapon recovery animation played after the hero returns from a battle attack.")]
    public Sprite[] sheatheFrames;
    [Min(1f)] public float battleFramesPerSecond = 12f;
    [Tooltip("Optional seconds per attack frame, for weapon-specific anticipation and recovery.")]
    public float[] attackFrameDurations;
    [Tooltip("Zero-based attack frame that releases the card VFX and attack sound.")]
    [Min(0)] public int attackFeedbackFrame;
    public bool hoverBattleAnimation;
    [Header("Melee Battle Movement")]
    public bool dashToBattleTarget;
    public Sprite[] battleDashFrames;

}
