using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>Hosts and wires the original Scene 2 settings objects in the battle scene.</summary>
public sealed class BattleSettingsMenu : MonoBehaviour
{
    private GameObject panel;
    private GameObject settingsButton;
    private Image inputBlocker;
    private TextMeshProUGUI turnCountLabel;
    private int turnCount = 1;
    private float previousTimeScale = 1f;
    private bool settingsPausedBattle;

    public static void EnsureCreated()
    {
        if (FindFirstObjectByType<BattleSettingsMenu>() != null) return;

        Scene activeScene = SceneManager.GetActiveScene();
        Transform settingsCanvas = FindNamedInScene("Battle Settings Canvas");
        Transform panelTransform = settingsCanvas != null ? settingsCanvas.Find("Panel") : null;
        Transform buttonTransform = settingsCanvas != null ? settingsCanvas.Find("setting") : null;
        if (panelTransform == null || buttonTransform == null)
        {
            Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform item in all)
            {
                if (item.gameObject.scene != activeScene) continue;
                if (panelTransform == null && item.name == "Panel") panelTransform = item;
                if (buttonTransform == null && item.name == "setting") buttonTransform = item;
            }
        }

        if (panelTransform == null || buttonTransform == null)
        {
            Debug.LogError("Battle settings could not find the reused Scene 2 Panel and setting button. Copy those Scene 2 UI objects into Battle Scene.");
            return;
        }

        buttonTransform.gameObject.AddComponent<BattleSettingsMenu>();
    }

    private void Awake()
    {
        settingsButton = gameObject;
        Scene activeScene = SceneManager.GetActiveScene();
        Transform settingsCanvas = FindNamedInScene("Battle Settings Canvas");
        Transform panelTransform = settingsCanvas != null ? settingsCanvas.Find("Panel") : null;
        if (panelTransform == null)
        {
            Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform item in all)
                if (item.gameObject.scene == activeScene && item.name == "Panel") { panelTransform = item; break; }
        }
        panel = panelTransform != null ? panelTransform.gameObject : null;
        if (panel == null)
        {
            Debug.LogError("The reused Scene 2 settings panel is missing from Battle Scene.");
            return;
        }

        // Build the shared settings actions during scene setup. Doing this on the
        // first click caused a noticeable main-thread hitch in the battle.
        GameControlsGuide.EnsureSettings(panel);

        Transform canvasTransform = FindNamedInScene("Battle Settings Canvas");
        GameObject canvasObject;
        if (canvasTransform == null)
        {
            canvasObject = new GameObject("Battle Settings Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }
        else canvasObject = canvasTransform.gameObject;

        Transform blockerTransform = canvasTransform != null
            ? canvasTransform.Find("Settings Pause Input Blocker") : null;
        if (blockerTransform == null)
        {
            GameObject blocker = new GameObject("Settings Pause Input Blocker", typeof(RectTransform), typeof(Image));
            blocker.transform.SetParent(canvasObject.transform, false);
            RectTransform blockerRect = blocker.GetComponent<RectTransform>();
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;
            inputBlocker = blocker.GetComponent<Image>();
            inputBlocker.color = new Color(0f, 0f, 0f, 0.56f);
            inputBlocker.raycastTarget = true;
            blocker.transform.SetAsFirstSibling();
            blocker.SetActive(false);
        }
        else inputBlocker = blockerTransform.GetComponent<Image>();

        // These are the original Scene 2 objects and their sprites/components; only their
        // canvas parent and battle positioning change.
        if (settingsButton.transform.parent != canvasObject.transform)
            settingsButton.transform.SetParent(canvasObject.transform, false);
        RectTransform buttonRect = settingsButton.GetComponent<RectTransform>();
        if (buttonRect != null && buttonRect.sizeDelta == Vector2.zero)
        {
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 1f);
            buttonRect.anchoredPosition = new Vector2(-30f, -26f);
        }

        BuildHamburgerIcon();
        BuildTurnCounter(canvasObject.transform, buttonRect);

        if (panel.transform.parent != canvasObject.transform)
            panel.transform.SetParent(canvasObject.transform, false);

        // SettingsUiVisibility normally hides non-settings UI while this panel
        // is open. Keep the menu toggle and its pause blocker inside that
        // protected branch so the user can close the menu and gameplay stays blocked.
        GameControlsGuide.RegisterSettingsControl(settingsButton);
        if (inputBlocker != null) GameControlsGuide.RegisterSettingsControl(inputBlocker.gameObject);

        Button settingsClick = settingsButton.GetComponent<Button>();
        if (settingsClick != null)
        {
            // Clear stale runtime handlers; the scene's legacy persistent click
            // calls are also cleared in Battle Scene so this toggle runs once.
            settingsClick.onClick.RemoveAllListeners();
            settingsClick.onClick.AddListener(ToggleSettings);
        }

        Slider[] sliders = panel.GetComponentsInChildren<Slider>(true);
        foreach (Slider slider in sliders)
        {
            string key = slider.name.ToLowerInvariant();
            if (key.Contains("music"))
            {
                slider.SetValueWithoutNotify(GameAudio.MusicVolume);
                slider.onValueChanged.AddListener(GameAudio.SetMusicVolume);
            }
            else if (key.Contains("sfx") || key.Contains("sound"))
            {
                slider.SetValueWithoutNotify(GameAudio.SfxVolume);
                slider.onValueChanged.AddListener(GameAudio.SetSfxVolume);
            }
        }
        GameAudio.ApplySavedVolumes();

        Button lobbyButton = FindButton(panel.transform, "Lobby");
        if (lobbyButton != null)
        {
            TMP_Text label = lobbyButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = "RETREAT";
            lobbyButton.onClick.AddListener(LeaveBattle);
        }

        Button resumeButton = FindButton(panel.transform, "Resume");
        if (resumeButton != null)
        {
            // The Scene 2 button still carries its old SettingsUI callback,
            // which always forces timeScale to 1. Route it through the battle
            // menu so it restores the exact state captured when the menu opened.
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(CloseSettings);
        }
    }

    private void Update()
    {
        if (panel != null && inputBlocker != null && inputBlocker.gameObject.activeSelf != panel.activeSelf)
        {
            inputBlocker.gameObject.SetActive(panel.activeSelf);
            // Recover cleanly if an old scene callback hid the panel directly.
            if (!panel.activeSelf && settingsPausedBattle) CloseSettings();
        }
        if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) CloseSettings();
    }

    private static Button FindButton(Transform root, string objectName)
    {
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
            if (button.gameObject.name == objectName) return button;
        return null;
    }

    private static Transform FindNamedInScene(string objectName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        foreach (Transform item in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (item.gameObject.scene == activeScene && item.name == objectName) return item;
        return null;
    }

    private void ToggleSettings()
    {
        if (panel != null && panel.activeSelf) CloseSettings();
        else OpenSettings();
    }

    private void BuildHamburgerIcon()
    {
        if (settingsButton == null) return;

        Image buttonImage = settingsButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            // Keep the original Button and its click listeners, but replace the gear art
            // with a compact dark-and-gold menu button.
            buttonImage.sprite = null;
            buttonImage.type = Image.Type.Simple;
            buttonImage.color = new Color(0.67f, 0.42f, 0.13f, 1f);
            buttonImage.raycastTarget = true;
        }

        Transform existingInner = settingsButton.transform.Find("Menu Icon Background");
        RectTransform inner = existingInner != null ? existingInner as RectTransform :
            CreateImage("Menu Icon Background", settingsButton.transform,
                new Color(0.12f, 0.075f, 0.035f, 1f), Vector2.zero, Vector2.one);
        if (existingInner == null)
        {
            inner.offsetMin = new Vector2(5f, 5f);
            inner.offsetMax = new Vector2(-5f, -5f);
        }
        Image innerImage = inner.GetComponent<Image>();
        if (innerImage != null) innerImage.raycastTarget = false;

        for (int i = 0; i < 3; i++)
        {
            Transform existingLine = inner.Find("Menu Icon Line " + (i + 1));
            if (existingLine != null)
            {
                Image existingLineImage = existingLine.GetComponent<Image>();
                if (existingLineImage != null) existingLineImage.raycastTarget = false;
                continue;
            }
            RectTransform bar = CreateImage("Menu Icon Line " + (i + 1), inner,
                new Color(1f, 0.82f, 0.42f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            bar.sizeDelta = new Vector2(30f, 4f);
            bar.anchoredPosition = new Vector2(0f, (1 - i) * 9f);
        }
    }

    private void BuildTurnCounter(Transform canvasTransform, RectTransform settingsRect)
    {
        Transform existing = canvasTransform.Find("Battle Turn Counter");
        GameObject counter = existing != null ? existing.gameObject :
            new GameObject("Battle Turn Counter", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform counterRect = counter.GetComponent<RectTransform>();
        if (counterRect.parent != canvasTransform) counterRect.SetParent(canvasTransform, false);
        bool needsLayout = counterRect.sizeDelta == Vector2.zero;
        if (needsLayout)
        {
            counterRect.anchorMin = counterRect.anchorMax = new Vector2(1f, 1f);
            counterRect.pivot = new Vector2(1f, 1f);
            counterRect.sizeDelta = new Vector2(132f, 54f);
        }

        float settingsWidth = settingsRect != null
            ? settingsRect.rect.width * Mathf.Abs(settingsRect.localScale.x)
            : 100f;
        Vector2 settingsPosition = settingsRect != null ? settingsRect.anchoredPosition : new Vector2(-30f, -26f);
        if (needsLayout)
            counterRect.anchoredPosition = new Vector2(
                settingsPosition.x - settingsWidth - 10f,
                settingsPosition.y - Mathf.Max(0f, (settingsRect != null ? settingsRect.rect.height * Mathf.Abs(settingsRect.localScale.y) : 80f) - counterRect.sizeDelta.y) * 0.5f);

        Image counterImage = counter.GetComponent<Image>();
        if (counterImage.color == Color.white) counterImage.color = new Color(0.67f, 0.42f, 0.13f, 1f);
        counterImage.raycastTarget = false;
        Button counterButton = counter.GetComponent<Button>();
        counterButton.transition = Selectable.Transition.None;
        counterButton.interactable = false;

        Transform existingInner = counter.transform.Find("Turn Counter Background");
        RectTransform inner = existingInner != null ? existingInner as RectTransform :
            CreateImage("Turn Counter Background", counter.transform,
                new Color(0.12f, 0.075f, 0.035f, 1f), Vector2.zero, Vector2.one);
        if (existingInner == null)
        {
            inner.offsetMin = new Vector2(4f, 4f);
            inner.offsetMax = new Vector2(-4f, -4f);
        }

        Transform existingLabel = inner.Find("Turn Count");
        GameObject labelObject = existingLabel != null ? existingLabel.gameObject :
            new GameObject("Turn Count", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        if (labelRect.parent != inner)
        {
            labelRect.SetParent(inner, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(3f, 0f);
            labelRect.offsetMax = new Vector2(-3f, 0f);
        }

        turnCountLabel = labelObject.GetComponent<TextMeshProUGUI>();
        if (turnCountLabel.font == null) turnCountLabel.font = TMP_Settings.defaultFontAsset;
        if (turnCountLabel.fontSize <= 0f) turnCountLabel.fontSize = 20f;
        if (turnCountLabel.color == Color.white) turnCountLabel.color = new Color(1f, 0.9f, 0.68f, 1f);
        turnCountLabel.alignment = TextAlignmentOptions.Center;
        turnCountLabel.raycastTarget = false;
        RefreshTurnCounter();
    }

    private static RectTransform CreateImage(string objectName, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    public static void AdvanceTurnCount()
    {
        BattleSettingsMenu menu = FindFirstObjectByType<BattleSettingsMenu>();
        if (menu == null) return;
        menu.turnCount++;
        menu.RefreshTurnCounter();
    }

    private void RefreshTurnCounter()
    {
        if (turnCountLabel != null) turnCountLabel.text = "TURN " + turnCount;
    }

    public void OpenSettings()
    {
        if (panel == null || panel.activeSelf) return;
        previousTimeScale = Time.timeScale;
        settingsPausedBattle = true;
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        if (settingsButton != null) settingsButton.transform.SetAsLastSibling();
        Time.timeScale = 0f;
        if (inputBlocker != null) inputBlocker.gameObject.SetActive(true);
        SettingsUiVisibility.RefreshNow();
    }

    public void CloseSettings()
    {
        if (panel == null || (!panel.activeSelf && !settingsPausedBattle)) return;
        panel.SetActive(false);
        if (inputBlocker != null) inputBlocker.gameObject.SetActive(false);
        // Preserve a pre-existing pause (for example, if another battle modal
        // had already paused the game before Settings was opened).
        if (settingsPausedBattle) Time.timeScale = previousTimeScale;
        settingsPausedBattle = false;
        SettingsUiVisibility.RefreshNow();
    }

    private void LeaveBattle()
    {
        Time.timeScale = 1f;
        EncounterBattleReturn battleReturn = FindFirstObjectByType<EncounterBattleReturn>();
        if (battleReturn != null) battleReturn.ReturnToLobby();
        else SceneManager.LoadScene("MainMenu");
    }
}
