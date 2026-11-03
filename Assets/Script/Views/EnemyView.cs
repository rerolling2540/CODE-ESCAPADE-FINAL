using TMPro;
using UnityEngine;
using System.Collections;

public class EnemyView : CombatantView
{
   [SerializeField] private TMP_Text attackText;
   private EnemyData data;
   private int attackCountdown;
   private int maxAttackCountdown = 15;
   private int breakMeter;
   private int maxBreakMeter = 100;
   private bool breakRestorePending;
   private bool openingActionResolved;
   private int openingDamageBuffBonus;
   public bool OpeningActionResolved => openingActionResolved;
   public EnemyData Data => data;
   public int AttackCountdown => attackCountdown;
   public int BreakMeter => breakMeter;
   public int MaxBreakMeter => maxBreakMeter;
   public bool IsBroken => breakMeter <= 0;
   public int ExperienceReward => data!=null?Mathf.Max(0,data.experienceReward):0;
   public bool IsRanged => data != null && data.attackStyle == EnemyAttackStyle.Ranged;
   public bool HasProjectile => data != null && data.projectileFrames != null && data.projectileFrames.Length > 0;
   public void SetUp(EnemyData enemyData)
    {
        data = enemyData;
        AttackPower = enemyData.AttackPower;
        maxAttackCountdown = Mathf.Max(1, enemyData.attackCountdown);
        attackCountdown = maxAttackCountdown;
        maxBreakMeter = Mathf.Max(1, enemyData.breakMeterMax);
        breakMeter = maxBreakMeter;
        breakRestorePending = false;
        openingActionResolved = false;
        openingDamageBuffBonus = 0;
        if (attackText != null) attackText.gameObject.SetActive(false);
        SetUpBase(enemyData.Health, enemyData.Image);
        SetBattleMaterial(enemyData.battleMaterial);
        EnlargeEnemyVisual(2.8f * Mathf.Max(0.1f, enemyData.battleVisualScale));
        EnsureEnemyHealthBar();
        ConfigureBattleAnimation(enemyData.idleFrames, enemyData.attackFrames, enemyData.battleFramesPerSecond, true, enemyData.hovering);
    }

    public void ResolveOpeningAction()
    {
        openingActionResolved = true;
        EnemyOpeningAction action = data != null ? data.openingAction : EnemyOpeningAction.Random;
        if (action == EnemyOpeningAction.Random)
            action = Random.value < 0.5f ? EnemyOpeningAction.Shield : EnemyOpeningAction.DamageBuff;
        if (action == EnemyOpeningAction.Shield)
        {
            int shield = Mathf.Max(1, data != null ? data.openingShield : 8);
            AddStatusEffect(StatusEffectType.ARMOR, shield);
            StatusEffectSystem.PlayShieldAnimation(this);
            BattleMessageUI.Instance?.ShowAt("ENEMY SHIELD", $"{GetDisplayName()} gains {shield} shield.", VisualCenter);
        }
        else
        {
            int damageBonus = Mathf.Max(1, data != null ? data.openingDamageBonus : 3);
            openingDamageBuffBonus = damageBonus;
            AttackPower += damageBonus;
            int empoweredAttacks = Mathf.Max(1, data != null ? data.openingDamageBuffAttacks : 3);
            AddStatusEffect(StatusEffectType.DAMAGE_BUFF, empoweredAttacks);
            StatusEffectSystem.PlayDamageBuffAnimation(this);
            BattleMessageUI.Instance?.ShowAt("DAMAGE UP", $"{GetDisplayName()} powers up for {empoweredAttacks} attacks.", VisualCenter);
        }
    }

    private string GetDisplayName()
    {
        return data != null && !string.IsNullOrWhiteSpace(data.name) ? data.name : "Enemy";
    }

    public void ConsumeDamageBuffAttack()
    {
        int attacksRemaining = GetStatusEffectStacks(StatusEffectType.DAMAGE_BUFF);
        if (attacksRemaining <= 0) return;

        RemoveStatusEffect(StatusEffectType.DAMAGE_BUFF, 1);
        if (GetStatusEffectStacks(StatusEffectType.DAMAGE_BUFF) == 0 && openingDamageBuffBonus > 0)
        {
            AttackPower = Mathf.Max(0, AttackPower - openingDamageBuffBonus);
            openingDamageBuffBonus = 0;
        }
    }

    /// <summary>Advances this enemy's intent by one player action. Returns true when an attack becomes due.</summary>
    public bool AdvanceAttackCountdown()
    {
        if (CurrentHealth <= 0) return false;
        if (attackCountdown > 0) attackCountdown--;
        bool attackDue = attackCountdown <= 0;
        if (attackDue) attackCountdown = maxAttackCountdown;
        UpdateEnemyIntentUI(attackCountdown, breakMeter, maxBreakMeter);
        return attackDue;
    }

    public void ApplyBreakDamage(int amount)
    {
        if (amount <= 0 || IsBroken || CurrentHealth <= 0) return;
        breakMeter = Mathf.Max(0, breakMeter - amount);
        if (breakMeter == 0) breakRestorePending = true;
        UpdateEnemyIntentUI(attackCountdown, breakMeter, maxBreakMeter);
    }

    public void RestoreBreakMeter()
    {
        // A positive meter persists between turns. Only a meter that actually
        // reached zero is restored, after this enemy has missed its turn.
        if (!breakRestorePending) return;
        breakMeter = maxBreakMeter;
        breakRestorePending = false;
        UpdateEnemyIntentUI(attackCountdown, breakMeter, maxBreakMeter);
    }
    public IEnumerator PlayProjectile(CombatantView target)
    {
        if (!HasProjectile || target == null || target.CurrentHealth <= 0) yield break;
        var projectile = new GameObject("Enemy Attack Projectile", typeof(SpriteRenderer));
        projectile.transform.SetParent(transform, true);
        var renderer = projectile.GetComponent<SpriteRenderer>();
        renderer.sortingOrder = 25;
        Vector3 start = VisualCenter;
        Vector3 end = target.VisualCenter;
        bool faceLeft = end.x < start.x;
        float duration = Mathf.Clamp(Vector3.Distance(start, end) / Mathf.Max(1f, data.projectileSpeed), 0.25f, 1f);
        float elapsed = 0;
        projectile.transform.position = start;
        try
        {
            while (elapsed < duration && target != null)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                projectile.transform.position = Vector3.Lerp(start, target.VisualCenter, t) + Vector3.up * (4f * data.projectileArc * t * (1f - t));
                renderer.sprite = data.projectileFrames[Mathf.FloorToInt(elapsed * data.projectileFrameRate) % data.projectileFrames.Length];
                renderer.flipX = faceLeft;
                float height = Mathf.Max(0.01f, renderer.sprite.bounds.size.y);
                projectile.transform.localScale = Vector3.one * (0.5f / height);
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (target != null) projectile.transform.position = target.VisualCenter;
            if (target != null && data.impactFrames != null)
            {
                foreach (var frame in data.impactFrames)
                {
                    if (target == null || frame == null) break;
                    projectile.transform.position = target.VisualCenter;
                    renderer.sprite = frame;
                    projectile.transform.localScale = Vector3.one * (1f / Mathf.Max(0.01f, frame.bounds.size.y));
                    yield return new WaitForSeconds(1f / Mathf.Max(1f, data.projectileFrameRate));
                }
            }
        }
        finally { if (projectile != null) Destroy(projectile); }
    }
}
