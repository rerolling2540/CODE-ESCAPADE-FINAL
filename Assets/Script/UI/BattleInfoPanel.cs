using System;
using System.Text;
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

    private float previousTimeScale = 1f;
    private int selectedTab;
    private Texture2D portraitTexture;
    private Sprite runtimePortraitSprite;
    private static readonly Color ActiveTab = new Color(0.42f, 0.25f, 0.11f, 1f);
    private static readonly Color InactiveTab = new Color(0.25f, 0.15f, 0.08f, 1f);
    private static readonly Color Ink = new Color(0.16f, 0.085f, 0.035f, 1f);

    private void Awake()
    {
        if (panelGroup == null) panelGroup = GetComponent<CanvasGroup>();
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

        bool showPortrait = tab == 0;
        if (portraitContainer != null) portraitContainer.SetActive(showPortrait);
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
        else ResizeTextContent();
    }

    private void RenderCharacterInfo()
    {
        CharacterInfoData info = Resources.Load<CharacterInfoData>("UI/CharacterInfo");
        int index = Mathf.Clamp(PlayerPrefs.GetInt("SelectedCharacter", 0), 0, 2);
        HeroData hero = info != null && info.heroes != null && index < info.heroes.Length
            ? info.heroes[index]
            : null;
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
        }

        string[] names = { "Warrior", "Archer", "Mage" };
        string name = names[index];
        PlayerStats stats = FindFirstObjectByType<PlayerStats>();
        HeroClassData heroClass = stats != null && stats.HeroClass != null
            ? stats.HeroClass
            : info != null ? info.battleClass : null;
        int level = PlayerProgression.Level;
        int health = PlayerHealth.CurrentHealth;
        int maxHealth = PlayerHealth.MaxHealth;
        string language = CharacterAlmanac.LanguageName(CharacterAlmanac.LanguageForCharacter(index));

        var text = new StringBuilder();
        text.AppendLine(name.ToUpperInvariant() + "  ·  " + language);
        text.AppendLine("Level " + level);
        text.AppendLine();
        text.AppendLine("PROGRESSION");
        text.AppendLine("Experience: " + PlayerProgression.Experience + " / " + PlayerProgression.RequiredExperience(level));
        text.AppendLine();
        text.AppendLine("BATTLE STATS");
        text.AppendLine("Health: " + health + " / " + maxHealth);
        if (heroClass != null)
        {
            text.AppendLine("Attack power: " + heroClass.GetAttackPowerForLevel(level));
            text.AppendLine("Attack bonus: " + heroClass.GetAttackBonusForLevel(level));
            text.AppendLine("Defense: " + heroClass.GetDefenseForLevel(level));
        }
        text.AppendLine("Starting deck: " + (hero != null && hero.Deck != null ? hero.Deck.Count : 0) + " cards");
        bodyText.text = text.ToString();
    }

    private void RenderCards()
    {
        if (bodyText != null) bodyText.text = string.Empty;
        if (cardGridContent == null || cardItemTemplate == null)
        {
            if (bodyText != null)
                bodyText.text = "Card inventory display is unavailable. Reopen or repair the Battle Codex card grid.";
            return;
        }

        for (int i = cardGridContent.childCount - 1; i >= 0; i--)
            Destroy(cardGridContent.GetChild(i).gameObject);

        int renderedCards = 0;
        CharacterInventory inventory = FindFirstObjectByType<CharacterInventory>();
        if (inventory != null)
        {
            if (inventory.Cards != null)
            {
                foreach (CharacterInventory.Stack stack in inventory.Cards)
                {
                    if (stack == null || stack.quantity <= 0) continue;
                    CardData card = inventory.GetCard(stack.id);
                    if (card == null) continue;
                    AddCardItem(card, stack.quantity, inventory.GetLevel(stack.id));
                    renderedCards++;
                }
            }
        }

        if (cardEmptyState != null)
        {
            cardEmptyState.gameObject.SetActive(renderedCards == 0);
            cardEmptyState.text = inventory == null
                ? "Player inventory is not loaded."
                : "This player's inventory has no available cards.";
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(cardGridContent);
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
        var text = new StringBuilder();
        EnemySystem enemySystem = FindFirstObjectByType<EnemySystem>();
        int count = 0;
        if (enemySystem != null && enemySystem.Enemies != null)
        {
            foreach (EnemyView enemy in enemySystem.Enemies)
            {
                if (enemy == null || enemy.CurrentHealth <= 0) continue;
                count++;
                string enemyName = enemy.Data != null ? enemy.Data.name : enemy.gameObject.name;
                text.AppendLine(enemyName.ToUpperInvariant());
                text.AppendLine("Health: " + enemy.CurrentHealth + " / " + enemy.MaxHealth);
                text.AppendLine("Attack: " + enemy.AttackPower);
                text.AppendLine("Defense: " + enemy.Defense);
                text.AppendLine("Attack style: " + (enemy.IsRanged ? "Ranged" : "Melee"));
                text.AppendLine("Experience reward: " + enemy.ExperienceReward + " XP");
                int armor = enemy.GetStatusEffectStacks(StatusEffectType.ARMOR);
                int burn = enemy.GetStatusEffectStacks(StatusEffectType.BURN);
                if (armor > 0 || burn > 0)
                {
                    text.AppendLine("Current effects:");
                    if (armor > 0) text.AppendLine("  Armor: " + armor);
                    if (burn > 0) text.AppendLine("  Burn: " + burn);
                }
                text.AppendLine();
            }
        }

        if (count == 0)
            text.AppendLine("No living enemies are currently in this battle.");
        bodyText.text = text.ToString();
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
