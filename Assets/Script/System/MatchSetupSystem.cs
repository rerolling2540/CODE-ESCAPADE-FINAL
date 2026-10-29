using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MatchSetupSystem : MonoBehaviour
{
    [SerializeField] private HeroData heroData;
    [SerializeField] private PerkData perkData;
    [SerializeField] private List<EnemyData> enemyDatas;
    [Header("Battle HUD Portraits")]
    [SerializeField] private Sprite warriorHudPortrait;
    [SerializeField] private Sprite mageHudPortrait;
    [SerializeField] private Sprite archerHudPortrait;

    private void OnEnable()
    {
        ActionSystem.AttachPerformer<StarfallSkillGA>(StarfallSkillPerformer);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<StarfallSkillGA>();
    }

    private void Update()
    {
        BattleHudMedieval.UpdateEpSkillAvailability();
    }

    private void Start()
    {
        BattleHudMedieval.ResetEpMeter();
        BattleSettingsMenu.EnsureCreated();
        BattleAudio.EnsureInstance();
        heroData = SelectedHero.Resolve(heroData);
        BattleUITheme.Apply();
        HeroSystem.Instance.SetUp(heroData);
        ApplyPlayerHudLayout(ResolveHudPortrait(), HeroSystem.Instance.HeroView);
        bool caveBattle = CaveEnemyEncounter.PendingEnemies != null && CaveEnemyEncounter.PendingEnemies.Count > 0;
        string encounterId = CaveEnemyEncounter.PendingEncounterId;
        if (caveBattle)
        {
            EnemySystem.Instance.Setup(CaveEnemyEncounter.PendingEnemies);
            CaveEnemyEncounter.PendingEnemies = null;
            CaveEnemyEncounter.PendingEncounterId = null;
        }
        else EnemySystem.Instance.Setup(enemyDatas);
        var platform = FindFirstObjectByType<BattlePlatform>();
        if (platform != null) platform.GroundCombatants(HeroSystem.Instance.HeroView, EnemySystem.Instance.Enemies);
        foreach (var enemy in EnemySystem.Instance.Enemies)
            enemy.FaceTowards(HeroSystem.Instance.HeroView);
        if (EnemySystem.Instance.Enemies.Count > 0)
            HeroSystem.Instance.HeroView.FaceTowards(EnemySystem.Instance.Enemies[0]);
        gameObject.AddComponent<EncounterBattleReturn>().Configure(caveBattle, encounterId);
        List<CardData> ownedBattleDeck = CharacterInventory.WithChestRewards(heroData.Deck);
        Debug.Log($"Battle deck created from this save slot's owned inventory: {ownedBattleDeck.Count} card(s). Unowned catalog cards are excluded.");
        CardSystem.Instance.Setup(ownedBattleDeck);
        // Keep the starting perk's effect active without drawing its crossed-swords badge.
        PerkSystem.Instance.AddPerk(new Perk(perkData), showIcon: false);
        DrawCardsGA drawCardsGA = new(5);
        ActionSystem.Instance.Perform(drawCardsGA);
        if (BattleTutorialGuide.IsTutorialEncounter(encounterId)) gameObject.AddComponent<BattleTutorialGuide>();
    }

    private IEnumerator StarfallSkillPerformer(StarfallSkillGA action)
    {
        HeroView hero = HeroSystem.Instance != null ? HeroSystem.Instance.HeroView : null;
        List<CombatantView> targets = EnemySystem.Instance != null && EnemySystem.Instance.Enemies != null
            ? EnemySystem.Instance.Enemies.FindAll(enemy => enemy != null && enemy.CurrentHealth > 0)
                .ConvertAll(enemy => (CombatantView)enemy)
            : new List<CombatantView>();
        if (hero == null || hero.CurrentHealth <= 0 || targets.Count == 0 || !BattleHudMedieval.TrySpendFullEp())
            yield break;

        BattleAudio.Play(BattleSound.MagicCast);
        yield return hero.PlayBattleAttack(targets[0]);
        if (hero == null || hero.CurrentHealth <= 0) yield break;

        int damage = Mathf.Max(1, hero.AttackPower * 2);
        ActionSystem.Instance.AddReaction(new DealDamageGA(damage, targets, hero));
    }

    private Sprite ResolveHudPortrait()
    {
        if (SelectedHero.IsMage && mageHudPortrait != null) return mageHudPortrait;
        if (SelectedHero.IsArcher && archerHudPortrait != null) return archerHudPortrait;
        return warriorHudPortrait != null ? warriorHudPortrait : heroData != null ? heroData.Image : null;
    }

    private static void ApplyPlayerHudLayout(Sprite portrait, HeroView hero)
    {
        var portraitFrame = GameObject.Find("PortraitFrame");
        if (portraitFrame != null && portraitFrame.GetComponent<UnityEngine.UI.RectMask2D>() == null)
            portraitFrame.AddComponent<UnityEngine.UI.RectMask2D>();

        BattleHudMedieval.Apply(portrait, hero);
    }
}

/// <summary>
/// Applies the ToffeeCraft medieval sprite sheet to the battle HUD while
/// retaining the existing five mana bubbles and health slider behavior.
/// </summary>
public static class BattleHudMedieval
{
    private const string AtlasResourcePath = "ToffeeCraft/MediavelFree";
    private static Texture2D atlas;
    private static Sprite portraitFrameSprite;
    private static Sprite healthBarSprite;
    private static Sprite healthFillSprite;
    private static Sprite epStarEmptySprite;
    private static Sprite epStarFillSprite;
    private static readonly Image[] epStarFills = new Image[5];
    private static int currentEp;
    private static Button epSkillButton;
    private static TMP_Text epSkillChargeText;
    private const int MaxEp = 5;

    public static void ResetEpMeter()
    {
        currentEp = 0;
        for (int i = 0; i < epStarFills.Length; i++) epStarFills[i] = null;
        epSkillButton = null;
        epSkillChargeText = null;
        RefreshEpMeter();
    }

    public static void GainEpFromCard()
    {
        AddEp(1);
    }

    public static void GainEpFromDamage()
    {
        AddEp(1);
    }

    private static void AddEp(int amount)
    {
        currentEp = Mathf.Clamp(currentEp + amount, 0, MaxEp);
        RefreshEpMeter();
    }

    public static bool TrySpendFullEp()
    {
        if (currentEp < MaxEp) return false;
        currentEp = 0;
        RefreshEpMeter();
        return true;
    }

    public static void UpdateEpSkillAvailability()
    {
        if (epSkillButton == null) return;
        bool resolving = ActionSystem.Instance != null && ActionSystem.Instance.IsPerforming;
        bool ready = currentEp >= MaxEp && !resolving && !QuestionUI.IsQuestionActive &&
            !BattleTutorialGuide.BlocksInput && !EncounterBattleReturn.BattleEnded &&
            EnemySystem.Instance != null && EnemySystem.Instance.Enemies.Exists(enemy => enemy != null && enemy.CurrentHealth > 0);
        epSkillButton.interactable = ready;
    }

    private static void RefreshEpMeter()
    {
        for (int i = 0; i < epStarFills.Length; i++)
            if (epStarFills[i] != null) epStarFills[i].fillAmount = currentEp > i ? 1f : 0f;

        if (epSkillChargeText != null)
            epSkillChargeText.text = currentEp >= MaxEp ? "READY" : $"CHARGE {currentEp}/{MaxEp}";
        UpdateEpSkillAvailability();
    }

    public static void Apply(Sprite portrait, HeroView hero)
    {
        GameObject hudObject = GameObject.Find("PlaayerHUD");
        if (hudObject == null) return;

        // Build the EP meter independently so the combat resource still works
        // if the optional medieval HUD frame atlas cannot be loaded.
        ApplyEpStar(hudObject.GetComponentInParent<Canvas>());

        if (atlas == null) atlas = Resources.Load<Texture2D>(AtlasResourcePath);
        if (atlas == null)
        {
            Debug.LogWarning("Medieval battle HUD sprite sheet is missing from Resources/ToffeeCraft.");
            return;
        }

        atlas.filterMode = FilterMode.Point;
        if (portraitFrameSprite == null) portraitFrameSprite = Slice("HUD portrait frame", new Rect(0, 45, 76, 77), new Vector4(12, 12, 12, 12));
        if (healthBarSprite == null) healthBarSprite = Slice("HUD health bar", new Rect(2, 18, 61, 12), new Vector4(8, 4, 8, 4));
        if (healthFillSprite == null) healthFillSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));

        var hudRect = hudObject.GetComponent<RectTransform>();
        var hudImage = hudObject.GetComponent<UnityEngine.UI.Image>();
        if (hudRect == null || hudImage == null) return;

        hudRect.anchorMin = hudRect.anchorMax = new Vector2(0, 1);
        hudRect.pivot = new Vector2(0, 1);
        hudRect.anchoredPosition = new Vector2(16, -14);
        hudRect.sizeDelta = new Vector2(456, 118);
        hudImage.enabled = false;
        hudImage.raycastTarget = false;

        ApplyPortrait(hudRect, portrait);
        ApplyHealthBar(hudRect);
        if (hero != null)
        {
            var playerHealthBar = GameObject.Find("healthSlider");
            hero.ConfigureHudHealthBar(playerHealthBar != null ? playerHealthBar.GetComponent<RectTransform>() : null);
        }
        ApplyManaBubbles();
        ApplyPlayerStatusIcons(hudRect, hero);
        RemoveCoinBalance(hudRect);
    }

    private static Sprite Slice(string spriteName, Rect rect, Vector4 border)
    {
        Sprite sprite = Sprite.Create(atlas, rect, new Vector2(0.5f, 0.5f), 100, 0,
            SpriteMeshType.FullRect, border, false);
        sprite.name = spriteName;
        return sprite;
    }

    private static void ApplyPortrait(RectTransform hudRect, Sprite portrait)
    {
        GameObject frameObject = GameObject.Find("PortraitFrame");
        GameObject iconObject = GameObject.Find("PortraitIcon");
        if (frameObject == null || iconObject == null) return;

        var frameRect = frameObject.GetComponent<RectTransform>();
        var frameImage = frameObject.GetComponent<UnityEngine.UI.Image>();
        if (frameRect == null || frameImage == null) return;

        frameRect.SetParent(hudRect, false);
        frameRect.anchorMin = frameRect.anchorMax = new Vector2(0, 1);
        frameRect.pivot = new Vector2(0, 1);
        // Leave visible breathing room below the health bar.
        frameRect.anchoredPosition = new Vector2(10, -46);
        frameRect.sizeDelta = new Vector2(74, 72);
        frameImage.sprite = portraitFrameSprite;
        frameImage.type = UnityEngine.UI.Image.Type.Sliced;
        frameImage.color = Color.white;
        frameImage.raycastTarget = false;

        var iconRect = iconObject.GetComponent<RectTransform>();
        var icon = iconObject.GetComponent<UnityEngine.UI.Image>();
        if (iconRect == null || icon == null) return;
        iconRect.SetParent(frameRect, false);
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(66, 66);
        iconRect.SetAsLastSibling();
        if (portrait != null) icon.sprite = portrait;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
    }

    private static void ApplyHealthBar(RectTransform hudRect)
    {
        GameObject sliderObject = GameObject.Find("healthSlider");
        var slider = sliderObject != null ? sliderObject.GetComponent<UnityEngine.UI.Slider>() : null;
        if (slider == null) return;

        var sliderRect = slider.GetComponent<RectTransform>();
        sliderRect.SetParent(hudRect, false);
        sliderRect.anchorMin = sliderRect.anchorMax = new Vector2(0, 1);
        sliderRect.pivot = new Vector2(0, 1);
        // Keep the bar at a fixed panel width. The hero's health ratio changes
        // the fill amount only, never the overall bar size.
        sliderRect.anchoredPosition = new Vector2(10, -8);
        sliderRect.sizeDelta = new Vector2(320, 28);
        sliderRect.localScale = Vector3.one;
        slider.interactable = false;

        var backgroundTransform = slider.transform.Find("Background") as RectTransform;
        if (backgroundTransform != null)
        {
            backgroundTransform.anchorMin = new Vector2(0, 0.05f);
            backgroundTransform.anchorMax = new Vector2(1, 0.95f);
            backgroundTransform.anchoredPosition = Vector2.zero;
            backgroundTransform.sizeDelta = Vector2.zero;
        }

        var fillArea = slider.transform.Find("Fill Area") as RectTransform;
        if (fillArea != null)
        {
            fillArea.anchorMin = new Vector2(0, 0.05f);
            fillArea.anchorMax = new Vector2(1, 0.95f);
            fillArea.anchoredPosition = Vector2.zero;
            fillArea.sizeDelta = new Vector2(-10, 0);
        }

        var background = backgroundTransform != null ? backgroundTransform.GetComponent<UnityEngine.UI.Image>() : null;
        if (background != null)
        {
            background.sprite = healthBarSprite;
            background.type = UnityEngine.UI.Image.Type.Sliced;
            // Keep a dark green empty track under the green health fill.
            background.color = new Color(0.16f, 0.25f, 0.14f, 1f);
            background.raycastTarget = false;
        }

        var fill = slider.fillRect != null ? slider.fillRect.GetComponent<UnityEngine.UI.Image>() : null;
        if (fill != null)
        {
            // Slider already sizes its Fill Rect from the health ratio. A flat sprite
            // prevents the atlas bar's rounded ends from stretching with that width.
            fill.sprite = healthFillSprite;
            fill.type = UnityEngine.UI.Image.Type.Simple;
            fill.color = new Color(0.25f, 0.78f, 0.28f, 1f);
            fill.raycastTarget = false;
        }
    }

    private static void ApplyManaBubbles()
    {
        var manaUI = Object.FindFirstObjectByType<ManaUI>(FindObjectsInactive.Include);
        if (manaUI == null) return;

        var rect = manaUI.GetComponent<RectTransform>();
        var hud = GameObject.Find("PlaayerHUD");
        if (hud != null) rect.SetParent(hud.transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        // Keep mana below the taller health bar, with a small gap.
        rect.anchoredPosition = new Vector2(96, -48);
        rect.sizeDelta = new Vector2(140, 28);

        int index = 0;
        foreach (RectTransform child in rect)
        {
            if (child.GetComponent<UnityEngine.UI.Image>() == null) continue;
            child.anchorMin = child.anchorMax = new Vector2(0, 0.5f);
            child.pivot = new Vector2(0.5f, 0.5f);
            child.sizeDelta = new Vector2(20, 20);
            child.anchoredPosition = new Vector2(10 + index * 23, 0);
            index++;
        }
    }

    private static void ApplyEpStar(Canvas hudCanvas)
    {
        if (hudCanvas == null) return;

        EnsureEpStarSprites();
        Transform existing = hudCanvas.transform.Find("EP Star Meter");
        GameObject meterObject = existing != null
            ? existing.gameObject
            : new GameObject("EP Star Meter", typeof(RectTransform));
        meterObject.transform.SetParent(hudCanvas.transform, false);

        var meterRect = meterObject.GetComponent<RectTransform>();
        meterRect.anchorMin = meterRect.anchorMax = Vector2.zero;
        meterRect.pivot = Vector2.zero;
        meterRect.anchoredPosition = new Vector2(18f, 125f);
        meterRect.sizeDelta = new Vector2(82f, 270f);

        // Reuse the original single-star objects as the first star when this
        // HUD is rebuilt during Play Mode/domain reload.
        Transform oldEmptyStar = meterRect.Find("Empty Star");
        if (oldEmptyStar != null && meterRect.Find("EP Star 1 Empty") == null)
            oldEmptyStar.name = "EP Star 1 Empty";
        Transform oldEnergyFill = meterRect.Find("Energy Fill");
        if (oldEnergyFill != null && meterRect.Find("EP Star 1 Fill") == null)
            oldEnergyFill.name = "EP Star 1 Fill";

        // Five independent stars fill one by one as cards are used or combatants
        // lose health. The special skill consumes all five.
        const int starCount = 5;
        const float starSize = 42f;
        const float starSpacing = 47f;
        for (int i = 0; i < starCount; i++)
        {
            float y = 28f + i * starSpacing;
            Image baseStar = GetOrCreateImage(meterRect, $"EP Star {i + 1} Empty", epStarEmptySprite);
            PositionEpStarImage(baseStar.rectTransform, new Vector2(41f, y), new Vector2(starSize, starSize));
            baseStar.color = Color.white;

            Image energyFill = GetOrCreateImage(meterRect, $"EP Star {i + 1} Fill", epStarFillSprite);
            PositionEpStarImage(energyFill.rectTransform, new Vector2(41f, y), new Vector2(35f, 35f));
            energyFill.type = Image.Type.Filled;
            energyFill.fillMethod = Image.FillMethod.Vertical;
            energyFill.fillOrigin = (int)Image.OriginVertical.Bottom;
            energyFill.fillAmount = currentEp > i ? 1f : 0f;
            energyFill.color = Color.white;
            epStarFills[i] = energyFill;
        }

        Transform labelTransform = meterRect.Find("EP Label");
        TextMeshProUGUI label;
        if (labelTransform == null)
        {
            var labelObject = new GameObject("EP Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(meterRect, false);
            label = labelObject.GetComponent<TextMeshProUGUI>();
        }
        else label = labelTransform.GetComponent<TextMeshProUGUI>();

        if (label != null)
        {
            label.text = "EP";
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = 16f;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.55f, 0.88f, 1f, 1f);
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            var labelRect = label.rectTransform;
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 0f);
            labelRect.sizeDelta = new Vector2(82f, 22f);
        }

        CreateGoldSkillCard(hudCanvas.transform);
        RefreshEpMeter();
    }

    private static void CreateGoldSkillCard(Transform canvasTransform)
    {
        Transform existing = canvasTransform.Find("EP Gold Skill Card");
        GameObject cardObject = existing != null
            ? existing.gameObject
            : new GameObject("EP Gold Skill Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        cardObject.transform.SetParent(canvasTransform, false);

        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        cardRect.anchorMin = cardRect.anchorMax = Vector2.zero;
        cardRect.pivot = Vector2.zero;
        cardRect.anchoredPosition = new Vector2(108f, 191f);
        cardRect.sizeDelta = new Vector2(104f, 138f);

        Image background = cardObject.GetComponent<Image>();
        BattleUITheme.DecoratePanel(background);
        background.color = new Color(1f, 0.78f, 0.42f, 1f);
        background.raycastTarget = true;
        var outline = cardObject.GetComponent<Outline>();
        if (outline == null) outline = cardObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.76f, 0.24f, 1f);
        outline.effectDistance = new Vector2(3f, -3f);

        Button button = cardObject.GetComponent<Button>();
        button.targetGraphic = background;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.16f, 1.12f, 0.85f, 1f);
        colors.pressedColor = new Color(0.76f, 0.62f, 0.32f, 1f);
        colors.disabledColor = new Color(0.58f, 0.52f, 0.39f, 0.82f);
        button.colors = colors;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            if (QuestionUI.IsQuestionActive || BattleTutorialGuide.BlocksInput || EncounterBattleReturn.BattleEnded ||
                ActionSystem.Instance == null || ActionSystem.Instance.IsPerforming || currentEp < MaxEp)
                return;
            ActionSystem.Instance.Perform(new StarfallSkillGA());
        });

        CreateEpCardText(cardRect, "EP Skill Title", "STARFALL", new Vector2(0f, -7f), new Vector2(96f, 22f),
            TextAlignmentOptions.Center, 15f, new Color(0.16f, 0.09f, 0.025f, 1f), topAnchored: true);
        Image skillIcon = GetOrCreateImage(cardRect, "EP Skill Star Icon", epStarFillSprite);
        PositionEpStarImage(skillIcon.rectTransform, new Vector2(52f, 68f), new Vector2(39f, 39f));
        skillIcon.color = Color.white;
        CreateEpCardText(cardRect, "EP Skill Description", "ALL ENEMIES\n2x ATK", new Vector2(0f, 25f), new Vector2(96f, 34f),
            TextAlignmentOptions.Center, 12f, new Color(0.18f, 0.11f, 0.045f, 1f));
        epSkillChargeText = CreateEpCardText(cardRect, "EP Skill Charge", "CHARGE 0/5", new Vector2(0f, 4f), new Vector2(100f, 18f),
            TextAlignmentOptions.Center, 10f, new Color(0.14f, 0.08f, 0.03f, 1f));

        epSkillButton = button;
        button.transform.SetAsLastSibling();
    }

    private static TMP_Text CreateEpCardText(RectTransform parent, string name, string value, Vector2 position,
        Vector2 size, TextAlignmentOptions alignment, float fontSize, Color color, bool topAnchored = false)
    {
        Transform existing = parent.Find(name);
        TextMeshProUGUI text;
        if (existing != null)
        {
            text = existing.GetComponent<TextMeshProUGUI>();
            if (text == null) text = existing.gameObject.AddComponent<TextMeshProUGUI>();
        }
        else
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            text = textObject.GetComponent<TextMeshProUGUI>();
        }

        text.text = value;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(8f, fontSize - 2f);
        text.fontSizeMax = fontSize;
        text.raycastTarget = false;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = topAnchored ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
        rect.pivot = topAnchored ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return text;
    }

    private static Image GetOrCreateImage(RectTransform parent, string objectName, Sprite sprite)
    {
        Transform existing = parent.Find(objectName);
        Image image;
        if (existing != null)
        {
            image = existing.GetComponent<Image>();
            if (image == null) image = existing.gameObject.AddComponent<Image>();
        }
        else
        {
            var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            image = imageObject.GetComponent<Image>();
        }

        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static void PositionEpStarImage(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void EnsureEpStarSprites()
    {
        if (epStarEmptySprite == null)
            epStarEmptySprite = CreateEpStarSprite("EP Empty Star", outlineOnly: true);
        if (epStarFillSprite == null)
            epStarFillSprite = CreateEpStarSprite("EP Blue Energy Fill", outlineOnly: false);
    }

    private static Sprite CreateEpStarSprite(string spriteName, bool outlineOnly)
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = spriteName + " Texture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        Vector2[] vertices = MakeEpStarVertices(size, outlineOnly ? 58f : 51f, outlineOnly ? 28f : 24f);
        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                float edgeDistance = DistanceToPolygon(point, vertices);
                bool inside = IsInsidePolygon(point, vertices);
                Color32 color = new Color32(0, 0, 0, 0);

                if (outlineOnly)
                {
                    if (inside && edgeDistance <= 4.5f)
                        color = new Color32(120, 225, 255, 255);
                    else if (inside)
                        color = new Color32(8, 27, 52, 220);
                    else if (edgeDistance < 11f)
                    {
                        byte alpha = (byte)Mathf.RoundToInt(65f * (1f - edgeDistance / 11f));
                        color = new Color32(32, 157, 255, alpha);
                    }
                }
                else if (inside)
                {
                    float vertical = Mathf.Clamp01(point.y / size);
                    color = new Color32(
                        (byte)Mathf.RoundToInt(Mathf.Lerp(13f, 26f, vertical)),
                        (byte)Mathf.RoundToInt(Mathf.Lerp(94f, 195f, vertical)),
                        (byte)Mathf.RoundToInt(Mathf.Lerp(220f, 255f, vertical)),
                        255);
                }

                pixels[y * size + x] = color;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Vector2[] MakeEpStarVertices(int size, float outerRadius, float innerRadius)
    {
        var vertices = new Vector2[10];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        for (int i = 0; i < vertices.Length; i++)
        {
            float angle = Mathf.Deg2Rad * (-90f + i * 36f);
            float radius = i % 2 == 0 ? outerRadius : innerRadius;
            vertices[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        return vertices;
    }

    private static bool IsInsidePolygon(Vector2 point, Vector2[] vertices)
    {
        bool inside = false;
        for (int i = 0, j = vertices.Length - 1; i < vertices.Length; j = i++)
        {
            Vector2 a = vertices[i];
            Vector2 b = vertices[j];
            bool crosses = (a.y > point.y) != (b.y > point.y) &&
                point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x;
            if (crosses) inside = !inside;
        }
        return inside;
    }

    private static float DistanceToPolygon(Vector2 point, Vector2[] vertices)
    {
        float minDistance = float.MaxValue;
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector2 a = vertices[i];
            Vector2 b = vertices[(i + 1) % vertices.Length];
            Vector2 edge = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(point - a, edge) / edge.sqrMagnitude);
            minDistance = Mathf.Min(minDistance, Vector2.Distance(point, a + edge * t));
        }
        return minDistance;
    }

    private static void RemoveCoinBalance(RectTransform hudRect)
    {
        foreach (string childName in new[] { "BattleCoinIcon", "BattleCoinCount" })
        {
            var child = hudRect.Find(childName);
            if (child != null) Object.Destroy(child.gameObject);
        }
    }

    private static void ApplyPlayerStatusIcons(RectTransform hudRect, HeroView hero)
    {
        if (hero == null || hero.StatusEffectsUI == null) return;
        // This replaces the old crossed-swords badge beneath the mana bubbles.
        hero.StatusEffectsUI.MoveToHud(hudRect, new Vector2(96, -78), new Vector2(220, 32));
    }
}

public static class BattleUITheme
{
    private static readonly Color Brass = new Color(0.75f, 0.55f, 0.25f);
    private static readonly Color Parchment = new Color(0.95f, 0.87f, 0.7f);
    private static readonly Color Wood = new Color(0.2f, 0.12f, 0.065f);
    private static Sprite panelSprite;

    public static void DecoratePanel(UnityEngine.UI.Image image)
    {
        if (image == null) return;
        if (panelSprite == null)
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            var pixels = new Color[32 * 32];
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, 31 - x), Mathf.Min(y, 31 - y));
                    pixels[y * 32 + x] = edge < 1 ? new Color(0.07f, 0.04f, 0.02f) :
                        edge < 3 ? Brass : edge < 5 ? new Color(0.35f, 0.22f, 0.1f) : Wood;
                }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            panelSprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f),
                100, 0, SpriteMeshType.FullRect, new Vector4(6, 6, 6, 6));
        }
        image.sprite = panelSprite;
        image.type = UnityEngine.UI.Image.Type.Sliced;
        image.color = Color.white;
    }

    public static void DrawResultPanel(Rect rect, string text)
    {
        Color previous = GUI.color;
        GUI.color = Brass;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Wood;
        GUI.DrawTexture(new Rect(rect.x + 4, rect.y + 4, rect.width - 8, rect.height - 8), Texture2D.whiteTexture);
        GUI.color = previous;
        var style = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        style.normal.textColor = Parchment;
        GUI.Label(new Rect(rect.x + 15, rect.y + 12, rect.width - 30, rect.height - 24), text, style);
    }

    public static bool DrawRetreatButton(Rect rect)
    {
        Color previousBackground = GUI.backgroundColor;
        Color previousContent = GUI.contentColor;
        GUI.backgroundColor = Wood;
        GUI.contentColor = Parchment;
        bool clicked = GUI.Button(rect, "Retreat to the cave");
        GUI.backgroundColor = previousBackground;
        GUI.contentColor = previousContent;
        return clicked;
    }

    public static void Apply()
    {
        foreach (var button in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var image = button.targetGraphic as UnityEngine.UI.Image;
            if (image != null)
            {
                DecoratePanel(image);
                var outline = image.GetComponent<UnityEngine.UI.Outline>();
                if (outline == null) outline = image.gameObject.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = Brass;
                outline.effectDistance = new Vector2(2, -2);
            }
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.12f, 1.02f);
            colors.pressedColor = new Color(0.7f, 0.65f, 0.55f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.2f, 0.18f, 0.15f, 0.6f);
            button.colors = colors;
            foreach (var text in button.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                if (text.text.Trim().Equals("End Turn", System.StringComparison.OrdinalIgnoreCase) &&
                    button.GetComponent<BattleEndTurnState>() == null)
                    button.gameObject.AddComponent<BattleEndTurnState>();
                text.color = Parchment;
                text.enableAutoSizing = true;
                text.fontSizeMin = 16;
                text.fontSizeMax = 27;
                text.fontStyle = TMPro.FontStyles.Bold;
                text.alignment = TMPro.TextAlignmentOptions.Center;
                text.margin = new Vector4(12, 5, 12, 5);
            }
        }

        foreach (var question in Object.FindObjectsByType<QuestionUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            foreach (var image in question.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                if (image.GetComponentInParent<UnityEngine.UI.Button>() != null) continue;
                if (image.name == "Background" || image.gameObject == question.gameObject)
                {
                    DecoratePanel(image);
                    var outline = image.GetComponent<UnityEngine.UI.Outline>();
                    if (outline == null) outline = image.gameObject.AddComponent<UnityEngine.UI.Outline>();
                    outline.effectColor = Brass;
                    outline.effectDistance = new Vector2(3, -3);
                }
            }
            foreach (var text in question.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                text.color = Parchment;
                text.enableAutoSizing = true;
                text.fontSizeMin = 17;
                text.fontSizeMax = text.name.Contains("Snippet") ? 26 : 32;
                text.margin = new Vector4(12, 8, 12, 8);
            }
        }
        foreach (var slider in Object.FindObjectsByType<UnityEngine.UI.Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (slider.fillRect == null) continue;
            var image = slider.fillRect.GetComponent<UnityEngine.UI.Image>();
            if (image != null) image.color = slider.name.ToLowerInvariant().Contains("timer") ? Brass : new Color(0.7f, 0.1f, 0.075f);
        }
        foreach (var text in Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (text.name.Contains("Health") || text.name.Contains("Mana") || text.name.Contains("Attack"))
            { text.color = Parchment; text.fontStyle = TMPro.FontStyles.Bold; }
        foreach (var image in Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (image.name.Contains("Frame")) image.color = Brass;
    }
}

public class BattleEndTurnState : MonoBehaviour
{
    private UnityEngine.UI.Button button;
    private TMPro.TMP_Text label;
    private QuestionUI question;

    void Awake()
    {
        button = GetComponent<UnityEngine.UI.Button>();
        label = GetComponentInChildren<TMPro.TMP_Text>(true);
        question = Object.FindFirstObjectByType<QuestionUI>(FindObjectsInactive.Include);
    }

    void Update()
    {
        bool resolving = ActionSystem.Instance != null && ActionSystem.Instance.IsPerforming;
        bool answering = question != null && question.isActiveAndEnabled;
        button.interactable = !resolving && !answering && !BattleTutorialGuide.BlocksInput && !EncounterBattleReturn.BattleEnded;
        if (label != null) label.text = EncounterBattleReturn.BattleEnded ? "Battle Complete" :
            answering ? "Answer Question" : resolving ? "Resolving..." : "End Turn";
    }
}

public class EncounterBattleReturn : MonoBehaviour
{
    public static bool BattleEnded { get; private set; }
    private bool returning;
    private bool caveBattle;
    private bool hadEnemies;
    private bool victory;
    private string encounterId;

    public void Configure(bool fromCave, string id)
    {
        caveBattle = fromCave;
        encounterId = id;
        hadEnemies = EnemySystem.Instance.Enemies.Count > 0;
        BattleEnded = false;
    }

    void Update()
    {
        if (returning || BattleEnded || !hadEnemies || EnemySystem.Instance == null ||
            ActionSystem.Instance == null || ActionSystem.Instance.IsPerforming) return;
        bool heroDefeated = HeroSystem.Instance != null && HeroSystem.Instance.HeroView.CurrentHealth <= 0;
        if (EnemySystem.Instance.Enemies.Count == 0 || heroDefeated)
        {
            victory = !heroDefeated && EnemySystem.Instance.Enemies.Count == 0;
            BattleEnded = true;
            BattleAudio.Play(victory ? BattleSound.Victory : BattleSound.Defeat);
            if (CardViewHoverSystem.Instance != null) CardViewHoverSystem.Instance.Hide();
            if (victory && caveBattle && !string.IsNullOrEmpty(encounterId))
            {
                var saved = SaveSlots.Read(SaveSlots.Active);
                if (saved != null)
                {
                    if (saved.defeatedEncounters == null) saved.defeatedEncounters = new List<string>();
                    if (!saved.defeatedEncounters.Contains(encounterId)) saved.defeatedEncounters.Add(encounterId);
                    SaveSlots.Write(saved);
                }
            }
            StartCoroutine(EndBattle());
        }
    }

    IEnumerator EndBattle()
    {
        yield return BattleTutorialGuide.ExplainResult(victory);
        yield return new WaitForSecondsRealtime(1.8f);
        Return();
    }

    void Return()
    {
        if (returning) return;
        returning = true;
        Time.timeScale = 1f;
        var saved = SaveSlots.Read(SaveSlots.Active);
        if (caveBattle && saved != null && Application.CanStreamedLevelBeLoaded(saved.scene))
            GameSave.EnsureInstance().ContinueGame();
        else SceneManager.LoadScene("MainMenu");
    }

    public void ReturnToLobby() => Return();

    void OnGUI()
    {
        if (GameControlsGuide.GameplayOverlayHidden) return;
        if (returning) return;
        if (BattleEnded)
        {
            BattleUITheme.DrawResultPanel(new Rect(Screen.width / 2f - 240, Screen.height / 2f - 65, 480, 130),
                victory ? "VICTORY\nReturning from battle..." : "DEFEAT\nReturning from battle...");
        }
        else if (caveBattle && !QuestionUI.IsQuestionActive)
        {
            bool previousEnabled = GUI.enabled;
            GUI.enabled = ActionSystem.Instance != null && !ActionSystem.Instance.IsPerforming;
            if (BattleUITheme.DrawRetreatButton(new Rect(20, 20, 230, 42))) Return();
            GUI.enabled = previousEnabled;
        }
    }

    void OnDestroy() { BattleEnded = false; }
}

/// <summary>Activates the five-star EP special skill from the battle HUD.</summary>
public sealed class StarfallSkillGA : GameAction { }
