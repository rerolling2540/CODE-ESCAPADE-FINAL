using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Battle codex opened from the player portrait.</summary>
public sealed class BattleInfoPanel : MonoBehaviour
{
    [Header("Panel")]
    public CanvasGroup panelGroup;
    public Button backdropButton;
    public Button closeButton;
    public Button[] tabButtons = new Button[3];
    public Image[] tabBackgrounds = new Image[3];
    public TMP_Text pageHeading;
    public TMP_Text bodyText;
    public ScrollRect bodyScroll;
    public RectTransform bodyContent;
    public RectTransform bodyViewport;
    public RectTransform cardGridContent;
    public CardInventoryItemUI cardItemTemplate;
    public TMP_Text cardEmptyState;
    public GameObject portraitContainer;
    public Image characterPortrait;

    [Header("Enemy Detail Icons")]
    public Sprite enemyHealthIcon;
    public Sprite enemyAttackIcon;
    public Sprite enemyDefenseIcon;
    public Sprite enemyRangedIcon;
    public Sprite enemyRewardIcon;
    public Sprite enemyBurnIcon;
    public Sprite characterDeckIcon;

    private float previousTimeScale = 1f;
    private int selectedTab;
    private Texture2D portraitTexture;
    private Sprite runtimePortraitSprite;
    private bool renderingCards;
    private static readonly Color ActiveTab = new Color(0.42f, 0.25f, 0.11f, 1f);
    private static readonly Color InactiveTab = new Color(0.25f, 0.15f, 0.08f, 1f);
    private static readonly Color Ink = new Color(0.16f, 0.085f, 0.035f, 1f);

    private void Awake()
    {
        if (panelGroup == null) panelGroup = GetComponent<CanvasGroup>();
        // Preserve the invisible backdrop's raycast so clicking outside the
        // window still closes the codex, without tinting the whole battle brown.
        if (backdropButton != null) backdropButton.transition = Selectable.Transition.None;
        if (backdropButton != null && backdropButton.targetGraphic != null)
        {
            Color backdropColor = backdropButton.targetGraphic.color;
            backdropColor.a = 0f;
            backdropButton.targetGraphic.color = backdropColor;
        }
        if (backdropButton != null) backdropButton.onClick.AddListener(Close);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int tab = i;
            if (tabButtons[i] != null) tabButtons[i].onClick.AddListener(() => ShowTab(tab));
        }
    }

    private void OnEnable()
    {
        CharacterInventory.InventoryChanged += HandleInventoryChanged;
    }

    private void OnDisable()
    {
        CharacterInventory.InventoryChanged -= HandleInventoryChanged;
    }

    private void HandleInventoryChanged()
    {
        if (selectedTab == 1 && gameObject.activeInHierarchy)
            RenderCards();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    public void ToggleFromPortrait()
    {
        if (gameObject.activeSelf)
        {
            Close();
            return;
        }

        if (GameControlsGuide.SettingsOpen || QuestionUI.IsQuestionActive || Time.timeScale <= 0f)
            return;

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        gameObject.SetActive(true);
        ShowTab(0);
    }

    public void Close()
    {
        if (!gameObject.activeSelf) return;
        gameObject.SetActive(false);
        Time.timeScale = previousTimeScale;
    }

    public void ShowTab(int tab)
    {
        tab = Mathf.Clamp(tab, 0, 2);
        selectedTab = tab;
        for (int i = 0; i < tabBackgrounds.Length; i++)
            if (tabBackgrounds[i] != null)
                tabBackgrounds[i].color = i == tab ? ActiveTab : InactiveTab;

        bool showPortrait = tab == 0 || tab == 2;
        if (portraitContainer != null) portraitContainer.SetActive(showPortrait);
        bool showCards = tab == 1;
        if (bodyContent != null) bodyContent.gameObject.SetActive(!showCards);
        if (cardGridContent != null) cardGridContent.gameObject.SetActive(showCards);
        if (!showCards && cardEmptyState != null) cardEmptyState.gameObject.SetActive(false);
        if (bodyViewport != null)
        {
            bodyViewport.anchoredPosition = showPortrait ? new Vector2(550f, -174f) : new Vector2(298f, -174f);
            bodyViewport.sizeDelta = showPortrait ? new Vector2(680f, 490f) : new Vector2(940f, 490f);
        }
        if (bodyScroll != null)
            bodyScroll.content = tab == 1 && cardGridContent != null ? cardGridContent : bodyContent;

        switch (tab)
        {
            case 0:
                pageHeading.text = "CHARACTER INFORMATION";
                RenderCharacterInfo();
                break;
            case 1:
                pageHeading.text = "CARD INVENTORY";
                RenderCards();
                break;
            default:
                pageHeading.text = "ENEMY DETAILS";
                RenderEnemies();
                break;
        }

        if (tab == 1) ResizeCardGridContent();
        else if (tab == 0 || tab == 2)
        {
            string listName = tab == 0 ? "Character Details List" : "Enemy Details List";
            RectTransform infoList = bodyContent != null ? bodyContent.Find(listName) as RectTransform : null;
            if (infoList != null) ResizeInfoListContent(infoList);
            else ResizeTextContent();
        }
        else ResizeTextContent();
    }

    private void RenderCharacterInfo()
    {
        ClearEnemyDetailsContent();
        if (bodyText != null)
        {
            bodyText.text = string.Empty;
            bodyText.gameObject.SetActive(false);
        }
        CharacterInfoData info = Resources.Load<CharacterInfoData>("UI/CharacterInfo");
        int index = Mathf.Clamp(PlayerPrefs.GetInt("SelectedCharacter", 0), 0, 2);
        HeroData hero = ResolveSelectedHero(info, index);
        if (characterPortrait != null)
        {
            Texture2D texture = info != null && info.portraits != null && index < info.portraits.Length
                ? info.portraits[index]
                : null;
            if (texture != null)
            {
                if (portraitTexture != texture || runtimePortraitSprite == null)
                {
                    if (runtimePortraitSprite != null) Destroy(runtimePortraitSprite);
                    portraitTexture = texture;
                    runtimePortraitSprite = Sprite.Create(texture,
                        new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }
                characterPortrait.sprite = runtimePortraitSprite;
            }
            else
            {
                characterPortrait.sprite = hero != null ? hero.Image : null;
            }
            characterPortrait.preserveAspect = true;
            characterPortrait.color = characterPortrait.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        string[] names = { "Warrior", "Archer", "Mage" };
        string name = names[index];
        PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
        HeroClassData heroClass = playerStats != null && playerStats.HeroClass != null
            ? playerStats.HeroClass
            : info != null ? info.battleClass : null;
        int level = PlayerProgression.Level;
        int health = PlayerHealth.CurrentHealth;
        int maxHealth = PlayerHealth.MaxHealth;
        string language = CharacterAlmanac.LanguageName(CharacterAlmanac.LanguageForCharacter(index));

        if (bodyContent == null) return;

        RectTransform list = CreateEnemyDetailsList("Character Details List");
        TMP_Text heading = CreateEnemyText("Character Name and Class", list,
            "<size=29><b>" + name.ToUpperInvariant() + "</b></size>\n<size=17><color=#80602B>" +
            EscapeRichText(language) + "  ·  LEVEL " + level + "</color></size>", 24f, FontStyles.Normal);
        heading.alignment = TextAlignmentOptions.MidlineLeft;
        heading.enableWordWrapping = false;
        heading.gameObject.AddComponent<LayoutElement>().minHeight =
            heading.gameObject.GetComponent<LayoutElement>().preferredHeight = 66f;

        CreateInfoHeading(list, "PROGRESSION");
        CreateEnemyStatsGrid(list, new List<EnemyDetailStat>
        {
            new EnemyDetailStat("EXPERIENCE", PlayerProgression.Experience + " / " +
                PlayerProgression.RequiredExperience(level), enemyRewardIcon, "XP", new Color(0.75f, 0.57f, 0.12f))
        });

        CreateInfoHeading(list, "BATTLE STATS");
        var stats = new List<EnemyDetailStat>
        {
            new EnemyDetailStat("HEALTH", health + " / " + maxHealth, enemyHealthIcon, "HP", new Color(0.78f, 0.20f, 0.22f))
        };
        if (heroClass != null)
        {
            stats.Add(new EnemyDetailStat("ATTACK POWER", heroClass.GetAttackPowerForLevel(level).ToString(),
                enemyAttackIcon, "ATK", new Color(0.78f, 0.36f, 0.15f)));
            stats.Add(new EnemyDetailStat("ATTACK BONUS", heroClass.GetAttackBonusForLevel(level).ToString(),
                enemyRangedIcon, "BONUS", new Color(0.64f, 0.43f, 0.18f)));
            stats.Add(new EnemyDetailStat("DEFENSE", heroClass.GetDefenseForLevel(level).ToString(),
                enemyDefenseIcon, "DEF", new Color(0.25f, 0.48f, 0.76f)));
        }
        stats.Add(new EnemyDetailStat("STARTING DECK", (hero != null && hero.Deck != null ? hero.Deck.Count : 0) + " cards",
            characterDeckIcon, "DECK", new Color(0.54f, 0.37f, 0.16f)));
        CreateEnemyStatsGrid(list, stats);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(list);
        ResizeInfoListContent(list);
    }

    private void CreateInfoHeading(Transform parent, string label)
    {
        TMP_Text heading = CreateEnemyText("Section - " + label, parent,
            "<size=21><b>" + label + "</b></size>", 21f, FontStyles.Bold);
        heading.alignment = TextAlignmentOptions.MidlineLeft;
        heading.enableWordWrapping = false;
        LayoutElement layout = heading.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = layout.preferredHeight = 30f;
    }

    private static HeroData ResolveSelectedHero(CharacterInfoData info, int index)
    {
        HeroData fallback = info != null && info.heroes != null && index < info.heroes.Length
            ? info.heroes[index]
            : null;
        return SelectedHero.Resolve(fallback);
    }

    private void RenderCards()
    {
        if (renderingCards) return;
        renderingCards = true;
        try
        {
            ClearEnemyDetailsContent();
            if (bodyText != null) bodyText.gameObject.SetActive(true);
            if (bodyText != null) bodyText.text = string.Empty;
            if (cardGridContent == null || cardItemTemplate == null)
            {
                if (cardGridContent != null) cardGridContent.gameObject.SetActive(false);
                if (bodyContent != null) bodyContent.gameObject.SetActive(true);
                if (bodyScroll != null) bodyScroll.content = bodyContent;
                if (bodyText != null)
                    bodyText.text = "Card inventory display is unavailable. Reopen or repair the Battle Codex card grid.";
                return;
            }

            for (int i = cardGridContent.childCount - 1; i >= 0; i--)
                Destroy(cardGridContent.GetChild(i).gameObject);

            int renderedCards = 0;
            CharacterInfoData info = Resources.Load<CharacterInfoData>("UI/CharacterInfo");
            int selectedIndex = Mathf.Clamp(PlayerPrefs.GetInt("SelectedCharacter", 0), 0, 2);
            HeroData hero = ResolveSelectedHero(info, selectedIndex);
            List<CardData> ownedCards = CharacterInventory.WithChestRewards(hero != null ? hero.Deck : null);
            var quantities = new Dictionary<CardData, int>();
            if (ownedCards != null)
            {
                foreach (CardData card in ownedCards)
                {
                    if (card == null) continue;
                    quantities[card] = quantities.TryGetValue(card, out int count) ? count + 1 : 1;
                }
            }

            var sortedCards = new List<CardData>(quantities.Keys);
            sortedCards.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            foreach (CardData card in sortedCards)
            {
                AddCardItem(card, quantities[card], CharacterInventory.SavedCardLevel(card));
                renderedCards++;
            }

            if (cardEmptyState != null)
            {
                cardEmptyState.gameObject.SetActive(renderedCards == 0);
                cardEmptyState.text = hero == null
                    ? "This character's card data could not be loaded."
                    : "This player's inventory has no available cards.";
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardGridContent);
        }
        finally
        {
            renderingCards = false;
        }
    }

    private void AddCardItem(CardData card, int quantity, int level)
    {
        CardInventoryItemUI item = Instantiate(cardItemTemplate, cardGridContent, false);
        item.gameObject.name = "Owned Card - " + card.name;
        item.gameObject.SetActive(true);
        item.Bind(card, quantity, level);
    }

    private void RenderEnemies()
    {
        ClearEnemyDetailsContent();
        EnemySystem enemySystem = FindFirstObjectByType<EnemySystem>();
        var livingEnemies = new List<EnemyView>();
        if (enemySystem != null && enemySystem.Enemies != null)
        {
            foreach (EnemyView enemy in enemySystem.Enemies)
            {
                if (enemy == null || enemy.CurrentHealth <= 0) continue;
                livingEnemies.Add(enemy);
            }
        }

        if (characterPortrait != null)
        {
            Sprite enemyPortrait = livingEnemies.Count > 0 ? GetEnemyPortrait(livingEnemies[0]) : null;
            characterPortrait.sprite = enemyPortrait;
            characterPortrait.preserveAspect = true;
            characterPortrait.color = enemyPortrait != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        if (bodyText == null || bodyContent == null)
            return;

        if (livingEnemies.Count == 0)
        {
            bodyText.gameObject.SetActive(true);
            bodyText.text = "No living enemies are currently in this battle.";
            return;
        }

        bodyText.text = string.Empty;
        bodyText.gameObject.SetActive(false);
        RectTransform listRect = CreateEnemyDetailsList();
        for (int i = 0; i < livingEnemies.Count; i++)
            CreateEnemyDetailsSection(listRect, livingEnemies[i], i == 0);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(listRect);
        ResizeInfoListContent(listRect);
    }

    private RectTransform CreateEnemyDetailsList(string objectName = "Enemy Details List")
    {
        var listObject = new GameObject(objectName, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        listObject.transform.SetParent(bodyContent, false);
        var rect = (RectTransform)listObject.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(-28f, 0f);

        var layout = listObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 14, 14);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = listObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rect;
    }

    private void CreateEnemyDetailsSection(RectTransform list, EnemyView enemy, bool isPortraitEnemy)
    {
        string enemyName = enemy.Data != null ? enemy.Data.name : enemy.gameObject.name;
        var section = new GameObject("Enemy Details - " + enemyName, typeof(RectTransform), typeof(Image),
            typeof(VerticalLayoutGroup), typeof(LayoutElement));
        section.transform.SetParent(list, false);
        section.GetComponent<Image>().color = new Color(0.35f, 0.22f, 0.10f, 0.18f);
        section.GetComponent<Image>().raycastTarget = false;
        var sectionLayout = section.GetComponent<VerticalLayoutGroup>();
        sectionLayout.padding = new RectOffset(12, 12, 10, 10);
        sectionLayout.spacing = 8f;
        sectionLayout.childAlignment = TextAnchor.UpperLeft;
        sectionLayout.childControlWidth = true;
        sectionLayout.childControlHeight = true;
        sectionLayout.childForceExpandWidth = true;
        sectionLayout.childForceExpandHeight = false;

        var stats = new List<EnemyDetailStat>
        {
            new EnemyDetailStat("HEALTH", enemy.CurrentHealth + " / " + enemy.MaxHealth, enemyHealthIcon, "HP", new Color(0.78f, 0.20f, 0.22f)),
            new EnemyDetailStat("ATTACK", enemy.AttackPower.ToString(), enemyAttackIcon, "ATK", new Color(0.78f, 0.36f, 0.15f)),
            new EnemyDetailStat("DEFENSE", enemy.Defense.ToString(), enemyDefenseIcon, "DEF", new Color(0.25f, 0.48f, 0.76f)),
            new EnemyDetailStat("ATTACK STYLE", enemy.IsRanged ? "Ranged" : "Melee", enemy.IsRanged ? enemyRangedIcon : enemyAttackIcon,
                enemy.IsRanged ? "RNG" : "ATK", new Color(0.64f, 0.43f, 0.18f)),
            new EnemyDetailStat("XP REWARD", enemy.ExperienceReward + " XP", enemyRewardIcon, "XP", new Color(0.75f, 0.57f, 0.12f))
        };

        int armor = enemy.GetStatusEffectStacks(StatusEffectType.ARMOR);
        int burn = enemy.GetStatusEffectStacks(StatusEffectType.BURN);
        if (armor > 0)
            stats.Add(new EnemyDetailStat("ARMOR", armor.ToString(), enemyDefenseIcon, "DEF", new Color(0.25f, 0.48f, 0.76f)));
        if (burn > 0)
            stats.Add(new EnemyDetailStat("BURN", burn.ToString(), enemyBurnIcon, "FIRE", new Color(0.82f, 0.27f, 0.10f)));

        CreateEnemyHeader(section.transform, enemy, enemyName, isPortraitEnemy);
        CreateEnemyStatsGrid(section.transform, stats);

        var sectionElement = section.GetComponent<LayoutElement>();
        int rows = Mathf.CeilToInt(stats.Count / 2f);
        sectionElement.minHeight = 112f + rows * 42f;
        sectionElement.preferredHeight = 112f + rows * 42f;
    }

    private void CreateEnemyHeader(Transform parent, EnemyView enemy, string enemyName, bool isPortraitEnemy)
    {
        var header = new GameObject("Enemy Heading", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        header.transform.SetParent(parent, false);
        var headerLayout = header.GetComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 14f;
        headerLayout.childAlignment = TextAnchor.MiddleLeft;
        headerLayout.childControlWidth = false;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;
        header.GetComponent<LayoutElement>().preferredHeight = 70f;

        var portraitObject = new GameObject("Enemy Portrait", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        portraitObject.transform.SetParent(header.transform, false);
        ((RectTransform)portraitObject.transform).sizeDelta = new Vector2(70f, 70f);
        var portrait = portraitObject.GetComponent<Image>();
        portrait.sprite = GetEnemyPortrait(enemy);
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;
        var portraitLayout = portraitObject.GetComponent<LayoutElement>();
        portraitLayout.minWidth = portraitLayout.preferredWidth = 70f;
        portraitLayout.minHeight = portraitLayout.preferredHeight = 70f;

        string subtitle = enemy.IsRanged ? "RANGED ENEMY" : "MELEE ENEMY";
        if (isPortraitEnemy) subtitle += "  ·  PORTRAIT";
        TMP_Text heading = CreateEnemyText("Enemy Name", parent: header.transform,
            value: "<size=27><b>" + EscapeRichText(enemyName.ToUpperInvariant()) + "</b></size>\n<size=17><color=#80602B>" + subtitle + "</color></size>",
            fontSize: 23f, style: FontStyles.Normal);
        heading.alignment = TextAlignmentOptions.MidlineLeft;
        heading.enableWordWrapping = false;
        heading.rectTransform.sizeDelta = new Vector2(470f, 70f);
        var headingLayout = heading.gameObject.AddComponent<LayoutElement>();
        headingLayout.flexibleWidth = 1f;
        headingLayout.minHeight = headingLayout.preferredHeight = 70f;
    }

    private void CreateEnemyStatsGrid(Transform parent, List<EnemyDetailStat> stats)
    {
        var grid = new GameObject("Enemy Stats", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
        grid.transform.SetParent(parent, false);
        var gridLayout = grid.GetComponent<GridLayoutGroup>();
        gridLayout.padding = new RectOffset(0, 0, 0, 0);
        gridLayout.spacing = new Vector2(8f, 4f);
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.UpperLeft;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 2;
        float viewportWidth = bodyViewport != null ? bodyViewport.rect.width : 680f;
        gridLayout.cellSize = new Vector2(Mathf.Max(190f, (viewportWidth - 102f) * 0.5f), 38f);
        grid.GetComponent<LayoutElement>().preferredHeight = Mathf.CeilToInt(stats.Count / 2f) * 42f;

        foreach (EnemyDetailStat stat in stats)
            CreateEnemyStatRow(grid.transform, stat);
    }

    private void CreateEnemyStatRow(Transform parent, EnemyDetailStat stat)
    {
        var row = new GameObject("Stat - " + stat.label, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);
        var layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var iconObject = new GameObject("Icon - " + stat.label, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        iconObject.transform.SetParent(row.transform, false);
        ((RectTransform)iconObject.transform).sizeDelta = new Vector2(30f, 30f);
        var icon = iconObject.GetComponent<Image>();
        icon.sprite = stat.icon;
        icon.preserveAspect = true;
        icon.color = stat.icon != null ? Color.white : stat.fallbackColor;
        icon.raycastTarget = false;
        var iconLayout = iconObject.GetComponent<LayoutElement>();
        iconLayout.minWidth = iconLayout.preferredWidth = 30f;
        iconLayout.minHeight = iconLayout.preferredHeight = 30f;
        if (stat.icon == null)
        {
            TMP_Text fallback = CreateEnemyText("Icon Lettering", iconObject.transform, stat.fallbackLabel, 10f, FontStyles.Bold);
            fallback.alignment = TextAlignmentOptions.Center;
            fallback.color = Color.white;
            RectTransform fallbackRect = fallback.rectTransform;
            fallbackRect.anchorMin = Vector2.zero;
            fallbackRect.anchorMax = Vector2.one;
            fallbackRect.offsetMin = fallbackRect.offsetMax = Vector2.zero;
        }

        TMP_Text label = CreateEnemyText("Stat Value", row.transform,
            "<b>" + stat.label + "</b>  " + EscapeRichText(stat.value), 17f, FontStyles.Normal);
        float labelWidth = bodyViewport != null ? Mathf.Max(140f, (bodyViewport.rect.width - 102f) * 0.5f - 44f) : 245f;
        label.rectTransform.sizeDelta = new Vector2(labelWidth, 34f);
        label.enableAutoSizing = true;
        label.fontSizeMin = 13f;
        label.fontSizeMax = 17f;
        label.enableWordWrapping = false;
        var labelLayout = label.gameObject.AddComponent<LayoutElement>();
        labelLayout.flexibleWidth = 1f;
        labelLayout.minHeight = labelLayout.preferredHeight = 34f;
    }

    private static TMP_Text CreateEnemyText(string objectName, Transform parent, string value, float fontSize, FontStyles style)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Ink;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private void ClearEnemyDetailsContent()
    {
        if (bodyContent == null) return;
        for (int i = bodyContent.childCount - 1; i >= 0; i--)
        {
            Transform child = bodyContent.GetChild(i);
            if (bodyText != null && child == bodyText.transform) continue;
            Destroy(child.gameObject);
        }
    }

    private void ResizeInfoListContent(RectTransform enemyList)
    {
        if (bodyContent == null || bodyViewport == null || enemyList == null) return;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(enemyList);
        float height = Mathf.Max(bodyViewport.rect.height, LayoutUtility.GetPreferredHeight(enemyList) + 20f);
        bodyContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        if (bodyScroll != null) bodyScroll.verticalNormalizedPosition = 1f;
    }

    private static string EscapeRichText(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    private static Sprite GetEnemyPortrait(EnemyView enemy)
    {
        if (enemy == null) return null;
        if (enemy.Data != null && enemy.Data.Image != null) return enemy.Data.Image;
        return enemy.MainSpriteRenderer != null ? enemy.MainSpriteRenderer.sprite : null;
    }

    private readonly struct EnemyDetailStat
    {
        public readonly string label;
        public readonly string value;
        public readonly Sprite icon;
        public readonly string fallbackLabel;
        public readonly Color fallbackColor;

        public EnemyDetailStat(string label, string value, Sprite icon, string fallbackLabel, Color fallbackColor)
        {
            this.label = label;
            this.value = value;
            this.icon = icon;
            this.fallbackLabel = fallbackLabel;
            this.fallbackColor = fallbackColor;
        }
    }

    private void ResizeTextContent()
    {
        if (bodyText == null || bodyContent == null || bodyViewport == null) return;
        Canvas.ForceUpdateCanvases();
        float width = Mathf.Max(100f, bodyViewport.rect.width - 52f);
        float height = bodyText.GetPreferredValues(bodyText.text, width, 0f).y + 48f;
        bodyContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(bodyViewport.rect.height, height));
        bodyText.rectTransform.sizeDelta = new Vector2(-52f, 0f);
        if (bodyScroll != null) bodyScroll.verticalNormalizedPosition = 1f;
    }

    private void ResizeCardGridContent()
    {
        if (cardGridContent == null || bodyViewport == null) return;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(cardGridContent);
        if (bodyScroll != null) bodyScroll.verticalNormalizedPosition = 1f;
    }

    private void OnDestroy()
    {
        if (runtimePortraitSprite != null) Destroy(runtimePortraitSprite);
    }
}
