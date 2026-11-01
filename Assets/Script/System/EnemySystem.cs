using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class EnemySystem : Singleton<EnemySystem>
{
    [SerializeField] private EnemyBoardView enemyBoardView;
    public List<EnemyView> Enemies => enemyBoardView.EnemyViews;
    public EnemyView CurrentEnemy
    {
        get
        {
            return enemyBoardView.EnemyViews[0];
        }
    }

    void OnEnable()
    {
        ActionSystem.AttachPerformer<EnemyTurnGA>(EnemyTurnPerformer);
        ActionSystem.AttachPerformer<AttackHeroGA>(AttackHeroPerformer);
        ActionSystem.AttachPerformer<KillEnemyGA>(KillEnemyPerformer);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<EnemyTurnGA>();
        ActionSystem.DetachPerformer<AttackHeroGA>();
        ActionSystem.DetachPerformer<KillEnemyGA>();
    }

    public void Setup(List<EnemyData> enemyDatas)
    {
        foreach (var enemyData in enemyDatas)
        {
            enemyBoardView.AddEnemy(enemyData);
        }
    }
    // Performers
    private IEnumerator EnemyTurnPerformer(EnemyTurnGA enemyTurnGA)
    {
        foreach (var enemy in enemyBoardView.EnemyViews)
        {
            if (enemy == null || enemy.CurrentHealth <= 0) continue;
            int burnStacks = enemy.GetStatusEffectStacks(StatusEffectType.BURN);
            if (burnStacks > 0)
            {
                ApplyBurnGA applyBurnGA = new(burnStacks, enemy);
                ActionSystem.Instance.AddReaction(applyBurnGA);
            }
            QueueStatusTick(enemy, StatusEffectType.POISON, removeAllStacks: false);
            AttackHeroGA attackHeroGA = new(enemy);
            ActionSystem.Instance.AddReaction(attackHeroGA);
            // Bleed detonates after this enemy finishes its turn, then all stacks clear.
            QueueStatusTick(enemy, StatusEffectType.BLEED, removeAllStacks: true);
        }
        yield return null;
    }

    private static void QueueStatusTick(CombatantView target, StatusEffectType type, bool removeAllStacks)
    {
        int stacks = target != null ? target.GetStatusEffectStacks(type) : 0;
        if (stacks <= 0) return;
        int stacksToRemove = removeAllStacks ? stacks : 1;
        ActionSystem.Instance.AddReaction(new TickStatusEffectGA(type, target, stacks, stacksToRemove));
    }

    private IEnumerator AttackHeroPerformer(AttackHeroGA attackHeroGA)
    {
        EnemyView attacker = attackHeroGA.Attacker;

        if (attacker == null || attacker.CurrentHealth <= 0)
            yield break;

        CombatantView hero = HeroSystem.Instance.HeroView;
        if (hero == null || hero.CurrentHealth <= 0) yield break;
        Vector3 startPosition = attacker.transform.position;
        bool melee = !attacker.IsRanged;
        Tween movement = null;
        attacker.FaceTowards(hero);
        hero.FaceTowards(attacker);
        try
        {
            if (melee)
            {
                // Approach the player's front, accounting for sprite offsets and size.
                float side = attacker.VisualCenter.x >= hero.VisualCenter.x ? 1f : -1f;
                float spacing = hero.BattleBodyBounds.extents.x + attacker.BattleBodyBounds.extents.x * 0.55f + 0.15f;
                Vector3 destination = startPosition;
                destination.x += hero.VisualCenter.x + side * spacing - attacker.VisualCenter.x;
                destination.y += hero.BattleGroundY - attacker.BattleGroundY;
                float approachDuration = Mathf.Clamp(Vector3.Distance(startPosition, destination) / 14f, 0.22f, 0.45f);
                movement = attacker.transform.DOMove(destination, approachDuration).SetEase(Ease.InOutSine);
                yield return movement.WaitForCompletion();
                yield return new WaitForSeconds(0.08f);
            }

            if (attacker == null || attacker.CurrentHealth <= 0 || hero == null || hero.CurrentHealth <= 0) yield break;
            // Melee attack frames contain the weapon slash; ranged frames launch the projectile.
            BattleAudio.Play(attacker.IsRanged ? BattleSound.MagicCast : BattleSound.SwordSwing);
            yield return attacker.PlayAttackAnimation();
            if (attacker == null || attacker.CurrentHealth <= 0 || hero == null || hero.CurrentHealth <= 0) yield break;

            if (!melee)
                yield return attacker.PlayProjectile(hero);
            else
            {
                yield return new WaitForSeconds(0.08f);
                movement = attacker.transform.DOMove(startPosition, 0.3f).SetEase(Ease.InOutQuad);
                yield return movement.WaitForCompletion();
            }
        }
        finally
        {
            if (movement != null && movement.IsActive()) movement.Kill();
            if (melee && attacker != null) attacker.transform.position = startPosition;
        }

        if (attacker == null || attacker.CurrentHealth <= 0 || hero == null || hero.CurrentHealth <= 0) yield break;
        int frostStacks = attacker.GetStatusEffectStacks(StatusEffectType.FROST);
        int attackDamage = Mathf.Max(1, attacker.AttackPower - frostStacks);
        DealDamageGA dealDamageGA = new(
            attackDamage,
            new() { HeroSystem.Instance.HeroView },
            attackHeroGA.Caster);
        DamageSystem damageSystem = FindFirstObjectByType<DamageSystem>();
        if (damageSystem != null)
            yield return damageSystem.ApplyDamageDirect(dealDamageGA);
        else
            HeroSystem.Instance.HeroView.Damage(attackDamage);

        // Frost suppresses the next attack; its stacks are consumed after damage lands.
        if (frostStacks > 0)
            attacker.RemoveStatusEffect(StatusEffectType.FROST, frostStacks);
    }
    private IEnumerator KillEnemyPerformer(KillEnemyGA killEnemyGA)
    {
        yield return enemyBoardView.RemoveEnemy(killEnemyGA.EnemyView);
    }
}
