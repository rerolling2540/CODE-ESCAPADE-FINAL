using System;
using UnityEngine;

public enum EnemyAttackStyle { Melee, Ranged }
public enum EnemyOpeningAction { Random, Shield, DamageBuff }

[CreateAssetMenu(menuName = "Data/Enemy")]
public class EnemyData : ScriptableObject
{
    [field: SerializeField] public Sprite Image {get; private set;}
    [field: SerializeField] public int Health {get; private set;}
    [field: SerializeField] public int AttackPower {get; private set;}
    [Header("Battle Animation")]
    [Tooltip("Enable when the original, unflipped battle sprites face left.")]
    public bool battleSpriteFacesLeft;
    public Sprite[] idleFrames;
    [Header("Overworld Patrol Animation")]
    public Sprite[] walkFrames;
    public Sprite[] attackFrames;
    [Min(1f)] public float battleFramesPerSecond = 12f;
    public bool hovering;
    [Min(0)] public int experienceReward = 25;
    public Material battleMaterial;
    [Min(0.1f)] public float battleVisualScale = 1f;
    [Header("Attack Behaviour")]
    public EnemyAttackStyle attackStyle = EnemyAttackStyle.Melee;
    [Header("Battle Intent")]
    [Min(1)] public int attackCountdown = 15;
    [Min(1)] public int breakMeterMax = 100;
    [Header("Attack Projectile")]
    public Sprite[] projectileFrames;
    public Sprite[] impactFrames;
    [Min(1f)] public float projectileSpeed = 10f;
    [Min(1f)] public float projectileFrameRate = 16f;
    public float projectileArc;

    [Header("Periodic status assault (turns 1, 3, 5, ...)")]
    [Tooltip("When enabled, this enemy applies its configured status to the player on its status-assault turns.")]
    public bool appliesStatusEffect;
    public StatusEffectType onHitStatus = StatusEffectType.BURN;
    [Min(1)] public int onHitStacks = 1;
    [Min(1)] public int natureHealAmount = 10;
    [Header("First player turn action (instead of attacking)")]
    public EnemyOpeningAction openingAction = EnemyOpeningAction.Random;
    [Min(1)] public int openingShield = 8;
    [Min(1)] public int openingDamageBonus = 3;
    [Min(1)] public int openingDamageBuffAttacks = 3;

}
