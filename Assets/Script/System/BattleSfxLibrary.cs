using System;
using System.Collections.Generic;
using UnityEngine;

public enum BattleSound
{
    CardDraw, CardPlay, TurnEnd, SwordSwing, ArrowRelease, MagicCast, FireCast,
    Hit, MagicHit, FireHit, Block, Armor, Heal, Correct, Wrong, EnemyDefeat, Victory, Defeat,
    CardHover, CardActivate, QuestionOpen, QuestionClose, AnswerSelect
}

[CreateAssetMenu(menuName = "Data/Battle SFX Library")]
public class BattleSfxLibrary : ScriptableObject
{
    [Serializable] public class Cue
    {
        public BattleSound sound;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 0.65f;
    }
    public List<Cue> cues = new List<Cue>();
}
