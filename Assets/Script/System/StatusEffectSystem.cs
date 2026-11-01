using System.Collections;
using UnityEngine;

public class StatusEffectSystem : MonoBehaviour
{
    [Header("Battle status and combat animations")]
    [SerializeField] private Texture2D[] armorVfxTextures;
    [SerializeField] private Texture2D[] burnVfxTextures;
    [SerializeField] private Texture2D[] healVfxTextures;
    [SerializeField] private Texture2D[] impactVfxTextures;
    [SerializeField, Min(1f)] private float vfxFramesPerSecond = 24f;

    private Sprite[] armorVfxFrames;
    private Sprite[] burnVfxFrames;
    private Sprite[] healVfxFrames;
    private Sprite[] impactVfxFrames;

    public static StatusEffectSystem Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        armorVfxFrames = CreateFrames(armorVfxTextures);
        burnVfxFrames = CreateFrames(burnVfxTextures);
        healVfxFrames = CreateFrames(healVfxTextures);
        impactVfxFrames = CreateFrames(impactVfxTextures);
    }

    private void OnEnable()
    {
        ActionSystem.AttachPerformer<AddStatusEffectGA>(AddStatusEffectPerformer);
        ActionSystem.AttachPerformer<TickStatusEffectGA>(TickStatusEffectPerformer);
        ActionSystem.AttachPerformer<NatureTurnGA>(NatureTurnPerformer);
    }
    private void OnDisable()
    {
        ActionSystem.DetachPerformer<AddStatusEffectGA>();
        ActionSystem.DetachPerformer<TickStatusEffectGA>();
        ActionSystem.DetachPerformer<NatureTurnGA>();
    }
   private IEnumerator AddStatusEffectPerformer(AddStatusEffectGA addStatusEffectGA)
    {
        if (addStatusEffectGA.Targets == null) yield break;
        foreach (var target in addStatusEffectGA.Targets)
        {
            if (target == null) continue;
            if (addStatusEffectGA.StatusEffectType == StatusEffectType.NATURE)
                target.ScheduleNatureHeal(addStatusEffectGA.StackCount, addStatusEffectGA.NatureHealAmount);
            else
                target.AddStatusEffect(addStatusEffectGA.StatusEffectType, addStatusEffectGA.StackCount);
            PlayStatusAnimation(target, addStatusEffectGA.StatusEffectType);
            yield return null;
        }
    }

    private IEnumerator TickStatusEffectPerformer(TickStatusEffectGA tick)
    {
        CombatantView target = tick != null ? tick.Target : null;
        if (target == null) yield break;

        int stacks = target.GetStatusEffectStacks(tick.Type);
        if (stacks <= 0) yield break;

        if (target.CurrentHealth > 0 && tick.Damage > 0)
        {
            int before = target.CurrentHealth;
            target.Damage(tick.Damage);
            if (target.CurrentHealth < before)
            {
                BattleBloodEffect.Play(target);
                PlayImpactAnimation(target);
            }
        }
        target.RemoveStatusEffect(tick.Type, Mathf.Min(stacks, tick.StacksToRemove));
        yield return null;
    }

    private IEnumerator NatureTurnPerformer(NatureTurnGA turn)
    {
        if (turn?.Target != null) turn.Target.TickNatureHeal();
        yield return null;
    }

    public static void PlayHealAnimation(CombatantView target)
    {
        if (Instance != null) Instance.PlayAnimation(target, Instance.healVfxFrames, 1.35f);
    }

    public static void PlayImpactAnimation(CombatantView target)
    {
        if (Instance != null) Instance.PlayAnimation(target, Instance.impactVfxFrames, 1.1f);
    }

    private void PlayStatusAnimation(CombatantView target, StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.ARMOR:
                PlayAnimation(target, armorVfxFrames, 1.5f);
                break;
            case StatusEffectType.BURN:
                PlayAnimation(target, burnVfxFrames, 1.0f, true);
                break;
        }
    }

    private void PlayAnimation(CombatantView target, Sprite[] frames, float sizeMultiplier, bool loop = false)
    {
        if (target == null || frames == null || frames.Length == 0 || target.MainSpriteRenderer == null) return;

        Transform anchor = target.MainSpriteRenderer.transform;
        GameObject effectObject = new GameObject("Combat Effect");
        effectObject.transform.SetParent(anchor, false);
        effectObject.transform.localPosition = anchor.InverseTransformPoint(target.VisualCenter);
        effectObject.transform.localRotation = Quaternion.identity;

        SpriteRenderer effectRenderer = effectObject.AddComponent<SpriteRenderer>();
        SpriteRenderer targetRenderer = target.MainSpriteRenderer;
        effectRenderer.sortingLayerID = targetRenderer.sortingLayerID;
        effectRenderer.sortingOrder = targetRenderer.sortingOrder + 5;

        float framesPerSecond = Mathf.Max(1f, vfxFramesPerSecond);
        Sprite firstFrame = null;
        for (int i = 0; i < frames.Length && firstFrame == null; i++) firstFrame = frames[i];
        if (firstFrame == null)
        {
            Destroy(effectObject);
            return;
        }

        effectRenderer.sprite = firstFrame;
        SetEffectScale(effectObject.transform, firstFrame, target, sizeMultiplier);
        int frameCount = frames.Length * (loop ? 4 : 1);
        StartCoroutine(AnimateSpriteEffect(effectObject, effectRenderer, target, frames, framesPerSecond, frameCount));
    }

    private IEnumerator AnimateSpriteEffect(GameObject effectObject, SpriteRenderer effectRenderer, CombatantView target,
        Sprite[] frames, float framesPerSecond, int frameCount)
    {
        float elapsed = 0f;
        int lastFrameIndex = -1;
        while (elapsed < frameCount / framesPerSecond && effectObject != null && target != null)
        {
            int frameIndex = Mathf.Min(Mathf.FloorToInt(elapsed * framesPerSecond), frameCount - 1) % frames.Length;
            if (frameIndex != lastFrameIndex)
            {
                if (frames[frameIndex] != null) effectRenderer.sprite = frames[frameIndex];
                lastFrameIndex = frameIndex;
            }

            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }

        if (effectObject != null) Destroy(effectObject);
    }

    private static void SetEffectScale(Transform effectTransform, Sprite frame, CombatantView target, float sizeMultiplier)
    {
        float parentScaleY = Mathf.Abs(effectTransform.parent.lossyScale.y);
        float frameHeight = Mathf.Max(0.01f, frame.bounds.size.y * parentScaleY);
        float desiredHeight = Mathf.Max(0.01f, target.VisualBounds.size.y * sizeMultiplier);
        float uniformScale = desiredHeight / frameHeight;
        effectTransform.localScale = new Vector3(uniformScale, uniformScale, 1f);
    }

    private static Sprite[] CreateFrames(Texture2D[] textures)
    {
        if (textures == null || textures.Length == 0) return System.Array.Empty<Sprite>();

        Sprite[] frames = new Sprite[textures.Length];
        for (int i = 0; i < textures.Length; i++)
        {
            Texture2D texture = textures[i];
            if (texture == null) continue;

            frames[i] = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            frames[i].name = texture.name + "_FullFrame";
        }
        return frames;
    }

    private void OnDestroy()
    {
        DestroyFrames(armorVfxFrames);
        DestroyFrames(burnVfxFrames);
        DestroyFrames(healVfxFrames);
        DestroyFrames(impactVfxFrames);
        if (Instance == this) Instance = null;
    }

    private static void DestroyFrames(Sprite[] frames)
    {
        if (frames == null) return;
        foreach (Sprite frame in frames)
            if (frame != null) Destroy(frame);
    }
}
