using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class EnemySystem : Singleton<EnemySystem>
{
    [SerializeField] private EnemyBoardView enemyBoardView;
    private int enemyPhaseCount;
    public List<EnemyView> Enemies => enemyBoardView.EnemyViews;
    public EnemyView CurrentEnemy
    {
        get
        {
            if (enemyBoardView == null || enemyBoardView.EnemyViews == null) return null;
            return enemyBoardView.EnemyViews.Find(enemy => enemy != null && enemy.CurrentHealth > 0);
        }
    }

    void OnEnable()
    {
        ActionSystem.AttachPerformer<EnemyTurnGA>(EnemyTurnPerformer);
        ActionSystem.AttachPerformer<AttackHeroGA>(AttackHeroPerformer);
        ActionSystem.AttachPerformer<KillEnemyGA>(KillEnemyPerformer);
        ActionSystem.AttachPerformer<ResetEnemyBreakMetersGA>(ResetEnemyBreakMetersPerformer);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<EnemyTurnGA>();
        ActionSystem.DetachPerformer<AttackHeroGA>();
        ActionSystem.DetachPerformer<KillEnemyGA>();
        ActionSystem.DetachPerformer<ResetEnemyBreakMetersGA>();
    }

    public void Setup(List<EnemyData> enemyDatas,string sourceScene=null)
    {
        enemyPhaseCount = 0;
        foreach (var enemyData in enemyDatas)
        {
            enemyBoardView.AddEnemy(enemyData,sourceScene);
        }
    }
    // Performers
    private IEnumerator EnemyTurnPerformer(EnemyTurnGA enemyTurnGA)
    {
        if (enemyBoardView == null || enemyBoardView.EnemyViews == null)
            yield break;

        // Status assault begins on the first enemy phase, then repeats every
        // other phase (turns 1, 3, 5, ...). A broken enemy cannot perform it.
        bool statusAssaultPhase = (++enemyPhaseCount % 2) == 1;
        CombatantView hero = HeroSystem.Instance != null ? HeroSystem.Instance.HeroView : null;

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
            EnemyData enemyData = enemy.Data;
            if (statusAssaultPhase && !enemy.IsBroken && hero != null && hero.CurrentHealth > 0 &&
                enemyData != null && enemyData.appliesStatusEffect)
            {
                ActionSystem.Instance.AddReaction(new AddStatusEffectGA(
                    enemyData.onHitStatus,
                    Mathf.Max(1, enemyData.onHitStacks),
                    new List<CombatantView> { hero },
                    Mathf.Max(1, enemyData.natureHealAmount)));
            }
            if (!enemy.OpeningActionResolved && !enemy.IsBroken)
            {
                enemy.ResolveOpeningAction();
                QueueStatusTick(enemy, StatusEffectType.BLEED, removeAllStacks: true);
                continue;
            }
            enemy.AdvanceAttackCountdown();
            // Every unbroken enemy retaliates when the player ends the turn.
            // The countdown still advances on every action for intent UI and
            // can trigger an additional attack mid-turn when it reaches zero.
            if (!enemy.IsBroken)
                ActionSystem.Instance.AddReaction(new AttackHeroGA(enemy));
            // Bleed detonates after this enemy finishes its turn, then all stacks clear.
            QueueStatusTick(enemy, StatusEffectType.BLEED, removeAllStacks: true);
        }
        // Break is a one-turn stagger. Restore meters after every enemy's turn effects
        // and attacks have resolved, so a broken enemy skips this attack window.
        ActionSystem.Instance.AddReaction(new ResetEnemyBreakMetersGA());
        yield return null;
    }

    public HashSet<EnemyView> AdvanceAttackCountdownsForCard()
    {
        HashSet<EnemyView> attacksQueued = new();
        if (enemyBoardView == null || enemyBoardView.EnemyViews == null || ActionSystem.Instance == null)
            return attacksQueued;
        foreach (EnemyView enemy in enemyBoardView.EnemyViews)
        {
            if (enemy == null || enemy.CurrentHealth <= 0 || !enemy.OpeningActionResolved) continue;
            if (!enemy.AdvanceAttackCountdown()) continue;
            if (enemy.IsBroken) continue;
            ActionSystem.Instance.AddReaction(new AttackHeroGA(enemy));
            attacksQueued.Add(enemy);
        }
        return attacksQueued;
    }

    private IEnumerator ResetEnemyBreakMetersPerformer(ResetEnemyBreakMetersGA action)
    {
        if (enemyBoardView == null || enemyBoardView.EnemyViews == null)
            yield break;

        foreach (EnemyView enemy in enemyBoardView.EnemyViews)
            if (enemy != null && enemy.CurrentHealth > 0)
                enemy.RestoreBreakMeter();
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

        // A wrong answer must retaliate even before the normal opening buff turn.
        if (!attackHeroGA.CanAttack)
            yield break;

        CombatantView hero = HeroSystem.Instance != null ? HeroSystem.Instance.HeroView : null;
        if (hero == null || hero.CurrentHealth <= 0) yield break;
        Vector3 startPosition = attacker.transform.position;
        bool melee = !attacker.IsRanged;
        Tween movement = null;
        attacker.FaceTowards(hero);
        hero.FaceTowards(attacker);
        BattleMessageUI.Instance?.ShowAt("ENEMY ATTACK", $"{(attacker.Data != null ? attacker.Data.name : "Enemy")} attacks.", attacker.VisualCenter);
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

            if (attacker == null || attacker.CurrentHealth <= 0 || attacker.IsBroken || hero == null || hero.CurrentHealth <= 0) yield break;
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

        if (attacker == null || attacker.CurrentHealth <= 0 || attacker.IsBroken || hero == null || hero.CurrentHealth <= 0) yield break;
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

        // Count each resolved enemy attack once. Damage-over-time effects do not
        // pass through this path, and two attacks charge one EP star.
        BattleHudMedieval.GainEpFromEnemyHit();

        // The sword icon counts empowered attacks. Consume one after each attack;
        // when it reaches zero, remove the temporary attack-power bonus.
        attacker.ConsumeDamageBuffAttack();

        // Frost suppresses the next attack; its stacks are consumed after damage lands.
        if (frostStacks > 0)
            attacker.RemoveStatusEffect(StatusEffectType.FROST, frostStacks);

    }
    private IEnumerator KillEnemyPerformer(KillEnemyGA killEnemyGA)
    {
        yield return enemyBoardView.RemoveEnemy(killEnemyGA.EnemyView);
    }
}
