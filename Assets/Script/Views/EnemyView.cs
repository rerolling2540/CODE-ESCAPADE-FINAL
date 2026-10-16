using TMPro;
using UnityEngine;
using System.Collections;

public class EnemyView : CombatantView
{
   [SerializeField] private TMP_Text attackText;
   private EnemyData data;
   public bool IsRanged => data != null && data.attackStyle == EnemyAttackStyle.Ranged;
   public bool HasProjectile => data != null && data.projectileFrames != null && data.projectileFrames.Length > 0;
   public void SetUp(EnemyData enemyData)
    {
        data = enemyData;
        AttackPower = enemyData.AttackPower;
        if (attackText != null) attackText.gameObject.SetActive(false);
        SetUpBase(enemyData.Health, enemyData.Image);
        SetBattleMaterial(enemyData.battleMaterial);
        EnlargeEnemyVisual();
        EnsureEnemyHealthBar();
        ConfigureBattleAnimation(enemyData.idleFrames, enemyData.attackFrames, enemyData.battleFramesPerSecond, true, enemyData.hovering);
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
