using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HeroView : CombatantView
{
    private HeroData battleData;
    private bool travellingAttack;
    private Vector3 attackOrigin;

    public IEnumerator PlayBattleAttack(CombatantView target, CardData card = null, IList<CombatantView> affectedTargets = null)
    {
        if (target != null) FaceTowards(target);
        if (battleData == null || !battleData.dashToBattleTarget || target == null || target.CurrentHealth <= 0)
        {
            PlayAttackFeedback(target, card, affectedTargets);
            yield return PlayAttackAnimation();
            if (CurrentHealth > 0 && battleData != null)
                yield return PlayBattleFramesOnce(battleData.sheatheFrames, battleData.battleFramesPerSecond);
            yield break;
        }
        attackOrigin = transform.position;
        travellingAttack = true;
        try
        {
            float side = VisualCenter.x <= target.VisualCenter.x ? -1f : 1f;
            float spacing = target.BattleBodyBounds.extents.x * 0.6f + BattleBodyBounds.extents.x * 0.55f + 0.2f;
            Vector3 destination = attackOrigin;
            destination.x += target.VisualCenter.x + side * spacing - VisualCenter.x;
            destination.y += target.BattleGroundY - BattleGroundY;
            float duration = Mathf.Clamp(Vector3.Distance(attackOrigin, destination) / 28f, 0.14f, 0.3f);
            yield return PlayBattleTravel(destination, duration, battleData.battleDashFrames);
            if (CurrentHealth <= 0 || target == null || target.CurrentHealth <= 0) yield break;
            PlayAttackFeedback(target, card, affectedTargets);
            yield return PlayAttackAnimation();
            if (CurrentHealth <= 0) yield break;
            yield return PlayBattleTravel(attackOrigin, 0.24f, battleData.battleDashFrames);
            if (CurrentHealth > 0)
                yield return PlayBattleFramesOnce(battleData.sheatheFrames, battleData.battleFramesPerSecond);
        }
        finally
        {
            if (this != null) transform.position = attackOrigin;
            travellingAttack = false;
        }
    }

    private void PlayAttackFeedback(CombatantView target, CardData card, IList<CombatantView> affectedTargets)
    {
        if (card != null && !BattleAudio.IsAttackCard(card)) return;

        // Trigger the existing class-specific sound and the visual on the same
        // frame, after any dash and immediately before the attack animation.
        BattleAudio.HeroAttack(card);
        // Sprite-based card effects render with Unity's normal SpriteRenderer,
        // avoiding the incompatible imported particle shader that showed pink.
        // A null card is a scripted attack; it keeps the attack sound/animation
        // without receiving a generic particle burst.
        if (card == null) return;
        Vector3 origin = VisualCenter + Vector3.up * 0.2f + Vector3.right * GetFacingDirection() * 0.35f;
        CardAttackVfx.Play(this, card, target, affectedTargets, origin, MainSpriteRenderer);
    }

    private float GetFacingDirection()
    {
        if (MainSpriteRenderer == null) return transform.right.x >= 0f ? 1f : -1f;
        float spriteAxis = MainSpriteRenderer.transform.TransformVector(Vector3.right).x;
        return Mathf.Sign(spriteAxis) * (MainSpriteRenderer.flipX ? -1f : 1f);
    }

    public void SetUp(HeroData heroData)
    {
        battleData = heroData;
        // If PlayerStats already calculated MaxHealth from level scaling, keep it!
        // Otherwise, fall back to heroData.Health as a default.
        int targetHealth = (MaxHealth > 0) ? MaxHealth : heroData.Health;

        SetUpBase(targetHealth, heroData.Image);
        ConfigureBattleAnimation(heroData.idleFrames, heroData.attackFrames, heroData.battleFramesPerSecond, false,
            hover: heroData.hoverBattleAnimation, frameDurations: heroData.attackFrameDurations);
    }

    void OnDisable()
    {
        if (travellingAttack) transform.position = attackOrigin;
        travellingAttack = false;
    }
}
