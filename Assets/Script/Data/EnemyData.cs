using System;
using UnityEngine;

public enum EnemyAttackStyle { Melee, Ranged }

[CreateAssetMenu(menuName = "Data/Enemy")]
public class EnemyData : ScriptableObject
{
    [field: SerializeField] public Sprite Image {get; private set;}
    [field: SerializeField] public int Health {get; private set;}
    [field: SerializeField] public int AttackPower {get; private set;}
    [Header("Battle Animation")]
    public Sprite[] idleFrames;
    public Sprite[] attackFrames;
    [Min(1f)] public float battleFramesPerSecond = 12f;
    public bool hovering;
    public Material battleMaterial;
    [Header("Attack Behaviour")]
    public EnemyAttackStyle attackStyle = EnemyAttackStyle.Melee;
    [Header("Attack Projectile")]
    public Sprite[] projectileFrames;
    public Sprite[] impactFrames;
    [Min(1f)] public float projectileSpeed = 10f;
    [Min(1f)] public float projectileFrameRate = 16f;
    public float projectileArc;

}
