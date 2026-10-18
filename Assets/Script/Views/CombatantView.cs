using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatantView : MonoBehaviour
{
    [Header("UI & Visual References")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private StatusEffectsUI statusEffectsUI;

    [Header("Dynamic Bar Scaling (Elden Ring Style)")]
    [SerializeField] private RectTransform healthBarRect; // Drag your HealthSlider RectTransform here
    [SerializeField] private float baseWidth = 200f;       // Bar width at base health (100 HP)
    [SerializeField] private int baseHealth = 100;         // Starting baseline health
    [SerializeField] private float maxWidth = 600f;        // Cap width to keep UI within screen bounds

    [Header("Health System Properties")]
    public int MaxHealth { get; private set; } = 100;
    public int CurrentHealth { get; private set; } = 100;
    public int AttackDamageBonus { get; private set; }
    public int AttackPower { get; protected set; } = 30;
    public int Defense { get; private set; }
    public Vector3 VisualCenter => spriteRenderer != null ? spriteRenderer.bounds.center : transform.position;
    public Bounds VisualBounds => spriteRenderer != null ? spriteRenderer.bounds : new Bounds(transform.position, Vector3.one);

    private Dictionary<StatusEffectType, int> statusEffects = new();
    private bool deathQueued;
    private Sprite[] idleFrames, attackFrames;
    private float battleFrameRate = 12f;
    private float[] attackFrameDurations;
    private bool attacking, battleAnimationReady, hovering;
    private float idleClock;
    private Vector3 visualScale, visualPosition;
    private float footY;
    private Sprite restingSprite;
    private CombatantView facingTarget;
    private float hitTime;
    private float hitDirection;
    private Color restingColor = Color.white;
    private bool healthTextBoundToSlider;
    private Vector3 FacingAnchor => battleAnimationReady && spriteRenderer != null
        ? spriteRenderer.transform.parent.TransformPoint(visualPosition) : transform.position;

    public void FaceTowards(CombatantView target)
    {
        facingTarget = target;
        UpdateFacing();
    }

    void LateUpdate() { UpdateFacing(); }

    void UpdateFacing()
    {
        if (spriteRenderer == null || facingTarget == null) return;
        float delta = facingTarget.FacingAnchor.x - FacingAnchor.x;
        if (Mathf.Abs(delta) > 0.35f)
        {
            // The enemy prefab rotates its visual parent 180 degrees around Y.
            // Compare against the transformed sprite axis to avoid flipping twice.
            float spriteAxis = spriteRenderer.transform.TransformVector(Vector3.right).x;
            spriteRenderer.flipX = delta * spriteAxis < 0f;
        }
    }

    protected void ConfigureBattleAnimation(Sprite[] idle, Sprite[] attack, float fps, bool faceLeft, bool hover = false,
        float[] frameDurations = null)
    {
        if (spriteRenderer == null) return;
        idleFrames = idle;
        attackFrames = attack;
        attackFrameDurations = frameDurations;
        battleFrameRate = Mathf.Max(1f, fps);
        hovering = hover;
        spriteRenderer.flipX = faceLeft;
        visualScale = spriteRenderer.transform.localScale;
        visualPosition = spriteRenderer.transform.localPosition;
        restingColor = spriteRenderer.color;
        restingSprite = idle != null && idle.Length > 0 && idle[0] != null ? idle[0] : spriteRenderer.sprite;
        footY = visualPosition.y + (spriteRenderer.sprite != null ? spriteRenderer.sprite.bounds.min.y * visualScale.y : 0);
        battleAnimationReady = true;
        SetBattleFrame(restingSprite, 1f, 0f);
    }

    void Update()
    {
        if (spriteRenderer != null && battleAnimationReady)
            spriteRenderer.color = Time.time < hitTime ? Color.Lerp(restingColor, Color.white, 0.7f) : restingColor;
        if (!battleAnimationReady || attacking || CurrentHealth <= 0) return;
        idleClock += Time.deltaTime;
        Sprite frame = restingSprite;
        if (idleFrames != null && idleFrames.Length > 0)
            frame = idleFrames[Mathf.FloorToInt(idleClock * 8f) % idleFrames.Length];
        float breath = 1f + Mathf.Sin(idleClock * 2.5f) * 0.012f;
        SetBattleFrame(frame, breath, hovering ? Mathf.Sin(idleClock * 2f) * 0.12f : 0f);
    }

    void SetBattleFrame(Sprite frame, float breath, float lift)
    {
        if (spriteRenderer == null || frame == null) return;
        spriteRenderer.sprite = frame;
        spriteRenderer.transform.localScale = new Vector3(visualScale.x, visualScale.y * breath, visualScale.z);
        // Keep the feet planted despite tightly-cropped attack frames.
        float recoil = Mathf.Max(0f, (hitTime - Time.time) / 0.18f);
        float axis = spriteRenderer.transform.parent.TransformVector(Vector3.right).x;
        spriteRenderer.transform.localPosition = new Vector3(visualPosition.x + hitDirection * Mathf.Sign(axis) * Mathf.Sin(recoil * Mathf.PI) * 0.12f,
            footY - frame.bounds.min.y * visualScale.y * breath + lift, visualPosition.z);
    }

    public IEnumerator PlayAttackAnimation()
    {
        if (!battleAnimationReady || CurrentHealth <= 0 || attacking) yield break;
        attacking = true;
        try
        {
            if (attackFrames != null && attackFrames.Length > 0)
            {
                for (int i = 0; i < attackFrames.Length; i++)
                {
                    if (CurrentHealth <= 0) yield break;
                    // Keep hovering casters at their current height during the cast.
                    SetBattleFrame(attackFrames[i], 1f, hovering ? Mathf.Sin(idleClock * 2f) * 0.12f : 0f);
                    // Preserve the default swing timing unless the hero supplies
                    // weapon-specific draw, release and recovery durations.
                    float timing = i == 0 ? 1.4f : i == attackFrames.Length - 1 ? 1.5f : 0.8f;
                    float duration = attackFrameDurations != null && i < attackFrameDurations.Length &&
                        attackFrameDurations[i] > 0f ? attackFrameDurations[i] : timing / battleFrameRate;
                    yield return new WaitForSeconds(duration);
                }
            }
            else
            {
                // Existing combatants without a sheet still have an attack pose.
                spriteRenderer.transform.localScale = new Vector3(visualScale.x * 1.08f, visualScale.y * 0.96f, visualScale.z);
                yield return new WaitForSeconds(0.25f);
            }
        }
        finally
        {
            attacking = false;
            if (!hovering && attackFrameDurations != null && attackFrameDurations.Length > 0) idleClock = 0f;
            if (spriteRenderer != null)
                SetBattleFrame(restingSprite, 1f, hovering ? Mathf.Sin(idleClock * 2f) * 0.12f : 0f);
        }
    }

    private void Start()
    {
        UpdateHealthUI();
    }

    // Call this to initialize hero stats from HeroClassData without hardcoding HP
    public void InitializeClass(HeroClassData classData, int level)
    {
        if (classData == null) return;

        MaxHealth = classData.GetHealthForLevel(level);
        CurrentHealth = MaxHealth;
        AttackDamageBonus = classData.GetAttackBonusForLevel(level);
        AttackPower = classData.GetAttackPowerForLevel(level);
        Defense = classData.GetDefenseForLevel(level);

        UpdateHealthUI();
    }

    protected void SetUpBase(int health, Sprite image)
    {
        MaxHealth = CurrentHealth = health;
        deathQueued = false;
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = image;
        }
        UpdateHealthUI();
    }

    protected void EnsureEnemyHealthBar()
    {
        if (healthSlider != null) return;
        if (healthText != null) healthText.gameObject.SetActive(false);
        var root = new GameObject("Enemy Health Bar", typeof(RectTransform), typeof(Canvas));
        root.transform.SetParent(transform, false);
        float top = spriteRenderer != null ? spriteRenderer.bounds.max.y - transform.position.y : 2f;
        root.transform.localPosition = new Vector3(0f, top + 0.6f, -0.1f);
        root.transform.localScale = Vector3.one * 0.013f;
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 50;
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 65);

        var bar = new GameObject("HP Slider", typeof(RectTransform), typeof(Image), typeof(Slider));
        bar.transform.SetParent(root.transform, false);
        var barRect = bar.GetComponent<RectTransform>();
        barRect.sizeDelta = new Vector2(300, 30);
        barRect.anchoredPosition = new Vector2(0, -12);
        bar.GetComponent<Image>().color = new Color(0.65f, 0.45f, 0.18f);
        bar.GetComponent<Image>().raycastTarget = false;
        var background = new GameObject("Dark Backing", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(bar.transform, false);
        var backingRect = background.GetComponent<RectTransform>();
        backingRect.anchorMin = Vector2.zero;
        backingRect.anchorMax = Vector2.one;
        backingRect.offsetMin = new Vector2(3, 3);
        backingRect.offsetMax = new Vector2(-3, -3);
        background.GetComponent<Image>().color = new Color(0.16f, 0.055f, 0.035f);
        background.GetComponent<Image>().raycastTarget = false;
        var fill = new GameObject("Blood Red Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(background.transform, false);
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.73f, 0.12f, 0.09f);
        fill.GetComponent<Image>().raycastTarget = false;
        healthSlider = bar.GetComponent<Slider>();
        healthSlider.interactable = false;
        healthSlider.transition = Selectable.Transition.None;
        healthSlider.navigation = new Navigation { mode = Navigation.Mode.None };
        healthSlider.fillRect = fillRect;
        healthSlider.direction = Slider.Direction.LeftToRight;
        healthSlider.minValue = 0;
        healthSlider.wholeNumbers = true;

        var label = new GameObject("HP Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(root.transform, false);
        label.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 32);
        label.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 18);
        healthText = label.GetComponent<TextMeshProUGUI>();
        healthText.fontSize = 29;
        healthText.alignment = TextAlignmentOptions.Center;
        healthText.color = new Color(1f, 0.9f, 0.67f);
        healthText.raycastTarget = false;
        healthBarRect = null; // All enemy bars retain a readable, fixed width.
        UpdateHealthUI();
    }

    protected void EnlargeEnemyVisual()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;
        float height = spriteRenderer.bounds.size.y;
        if (height > 0.01f) spriteRenderer.transform.localScale *= 2.8f / height;
        // Keep the target collider aligned with the enlarged figure.
        Bounds bounds = spriteRenderer.bounds;
        Vector3 min = transform.InverseTransformPoint(bounds.min);
        Vector3 max = transform.InverseTransformPoint(bounds.max);
        var box2D = GetComponent<BoxCollider2D>();
        if (box2D != null)
        {
            box2D.offset = (min + max) * 0.5f;
            box2D.size = max - min;
        }
        var box3D = GetComponent<BoxCollider>();
        if (box3D != null)
        {
            box3D.center = (min + max) * 0.5f;
            box3D.size = new Vector3(max.x - min.x, max.y - min.y, 0.2f);
        }
    }

    protected void SetBattleMaterial(Material material)
    {
        if (spriteRenderer != null && material != null) spriteRenderer.sharedMaterial = material;
    }

    public void UpdateHealthUI()
    {
        BindHealthTextToSlider();
        // 1. Dynamic Health Bar Width Scaling
        if (healthBarRect != null && baseHealth > 0)
        {
            float healthRatio = (float)MaxHealth / baseHealth;
            float newWidth = Mathf.Min(baseWidth * healthRatio, maxWidth);

            // Dynamically updates RectTransform width
            healthBarRect.sizeDelta = new Vector2(newWidth, healthBarRect.sizeDelta.y);
        }

        // 2. Standard Slider & Text Refresh
        if (healthText != null)
        {
            healthText.text = "HP: " + CurrentHealth + " / " + MaxHealth;
        }

        if (healthSlider != null)
        {
            healthSlider.maxValue = MaxHealth;
            healthSlider.value = CurrentHealth;
        }
    }

    private void BindHealthTextToSlider()
    {
        if (healthTextBoundToSlider || healthText == null || healthSlider == null) return;
        RectTransform label = healthText.rectTransform;
        label.SetParent(healthSlider.transform, false);
        label.anchorMin = Vector2.zero;
        label.anchorMax = Vector2.one;
        label.pivot = new Vector2(0.5f, 0.5f);
        label.anchoredPosition = Vector2.zero;
        label.sizeDelta = Vector2.zero;
        label.SetAsLastSibling();
        healthText.alignment = TextAlignmentOptions.Center;
        healthText.fontSize = 18f;
        healthText.color = Color.white;
        healthText.raycastTarget = false;
        healthTextBoundToSlider = true;
    }

    public void Damage(int damageAmount)
    {
        int healthBefore = CurrentHealth;
        int remainingDamage = Mathf.Max(0, damageAmount - Defense);
        int currentArmor = GetStatusEffectStacks(StatusEffectType.ARMOR);

        if (currentArmor > 0)
        {
            if (currentArmor >= damageAmount)
            {
                RemoveStatusEffect(StatusEffectType.ARMOR, remainingDamage);
                remainingDamage = 0;
            }
            else
            {
                RemoveStatusEffect(StatusEffectType.ARMOR, currentArmor);
                remainingDamage -= currentArmor;
            }
        }

        if (remainingDamage > 0)
        {
            CurrentHealth -= remainingDamage;
            if (CurrentHealth < 0)
            {
                CurrentHealth = 0;
            }
        }

        // Recoil only the artwork: moving the combatant root can flip its facing
        // when a melee enemy stands close to the player.
        hitTime = Time.time + 0.18f;
        hitDirection = spriteRenderer != null && spriteRenderer.transform.TransformVector(Vector3.right).x * (spriteRenderer.flipX ? -1f : 1f) > 0f ? -1f : 1f;
        UpdateHealthUI();
        int healthDamage = healthBefore - CurrentHealth;
        if (healthDamage > 0) ShowDamageIndicator(healthDamage);
        if (CurrentHealth <= 0 && !deathQueued && this is EnemyView enemy && ActionSystem.Instance != null)
        {
            deathQueued = true;
            var kill = new KillEnemyGA(enemy);
            if (ActionSystem.Instance.IsPerforming) ActionSystem.Instance.AddReaction(kill);
            else ActionSystem.Instance.Perform(kill);
        }
    }

    private void ShowDamageIndicator(int amount)
    {
        var popup = new GameObject("Damage Indicator", typeof(TextMeshPro));
        popup.transform.position = new Vector3(VisualCenter.x, VisualBounds.max.y + 0.2f, VisualCenter.z - 0.2f);
        var label = popup.GetComponent<TextMeshPro>();
        label.text = "-" + amount;
        label.fontSize = 5.5f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.55f, 0.015f, 0.025f, 1f);
        label.outlineColor = new Color(1f, 0.82f, 0.48f, 1f);
        label.outlineWidth = 0.25f;
        label.sortingOrder = 100;
        StartCoroutine(AnimateDamageIndicator(popup, label));
    }

    private IEnumerator AnimateDamageIndicator(GameObject popup, TextMeshPro label)
    {
        const float duration = 1.5f;
        Vector3 start = popup.transform.position;
        Color color = label.color;
        float elapsed = 0f;
        while (elapsed < duration && popup != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            popup.transform.position = start + Vector3.up * (1f * t);
            color.a = 1f - t;
            label.color = color;
            yield return null;
        }
        if (popup != null) Destroy(popup);
    }

    public void Heal(int amount)
    {
        CurrentHealth += amount;
        if (CurrentHealth > MaxHealth)
        {
            CurrentHealth = MaxHealth;
        }
        UpdateHealthUI();
    }

    #region Context Menu Testing
    [ContextMenu("Test - Take 20 Damage")]
    public void TestDamage()
    {
        Damage(20);
    }

    [ContextMenu("Test - Heal 20 HP")]
    public void TestHeal()
    {
        Heal(20);
    }
    #endregion

    #region Status Effect Logic
    public void AddStatusEffect(StatusEffectType type, int stackCount)
    {
        if (statusEffects.ContainsKey(type))
        {
            statusEffects[type] += stackCount;
        }
        else
        {
            statusEffects.Add(type, stackCount);
        }

        if (statusEffectsUI != null)
        {
            statusEffectsUI.UpdateStatusEffectUI(type, GetStatusEffectStacks(type));
        }
    }

    public void RemoveStatusEffect(StatusEffectType type, int stackCount)
    {
        if (statusEffects.ContainsKey(type))
        {
            statusEffects[type] -= stackCount;
            if (statusEffects[type] <= 0)
            {
                statusEffects.Remove(type);
            }
        }

        if (statusEffectsUI != null)
        {
            statusEffectsUI.UpdateStatusEffectUI(type, GetStatusEffectStacks(type));
        }
    }

    public int GetStatusEffectStacks(StatusEffectType type)
    {
        if (statusEffects.ContainsKey(type)) return statusEffects[type];
        else return 0;
    }
    #endregion
}
