using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;

public class MatchSetupSystem : MonoBehaviour
{
    [SerializeField] private HeroData heroData;
    [SerializeField] private PerkData perkData;
    [SerializeField] private List<EnemyData> enemyDatas;
    

    private void Start()
    {
        BattleUITheme.Apply();
        HeroSystem.Instance.SetUp(heroData);
        bool caveBattle = CaveEnemyEncounter.PendingEnemy != null;
        string encounterId = CaveEnemyEncounter.PendingEncounterId;
        if (CaveEnemyEncounter.PendingEnemy != null)
        {
            EnemySystem.Instance.Setup(new List<EnemyData> { CaveEnemyEncounter.PendingEnemy });
            CaveEnemyEncounter.PendingEnemy = null;
            CaveEnemyEncounter.PendingEncounterId = null;
        }
        else EnemySystem.Instance.Setup(enemyDatas);
        foreach (var enemy in EnemySystem.Instance.Enemies)
            enemy.FaceTowards(HeroSystem.Instance.HeroView);
        if (EnemySystem.Instance.Enemies.Count > 0)
            HeroSystem.Instance.HeroView.FaceTowards(EnemySystem.Instance.Enemies[0]);
        gameObject.AddComponent<EncounterBattleReturn>().Configure(caveBattle, encounterId);
        CardSystem.Instance.Setup(heroData.Deck);
        PerkSystem.Instance.AddPerk(new Perk(perkData));
        DrawCardsGA drawCardsGA = new(5);
        ActionSystem.Instance.Perform(drawCardsGA);
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
        button.interactable = !resolving && !answering && !EncounterBattleReturn.BattleEnded;
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

    void OnGUI()
    {
        if (returning) return;
        if (BattleEnded)
        {
            BattleUITheme.DrawResultPanel(new Rect(Screen.width / 2f - 240, Screen.height / 2f - 65, 480, 130),
                victory ? "VICTORY\nReturning from battle..." : "DEFEAT\nReturning from battle...");
        }
        else if (caveBattle)
        {
            bool previousEnabled = GUI.enabled;
            GUI.enabled = ActionSystem.Instance != null && !ActionSystem.Instance.IsPerforming;
            if (BattleUITheme.DrawRetreatButton(new Rect(20, 20, 230, 42))) Return();
            GUI.enabled = previousEnabled;
        }
    }

    void OnDestroy() { BattleEnded = false; }
}
