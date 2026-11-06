using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatantView : MonoBehaviour
{
    private static Sprite enemyIntentCircleSprite;

    [Header("UI & Visual References")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private bool preserveHealthHudLayout;
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
    public StatusEffectsUI StatusEffectsUI => statusEffectsUI;
    public SpriteRenderer MainSpriteRenderer => spriteRenderer;
    public RectTransform HealthSliderRect => healthSlider != null ? healthSlider.GetComponent<RectTransform>() : null;
    public Vector3 VisualCenter => BattleBodyBounds.center;
    // Attack placement must not depend on transparent frame padding, a weapon
    // swing, or a hurt pose. These anchors follow the combatant root instead.
    public Bounds BattleBodyBounds
    {
        get
        {
            if(!battleAnimationReady||restingSprite==null||spriteRenderer==null)
                return spriteRenderer!=null?spriteRenderer.bounds:new Bounds(transform.position,Vector3.one);
            var parent=spriteRenderer.transform.parent;
            var localCenter=new Vector3(visualPosition.x,footY-restingSprite.bounds.min.y*visualScale.y,visualPosition.z)+Vector3.Scale(restingSprite.bounds.center,visualScale);
            Vector3 size=Vector3.Scale(restingSprite.bounds.size,Vector3.Scale(visualScale,parent.lossyScale));
            return new Bounds(parent.TransformPoint(localCenter),new Vector3(Mathf.Abs(size.x),Mathf.Abs(size.y),Mathf.Abs(size.z)));
        }
    }
    public float BattleGroundY => battleAnimationReady&&spriteRenderer!=null ?
        spriteRenderer.transform.parent.TransformPoint(new Vector3(visualPosition.x,footY,visualPosition.z)).y : VisualBounds.min.y;
    public Bounds VisualBounds => spriteRenderer != null ? spriteRenderer.bounds : new Bounds(transform.position, Vector3.one);
    public bool IsHovering => hovering;

    public void ConfigureHudHealthBar(RectTransform barRect)
    {
        if (barRect == null) return;

        healthBarRect = barRect;
        baseWidth = barRect.sizeDelta.x;
        baseHealth = Mathf.Max(1, MaxHealth);
        // Let the bar grow with max HP while keeping it inside the compact HUD.
        maxWidth = Mathf.Max(baseWidth, Mathf.Min(maxWidth, 480f));

        // Keep the authored HP label placement when it is already part of the
        // authored bar. Only repair legacy labels that live elsewhere.
        if (healthText != null)
        {
            RectTransform label = healthText.rectTransform;
            if (label.parent != barRect)
            {
                label.SetParent(barRect, false);
                label.anchorMin = Vector2.zero;
                label.anchorMax = Vector2.one;
                label.pivot = new Vector2(0.5f, 0.5f);
                label.anchoredPosition = Vector2.zero;
                label.sizeDelta = Vector2.zero;
                label.SetAsLastSibling();
            }
            healthText.alignment = TextAlignmentOptions.Center;
            healthText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            healthText.verticalAlignment = VerticalAlignmentOptions.Middle;
            healthText.enableAutoSizing = true;
            healthText.fontSizeMin = 12f;
            healthText.fontSizeMax = 18f;
            healthText.enableWordWrapping = false;
            healthText.margin = Vector4.zero;
            healthText.color = Color.white;
            healthText.raycastTarget = false;
            healthTextBoundToSlider = true;
        }

        UpdateHealthUI();
    }

    /// <summary>Bind the player HUD's authored slider and label without changing their scene layout.</summary>
    public void ConfigureHudHealthBar(Slider authoredSlider, TMP_Text authoredLabel)
    {
        if (authoredSlider == null) return;

        if (healthSlider != null && healthSlider != authoredSlider)
            healthSlider.gameObject.SetActive(false);
        if (healthText != null && healthText != authoredLabel)
            healthText.gameObject.SetActive(false);

        healthSlider = authoredSlider;
        healthText = authoredLabel;
        healthBarRect = authoredSlider.GetComponent<RectTransform>();
        preserveHealthHudLayout = true;
        authoredSlider.interactable = false;
        baseWidth = healthBarRect != null ? healthBarRect.sizeDelta.x : baseWidth;
        baseHealth = Mathf.Max(1, MaxHealth);
        maxWidth = Mathf.Max(baseWidth, Mathf.Min(maxWidth, 480f));
        if (healthText != null)
        {
            healthText.alignment = TextAlignmentOptions.Center;
            healthText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            healthText.verticalAlignment = VerticalAlignmentOptions.Middle;
            healthText.enableAutoSizing = true;
            healthText.fontSizeMin = 12f;
            healthText.fontSizeMax = 18f;
            healthText.enableWordWrapping = false;
            healthText.margin = Vector4.zero;
            healthText.color = Color.white;
            healthText.raycastTarget = false;
            healthTextBoundToSlider = authoredLabel != null && authoredLabel.transform.parent == authoredSlider.transform;
        }
        UpdateHealthUI();
    }

    private Dictionary<StatusEffectType, int> statusEffects = new();
    private int natureHealAmount;
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
    private bool battleSpriteFacesLeft;
    private float hitTime;
    private float hitDirection;
    private Color restingColor = Color.white;
    private bool healthTextBoundToSlider;
    private Slider enemyBreakSlider;
    private TMP_Text enemyIntentText;
    private TMP_Text enemyBreakText;
    private RectTransform enemyHudRoot;
    private GameObject targetOutlineObject;
    private SpriteRenderer targetOutlineRenderer;
    private Vector3 FacingAnchor => battleAnimationReady && spriteRenderer != null
        ? spriteRenderer.transform.parent.TransformPoint(visualPosition) : transform.position;

    public void FaceTowards(CombatantView target)
    {
        facingTarget = target;
        UpdateFacing();
    }

    void LateUpdate()
    {
        UpdateFacing();
        if (targetOutlineRenderer != null && targetOutlineObject != null && targetOutlineObject.activeSelf && spriteRenderer != null)
        {
            targetOutlineRenderer.sprite = spriteRenderer.sprite;
            targetOutlineRenderer.flipX = spriteRenderer.flipX;
            targetOutlineRenderer.flipY = spriteRenderer.flipY;
            targetOutlineRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            targetOutlineRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
        }
    }

    public void SetTargetHighlighted(bool highlighted)
    {
        if (spriteRenderer == null) return;

        if (!highlighted)
        {
            if (targetOutlineObject != null) targetOutlineObject.SetActive(false);
            return;
        }

        if (targetOutlineObject == null)
        {
            targetOutlineObject = new GameObject("Target Highlight Outline");
            targetOutlineObject.transform.SetParent(spriteRenderer.transform, false);
            targetOutlineObject.transform.localPosition = new Vector3(0f, 0f, 0.01f);
            targetOutlineObject.transform.localRotation = Quaternion.identity;
            targetOutlineObject.transform.localScale = Vector3.one * 1.1f;
            targetOutlineRenderer = targetOutlineObject.AddComponent<SpriteRenderer>();
            targetOutlineRenderer.sharedMaterial = spriteRenderer.sharedMaterial;
            targetOutlineRenderer.color = new Color(1f, 0.06f, 0.04f, 1f);
            targetOutlineRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            targetOutlineRenderer.maskInteraction = spriteRenderer.maskInteraction;
            targetOutlineRenderer.drawMode = spriteRenderer.drawMode;
            targetOutlineRenderer.size = spriteRenderer.size;
        }

        targetOutlineObject.SetActive(true);
        targetOutlineRenderer.sprite = spriteRenderer.sprite;
        targetOutlineRenderer.flipX = spriteRenderer.flipX;
        targetOutlineRenderer.flipY = spriteRenderer.flipY;
        targetOutlineRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        targetOutlineRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
    }

    void UpdateFacing()
    {
        if (spriteRenderer == null || facingTarget == null) return;
        float delta = facingTarget.FacingAnchor.x - FacingAnchor.x;
        if (Mathf.Abs(delta) > 0.35f)
        {
            // The enemy prefab rotates its visual parent 180 degrees around Y.
            // Compare against the transformed sprite axis to avoid flipping twice.
            float spriteAxis = spriteRenderer.transform.TransformVector(Vector3.right).x;
            spriteRenderer.flipX = (delta * spriteAxis < 0f) ^ battleSpriteFacesLeft;
        }
    }

    protected void ConfigureBattleAnimation(Sprite[] idle, Sprite[] attack, float fps, bool faceLeft, bool hover = false,
        float[] frameDurations = null, bool sourceFacesLeft = false)
    {
        if (spriteRenderer == null) return;
        idleFrames = idle;
        attackFrames = attack;
        attackFrameDurations = frameDurations;
        battleFrameRate = Mathf.Max(1f, fps);
        hovering = hover;
        battleSpriteFacesLeft = sourceFacesLeft;
        spriteRenderer.flipX = faceLeft ^ battleSpriteFacesLeft;
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
        if(this is HeroView&&CurrentHealth>0&&Time.time<hurtAnimationUntil&&heroHurtFrames!=null)
        {
            SetBattleFrame(heroHurtFrames[Mathf.Min(heroHurtFrames.Length-1,Mathf.FloorToInt((Time.time-hurtAnimationStart)*10f))],1f,
                hovering ? Mathf.Sin(idleClock * 2f) * 0.12f : 0f, hurtUsesFootPivot);
            return;
        }
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

    void SetBattleFrame(Sprite frame, float breath, float lift, bool useFootPivot = false)
    {
        if (spriteRenderer == null || frame == null) return;
        spriteRenderer.sprite = frame;
        spriteRenderer.transform.localScale = new Vector3(visualScale.x, visualScale.y * breath, visualScale.z);
        // Keep the feet planted despite tightly-cropped attack frames.
        float recoil = Mathf.Max(0f, (hitTime - Time.time) / 0.18f);
        float axis = spriteRenderer.transform.parent.TransformVector(Vector3.right).x;
        spriteRenderer.transform.localPosition = new Vector3(visualPosition.x + hitDirection * Mathf.Sign(axis) * Mathf.Sin(recoil * Mathf.PI) * 0.12f,
            footY - (useFootPivot ? 0f : frame.bounds.min.y) * visualScale.y * breath + lift, visualPosition.z);
    }

    public IEnumerator PlayAttackAnimation()
    {
        return PlayAttackAnimationWithRelease(0f, null);
    }

    public IEnumerator PlayChargedBowAttack(System.Action onRelease)
    {
        return PlayAttackAnimationWithRelease(1.5f, onRelease);
    }

    private IEnumerator PlayAttackAnimationWithRelease(float drawHold, System.Action onRelease)
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
                    OnAttackFrame(i, attackFrames.Length);
                    // ElfBasicAtk001BGR-Sheet-export_2 is the fully drawn bow
                    // pose. Release the special projectile on the following pose.
                    int releaseFrame = Mathf.Min(3, attackFrames.Length - 1);
                    if (i == releaseFrame) onRelease?.Invoke();
                    // Preserve the default swing timing unless the hero supplies
                    // weapon-specific draw, release and recovery durations.
                    float timing = i == 0 ? 1.4f : i == attackFrames.Length - 1 ? 1.5f : 0.8f;
                    float duration = attackFrameDurations != null && i < attackFrameDurations.Length &&
                        attackFrameDurations[i] > 0f ? attackFrameDurations[i] : timing / battleFrameRate;
                    if (drawHold > 0f)
                    {
                        if (i == Mathf.Min(2, attackFrames.Length - 1))
                            duration += drawHold;
                        yield return new WaitForSecondsRealtime(duration);
                    }
                    else yield return new WaitForSeconds(duration);
                }
            }
            else
            {
                if (drawHold > 0f) yield return new WaitForSecondsRealtime(drawHold);
                onRelease?.Invoke();
                // Existing combatants without a sheet still have an attack pose.
                spriteRenderer.transform.localScale = new Vector3(visualScale.x * 1.08f, visualScale.y * 0.96f, visualScale.z);
                yield return new WaitForSeconds(0.25f);
            }
        }
        finally
        {
            OnAttackFinished();
            attacking = false;
            if (!hovering && attackFrameDurations != null && attackFrameDurations.Length > 0) idleClock = 0f;
            if (spriteRenderer != null)
                SetBattleFrame(restingSprite, 1f, hovering ? Mathf.Sin(idleClock * 2f) * 0.12f : 0f);
        }
    }

    protected IEnumerator PlayBattleFramesOnce(Sprite[] frames, float framesPerSecond)
    {
        if (!battleAnimationReady || CurrentHealth <= 0 || frames == null || frames.Length == 0 || attacking) yield break;
        attacking = true;
        float frameDelay = 1f / Mathf.Max(1f, framesPerSecond);
        try
        {
            foreach (Sprite frame in frames)
            {
                if (CurrentHealth <= 0) yield break;
                SetBattleFrame(frame, 1f, hovering ? Mathf.Sin(idleClock * 2f) * 0.12f : 0f);
                yield return new WaitForSeconds(frameDelay);
            }
        }
        finally
        {
            attacking = false;
            if (spriteRenderer != null && CurrentHealth > 0)
                SetBattleFrame(restingSprite, 1f, hovering ? Mathf.Sin(idleClock * 2f) * 0.12f : 0f);
        }
    }

    protected virtual void OnAttackFrame(int index, int count) { }
    protected virtual void OnAttackFinished() { }

    protected IEnumerator PlayBattleTravel(Vector3 destination, float duration, Sprite[] frames)
    {
        Vector3 origin = transform.position;
        attacking = true;
        float elapsed = 0f;
        try
        {
            while (elapsed < duration && CurrentHealth > 0)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(origin, destination, 1f - (1f - t) * (1f - t));
                if (frames != null && frames.Length > 0)
                    SetBattleFrame(frames[Mathf.Min(frames.Length - 1, Mathf.FloorToInt(t * frames.Length))], 1f, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (CurrentHealth > 0) transform.position = destination;
        }
        finally
        {
            attacking = false;
            if (spriteRenderer != null) SetBattleFrame(restingSprite, 1f, 0f);
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

        RestorePlayerHealth();
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
        RestorePlayerHealth();
        UpdateHealthUI();
    }

    private void RestorePlayerHealth()
    {
        if (!(this is HeroView) || !Application.isPlaying) return;
        PlayerHealth.ConfigureMaximum(MaxHealth);
        CurrentHealth = PlayerHealth.CurrentHealth;
    }

    protected void EnsureEnemyHealthBar()
    {
        Transform existingRoot = transform.Find("Enemy Health Bar");
        if (existingRoot == null)
        {
            var rootObject = new GameObject("Enemy Health Bar", typeof(RectTransform), typeof(Canvas));
            rootObject.transform.SetParent(transform, false);
            existingRoot = rootObject.transform;
        }
        enemyHudRoot = existingRoot.GetComponent<RectTransform>();
        float top = spriteRenderer != null ? spriteRenderer.bounds.max.y - transform.position.y : 2f;
        enemyHudRoot.localPosition = new Vector3(0f, top + 0.6f, -0.1f);
        enemyHudRoot.localScale = Vector3.one * 0.013f;
        enemyHudRoot.sizeDelta = new Vector2(380, 140);
        var canvas = existingRoot.GetComponent<Canvas>();
        if (canvas == null) canvas = existingRoot.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 50;

        if (healthSlider == null)
        {
            // EnemyView now builds its HP bar and label on a dedicated world
            // canvas. Hide the legacy HealthText inherited from CombatantViewBase
            // so it does not overlap the generated HP label.
            if (healthText != null)
                healthText.gameObject.SetActive(false);

            Transform oldLabel = existingRoot.Find("HP Label");
            if (oldLabel != null) Destroy(oldLabel.gameObject);
            var bar = new GameObject("HP Slider", typeof(RectTransform), typeof(Image), typeof(Slider));
            bar.transform.SetParent(existingRoot, false);
            var barRect = bar.GetComponent<RectTransform>();
            barRect.anchorMin = barRect.anchorMax = new Vector2(0.5f, 0.5f);
            barRect.pivot = new Vector2(0.5f, 0.5f);
            barRect.sizeDelta = new Vector2(220, 28);
            barRect.anchoredPosition = new Vector2(30, 6);
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
            healthBarRect = null; // Enemy bars retain a readable, fixed width.

            var label = new GameObject("HP Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(existingRoot, false);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.sizeDelta = new Vector2(220, 28);
            labelRect.anchoredPosition = new Vector2(30, 6);
            healthText = label.GetComponent<TextMeshProUGUI>();
            healthText.fontSize = 22;
            healthText.alignment = TextAlignmentOptions.Center;
            healthText.color = new Color(1f, 0.9f, 0.67f);
            healthText.raycastTarget = false;
        }

        CreateEnemyIntentAndBreakUI(existingRoot);
        if (this is EnemyView && statusEffectsUI != null)
            statusEffectsUI.MoveToEnemyHud(enemyHudRoot);
        if (this is EnemyView enemyView)
            UpdateEnemyIntentUI(enemyView.AttackCountdown, enemyView.BreakMeter, enemyView.MaxBreakMeter);
        UpdateHealthUI();
    }

    private void CreateEnemyIntentAndBreakUI(Transform root)
    {
        Transform intent = root.Find("Enemy Attack Countdown");
        if (intent == null)
        {
            var outer = new GameObject("Enemy Attack Countdown", typeof(RectTransform), typeof(Image));
            outer.transform.SetParent(root, false);
            intent = outer.transform;
            var rect = outer.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(40, 40);
            rect.anchoredPosition = new Vector2(-108, 6);
            var image = outer.GetComponent<Image>();
            image.sprite = GetEnemyIntentCircleSprite();
            image.color = new Color(1f, 0.73f, 0.12f, 1f);
            image.raycastTarget = false;

            var inner = new GameObject("Countdown Face", typeof(RectTransform), typeof(Image));
            inner.transform.SetParent(outer.transform, false);
            var innerRect = inner.GetComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(4, 4);
            innerRect.offsetMax = new Vector2(-4, -4);
            var innerImage = inner.GetComponent<Image>();
            innerImage.sprite = GetEnemyIntentCircleSprite();
            innerImage.color = new Color(0.18f, 0.11f, 0.05f, 1f);
            innerImage.raycastTarget = false;
            var number = new GameObject("Countdown Number", typeof(RectTransform), typeof(TextMeshProUGUI));
            number.transform.SetParent(outer.transform, false);
            var numberRect = number.GetComponent<RectTransform>();
            numberRect.anchorMin = Vector2.zero;
            numberRect.anchorMax = Vector2.one;
            numberRect.offsetMin = numberRect.offsetMax = Vector2.zero;
            enemyIntentText = number.GetComponent<TextMeshProUGUI>();
            ConfigureEnemyHudText(enemyIntentText, 22, new Color(1f, 0.93f, 0.72f));
        }
        else enemyIntentText = intent.Find("Countdown Number")?.GetComponent<TMP_Text>();

        // Keep existing authored countdown objects aligned with the HP bar too,
        // not just the countdown object created at runtime for a new enemy.
        RectTransform intentRect = intent as RectTransform;
        RectTransform hpRect = root.Find("HP Slider") as RectTransform;
        if (intentRect != null)
        {
            intentRect.anchorMin = intentRect.anchorMax = new Vector2(0.5f, 0.5f);
            intentRect.pivot = new Vector2(0.5f, 0.5f);
            float hpCenterY = hpRect != null ? hpRect.anchoredPosition.y : 6f;
            intentRect.anchoredPosition = new Vector2(-108f, hpCenterY);
        }

        Transform breakBar = root.Find("Enemy Break Meter");
        if (breakBar == null)
        {
            var bar = new GameObject("Enemy Break Meter", typeof(RectTransform), typeof(Image), typeof(Slider));
            bar.transform.SetParent(root, false);
            breakBar = bar.transform;
            var rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(220, 18);
            rect.anchoredPosition = new Vector2(30, -21);
            bar.GetComponent<Image>().color = new Color(0.1f, 0.22f, 0.32f);
            bar.GetComponent<Image>().raycastTarget = false;
            var fill = new GameObject("Break Blue Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(bar.transform, false);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = new Color(0.1f, 0.62f, 0.95f);
            fill.GetComponent<Image>().raycastTarget = false;
            enemyBreakSlider = bar.GetComponent<Slider>();
            enemyBreakSlider.interactable = false;
            enemyBreakSlider.transition = Selectable.Transition.None;
            enemyBreakSlider.navigation = new Navigation { mode = Navigation.Mode.None };
            enemyBreakSlider.fillRect = fillRect;
            enemyBreakSlider.direction = Slider.Direction.LeftToRight;
            enemyBreakSlider.minValue = 0;
            enemyBreakSlider.wholeNumbers = true;

            var label = new GameObject("Break Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(bar.transform, false);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            enemyBreakText = label.GetComponent<TextMeshProUGUI>();
            ConfigureEnemyHudText(enemyBreakText, 13, Color.white);
        }
        else
        {
            enemyBreakSlider = breakBar.GetComponent<Slider>();
            enemyBreakText = breakBar.Find("Break Label")?.GetComponent<TMP_Text>();
        }
    }

    private static Sprite GetEnemyIntentCircleSprite()
    {
        if (enemyIntentCircleSprite != null) return enemyIntentCircleSprite;

        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Enemy Intent Circle Texture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        var pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        float radius = size * 0.47f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(radius + 0.75f - distance) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        enemyIntentCircleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        enemyIntentCircleSprite.name = "Enemy Intent Circle";
        return enemyIntentCircleSprite;
    }

    private static void ConfigureEnemyHudText(TMP_Text text, float size, Color color)
    {
        if (text == null) return;
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(10, size - 3);
        text.fontSizeMax = size;
        text.alignment = TextAlignmentOptions.Center;
        text.horizontalAlignment = HorizontalAlignmentOptions.Center;
        text.verticalAlignment = VerticalAlignmentOptions.Middle;
        text.color = color;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.margin = Vector4.zero;
    }

    public void UpdateEnemyIntentUI(int countdown, int breakValue, int breakMax)
    {
        if (enemyIntentText != null) enemyIntentText.text = Mathf.Max(0, countdown).ToString();
        if (enemyBreakSlider != null)
        {
            enemyBreakSlider.maxValue = Mathf.Max(1, breakMax);
            enemyBreakSlider.value = Mathf.Clamp(breakValue, 0, breakMax);
        }
        if (enemyBreakText != null)
            enemyBreakText.text = breakValue <= 0 ? "BROKEN" : $"BREAK {breakValue}/{breakMax}";
    }

    public void SetEnemyHealthBarRow(int row)
    {
        if (!(this is EnemyView) || row <= 0) return;
        Transform barRoot = transform.Find("Enemy Health Bar");
        if (barRoot == null) return;
        Vector3 position = barRoot.localPosition;
        position.y += row * 0.42f;
        barRoot.localPosition = position;
    }

    protected void EnlargeEnemyVisual(float targetHeight = 2.8f)
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;
        float height = spriteRenderer.bounds.size.y;
        if (height > 0.01f) spriteRenderer.transform.localScale *= targetHeight / height;
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
        if (!preserveHealthHudLayout && healthBarRect != null && baseHealth > 0)
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
        if (preserveHealthHudLayout || healthTextBoundToSlider || healthText == null || healthSlider == null) return;
        RectTransform label = healthText.rectTransform;
        label.SetParent(healthSlider.transform, false);
        label.anchorMin = Vector2.zero;
        label.anchorMax = Vector2.one;
        label.pivot = new Vector2(0.5f, 0.5f);
        label.anchoredPosition = Vector2.zero;
        label.sizeDelta = Vector2.zero;
        label.SetAsLastSibling();
        healthText.horizontalAlignment = HorizontalAlignmentOptions.Center;
        healthText.verticalAlignment = VerticalAlignmentOptions.Middle;
        healthText.alignment = TextAlignmentOptions.Center;
        healthText.fontSize = 18f;
        healthText.enableAutoSizing = true;
        healthText.fontSizeMin = 12f;
        healthText.fontSizeMax = 18f;
        healthText.enableWordWrapping = false;
        healthText.margin = Vector4.zero;
        healthText.color = Color.white;
        healthText.raycastTarget = false;
        healthTextBoundToSlider = true;
    }

    private Sprite[] heroHurtFrames;
    private bool hurtUsesFootPivot;
    private float hurtAnimationStart,hurtAnimationUntil;
    public void Damage(int damageAmount)
    {
        int healthBefore = CurrentHealth;
        // Positive attacks always get through for at least one point before armor.
        // This prevents low-level enemies from being completely negated by base defense.
        int remainingDamage = damageAmount > 0 ? Mathf.Max(1, damageAmount - Defense) : 0;
        int currentArmor = GetStatusEffectStacks(StatusEffectType.ARMOR);

        if (currentArmor > 0 && remainingDamage > 0)
        {
            int absorbedDamage = Mathf.Min(currentArmor, remainingDamage);
            RemoveStatusEffect(StatusEffectType.ARMOR, absorbedDamage);
            remainingDamage -= absorbedDamage;
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
        if (healthDamage > 0 && this is EnemyView enemyView)
            enemyView.ApplyBreakDamage(healthDamage);
        if (this is HeroView && healthDamage > 0 && CurrentHealth > 0 && spriteRenderer != null && restingSprite != null)
        {
            var bank = Resources.Load<CharacterHurtFrames>("PlayerDeath/Hurt");
            if (bank != null)
            {
                int character = PlayerPrefs.GetInt("SelectedCharacter", 0);
                hurtUsesFootPivot = character == SelectedHero.ArcherIndex || character == SelectedHero.MageIndex;
                heroHurtFrames = bank.Frames(character, restingSprite.pixelsPerUnit);
                if (heroHurtFrames != null && heroHurtFrames.Length > 0)
                {
                    hurtAnimationStart = Time.time;
                    hurtAnimationUntil = Time.time + heroHurtFrames.Length / 10f;
                }
            }
        }
        if (this is HeroView && Application.isPlaying) PlayerHealth.Set(CurrentHealth, MaxHealth);
        if (healthDamage > 0)
        {
            ShowDamageIndicator(healthDamage);
        }
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
        // The popup is parented to this combatant only through its coroutine.
        // Destroy it independently too, so killing the enemy cannot stop cleanup.
        Destroy(popup, 1.6f);
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
        if (this is HeroView && Application.isPlaying) PlayerHealth.Set(CurrentHealth, MaxHealth);
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
        if (stackCount <= 0) return;

        // Nature uses its displayed stacks as a turn countdown. Reapplying it
        // refreshes the timer without turning the countdown into an ever-growing stack.
        if (type == StatusEffectType.NATURE)
        {
            statusEffects[type] = Mathf.Max(GetStatusEffectStacks(type), stackCount);
            if (statusEffectsUI != null)
                statusEffectsUI.UpdateStatusEffectUI(type, GetStatusEffectStacks(type));
            return;
        }

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

    public void ScheduleNatureHeal(int turnsUntilHeal, int healAmount)
    {
        if (!(this is HeroView) || turnsUntilHeal <= 0 || healAmount <= 0) return;
        natureHealAmount = Mathf.Max(natureHealAmount, healAmount);
        AddStatusEffect(StatusEffectType.NATURE, turnsUntilHeal);
    }

    public void TickNatureHeal()
    {
        if (!(this is HeroView)) return;
        int turnsRemaining = GetStatusEffectStacks(StatusEffectType.NATURE);
        if (turnsRemaining <= 0) return;

        if (turnsRemaining > 1)
        {
            RemoveStatusEffect(StatusEffectType.NATURE, 1);
            return;
        }

        RemoveStatusEffect(StatusEffectType.NATURE, turnsRemaining);
        int amount = natureHealAmount;
        natureHealAmount = 0;
        if (amount > 0 && CurrentHealth > 0)
        {
            Heal(amount);
            StatusEffectSystem.PlayHealAnimation(this);
        }
    }
    #endregion
}
