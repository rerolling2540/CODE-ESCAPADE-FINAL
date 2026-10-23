using UnityEngine;
using System.Collections;

public class HeroView : CombatantView
{
    private HeroData battleData;
    private SpriteRenderer attackVfx;
    private bool travellingAttack;
    private Vector3 attackOrigin;

    public IEnumerator PlayBattleAttack(CombatantView target)
    {
        if (target != null) FaceTowards(target);
        if (battleData == null || !battleData.dashToBattleTarget || target == null || target.CurrentHealth <= 0)
        {
            yield return PlayAttackAnimation();
            yield break;
        }
        attackOrigin = transform.position;
        travellingAttack = true;
        try
        {
            float side = VisualCenter.x <= target.VisualCenter.x ? -1f : 1f;
            float spacing = target.VisualBounds.extents.x * 0.6f + VisualBounds.extents.x * 0.55f + 0.2f;
            Vector3 destination = attackOrigin;
            destination.x += target.VisualCenter.x + side * spacing - VisualCenter.x;
            destination.y += target.VisualBounds.min.y - VisualBounds.min.y;
            float duration = Mathf.Clamp(Vector3.Distance(attackOrigin, destination) / 28f, 0.14f, 0.3f);
            yield return PlayBattleTravel(destination, duration, battleData.battleDashFrames);
            if (CurrentHealth <= 0 || target == null || target.CurrentHealth <= 0) yield break;
            yield return PlayAttackAnimation();
            if (CurrentHealth <= 0) yield break;
            yield return PlayBattleTravel(attackOrigin, 0.24f, battleData.battleDashFrames);
        }
        finally
        {
            if (this != null) transform.position = attackOrigin;
            travellingAttack = false;
            ClearAttackVfx();
        }
    }
    public void SetUp(HeroData heroData)
    {
        ClearAttackVfx();
        battleData = heroData;
        // If PlayerStats already calculated MaxHealth from level scaling, keep it!
        // Otherwise, fall back to heroData.Health as a default.
        int targetHealth = (MaxHealth > 0) ? MaxHealth : heroData.Health;

        SetUpBase(targetHealth, heroData.Image);
        ConfigureBattleAnimation(heroData.idleFrames, heroData.attackFrames, heroData.battleFramesPerSecond, false,
            hover: heroData.hoverBattleAnimation, frameDurations: heroData.attackFrameDurations);
    }

    protected override void OnAttackFrame(int index, int count)
    {
        if (battleData == null || battleData.attackVfxFrames == null || battleData.attackVfxFrames.Length == 0) return;
        int start = Mathf.Clamp(battleData.attackVfxStartFrame, 0, count - 1);
        if (index < start) return;
        if (attackVfx == null)
        {
            var effect = new GameObject("Warrior Battle Slash VFX", typeof(SpriteRenderer));
            effect.transform.SetParent(transform, false);
            attackVfx = effect.GetComponent<SpriteRenderer>();
            var heroSprite = GetComponentInChildren<SpriteRenderer>();
            if (heroSprite != null && heroSprite != attackVfx)
            {
                attackVfx.sortingLayerID = heroSprite.sortingLayerID;
                attackVfx.sortingOrder = heroSprite.sortingOrder + 2;
            }
        }
        int frame = Mathf.Min(battleData.attackVfxFrames.Length - 1,
            Mathf.FloorToInt((float)(index - start) / Mathf.Max(1, count - start - 1) * (battleData.attackVfxFrames.Length - 1)));
        var sprite = battleData.attackVfxFrames[frame];
        if (sprite == null) return;
        attackVfx.sprite = sprite;
        var body = GetComponentInChildren<SpriteRenderer>();
        float direction = body != null ? Mathf.Sign(body.transform.TransformVector(Vector3.right).x) * (body.flipX ? -1f : 1f) : 1f;
        attackVfx.flipX = direction < 0f;
        // Use world space so mirrored/scaled hero prefab parents do not mirror twice.
        attackVfx.transform.rotation = Quaternion.identity;
        float scale = battleData.attackVfxHeight / Mathf.Max(0.01f, sprite.bounds.size.y);
        Vector3 parentScale = transform.lossyScale;
        attackVfx.transform.localScale = new Vector3(scale / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)),
            scale / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)), 1f);
        if (parentScale.x < 0f) attackVfx.transform.localScale = new Vector3(-attackVfx.transform.localScale.x, attackVfx.transform.localScale.y, 1f);
        attackVfx.transform.position = VisualCenter + Vector3.right * direction * 0.85f;
    }

    protected override void OnAttackFinished() { ClearAttackVfx(); }
    void OnDisable()
    {
        if (travellingAttack) transform.position = attackOrigin;
        travellingAttack = false;
        ClearAttackVfx();
    }
    void ClearAttackVfx()
    {
        if (attackVfx != null) { attackVfx.gameObject.SetActive(false); Destroy(attackVfx.gameObject); }
        attackVfx = null;
    }
}
