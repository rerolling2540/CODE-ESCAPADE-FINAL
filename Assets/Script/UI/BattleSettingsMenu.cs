using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>Hosts and wires the original Scene 2 settings objects in the battle scene.</summary>
public sealed class BattleSettingsMenu : MonoBehaviour
{
    private static BattleSettingsMenu activeInstance;

    [Header("Battle UI Click Sound")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField, Range(0f, 1f)] private float buttonClickVolume = 1f;

    private GameObject panel;
    private GameObject settingsButton;
    private Transform menuRoot;
    private Image inputBlocker;
    private TextMeshProUGUI turnCountLabel;
    private int turnCount = 1;
    private float previousTimeScale = 1f;
    private bool settingsPausedBattle;
    private AudioSource buttonAudioSource;

    public static void EnsureCreated()
    {
        Transform settingsCanvas = FindNamedInScene("Battle Settings Canvas");
        Transform panelTransform = FindDescendant(settingsCanvas, "Panel");
        Transform buttonTransform = FindDescendant(settingsCanvas, "setting");

        if (panelTransform == null || buttonTransform == null)
        {
            Debug.LogError("Battle settings could not find the reused Scene 2 Panel and setting button. Copy those Scene 2 UI objects into Battle Scene.");
            return;
        }

        // Use the settings canvas's own controls. Other canvases can contain
        // generic objects named "Panel" or "setting" and must not be selected.
        if (buttonTransform.GetComponent<BattleSettingsMenu>() == null)
            buttonTransform.gameObject.AddComponent<BattleSettingsMenu>();
    }

    private void Awake()
    {
        activeInstance = this;
        buttonAudioSource = GetComponent<AudioSource>();
        if (buttonAudioSource == null) buttonAudioSource = gameObject.AddComponent<AudioSource>();
        buttonAudioSource.playOnAwake = false;
        buttonAudioSource.loop = false;
        buttonAudioSource.spatialBlend = 0f;
        buttonAudioSource.volume = 1f;
        GameAudio.Route(buttonAudioSource, false);

        settingsButton = gameObject;
        Transform settingsCanvas = FindNamedInScene("Battle Settings Canvas");
        Transform panelTransform = FindDescendant(settingsCanvas, "Panel");
        panel = panelTransform != null ? panelTransform.gameObject : null;
        if (panel == null)
        {
            Debug.LogError("The reused Scene 2 settings panel is missing from Battle Scene.");
            return;
        }

        // The panel is opened only by the battle menu button. Force it closed
        // on scene entry even if an editor change or saved scene state left it visible.
        panel.SetActive(false);

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

        // Keep the user-authored MENU CANVAS grouping intact. The menu root is
        // raised as a unit above the pause blocker when settings opens.
        menuRoot = FindDescendant(canvasObject.transform, "MENU CANVAS");
        if (menuRoot == null) menuRoot = canvasObject.transform;

        Transform blockerTransform = FindDescendant(canvasObject.transform, "Settings Pause Input Blocker");
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
        else
        {
            inputBlocker = blockerTransform.GetComponent<Image>();
            if (inputBlocker == null) inputBlocker = blockerTransform.gameObject.AddComponent<Image>();
            RectTransform blockerRect = blockerTransform as RectTransform;
            if (blockerRect != null)
            {
                blockerRect.anchorMin = Vector2.zero;
                blockerRect.anchorMax = Vector2.one;
                blockerRect.offsetMin = Vector2.zero;
                blockerRect.offsetMax = Vector2.zero;
            }
            inputBlocker.color = new Color(0f, 0f, 0f, 0.56f);
            inputBlocker.raycastTarget = true;
            blockerTransform.SetAsFirstSibling();
            blockerTransform.gameObject.SetActive(false);
        }

        // These are the original Scene 2 objects and their sprites/components; only their
        // canvas parent and battle positioning change.
        if (!IsWithin(settingsButton.transform, canvasObject.transform))
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

        if (!IsWithin(panel.transform, canvasObject.transform))
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
            // The copied Scene 2 Lobby event refers to its own scene manager.
            // In battle, route this action through the battle return flow.
            lobbyButton.onClick.RemoveAllListeners();
            lobbyButton.onClick.AddListener(LeaveBattle);
        }

        Button resumeButton = FindButton(panel.transform, "Resume");
        Sprite closeSprite = null;
        if (resumeButton != null)
        {
            // The Scene 2 button still carries its old SettingsUI callback,
            // which always forces timeScale to 1. Route it through the battle
            // menu so it restores the exact state captured when the menu opened.
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(CloseSettings);

            // Resume is the close control in this reused settings artwork. The
            // serialized Image contains the X sprite; the old text label can
            // cover it, so keep the image and remove only the text overlay.
            Image resumeImage = resumeButton.GetComponent<Image>();
            if (resumeImage != null)
            {
                resumeImage.enabled = true;
                resumeImage.color = Color.white;
                resumeImage.raycastTarget = true;
                closeSprite = resumeImage.sprite;
            }
            TMP_Text resumeLabel = resumeButton.GetComponentInChildren<TMP_Text>(true);
            if (resumeLabel != null) resumeLabel.gameObject.SetActive(false);
            EnsureVisibleCloseArtwork(resumeButton, closeSprite);
        }

        // Battle's copied settings window has a dedicated Scene 2-style X in
        // its top-right corner. Give it the battle-aware close path so it
        // restores the time scale captured when the panel opened.
        Button closeButton = FindButton(panel.transform, "Close Button");
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseSettings);

            // Scene 2's close artwork is the X sprite itself; hide the old
            // "CLOSE [ESC]" overlay text that obscures the icon.
            TMP_Text closeLabel = closeButton.GetComponentInChildren<TMP_Text>(true);
            if (closeLabel != null) closeLabel.gameObject.SetActive(false);
            EnsureVisibleCloseArtwork(closeButton, closeSprite);
        }

        // Attach pointer/submit relays once to authored scene buttons. Runtime
        // Codex controls register themselves when they are created.
        AttachClickSoundToSceneButtons();
    }

    private static void EnsureVisibleCloseArtwork(Button closeButton, Sprite fallbackSprite)
    {
        if (closeButton == null) return;

        Image buttonImage = closeButton.GetComponent<Image>();
        if (buttonImage == null) buttonImage = closeButton.gameObject.AddComponent<Image>();
        Sprite iconSprite = buttonImage.sprite != null ? buttonImage.sprite : fallbackSprite;
        if (iconSprite == null) return;

        buttonImage.enabled = true;
        buttonImage.sprite = iconSprite;
        buttonImage.color = Color.white;
        buttonImage.raycastTarget = true;
        buttonImage.type = Image.Type.Simple;

        // Keep the icon inside the same settings branch so CanvasGroups and
        // SettingsUiVisibility continue to hide/block gameplay consistently.
        Transform iconTransform = closeButton.transform.Find("Close X Icon");
        Image iconImage;
        if (iconTransform == null)
        {
            GameObject iconObject = new GameObject("Close X Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(closeButton.transform, false);
            iconTransform = iconObject.transform;
            iconImage = iconObject.GetComponent<Image>();
        }
        else
        {
            iconImage = iconTransform.GetComponent<Image>();
            if (iconImage == null) iconImage = iconTransform.gameObject.AddComponent<Image>();
        }

        RectTransform iconRect = iconTransform as RectTransform;
        if (iconRect != null)
        {
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            iconRect.localScale = Vector3.one;
        }

        iconImage.enabled = true;
        iconImage.sprite = iconSprite;
        iconImage.color = Color.white;
        iconImage.raycastTarget = false;
        iconImage.type = Image.Type.Simple;
        iconImage.preserveAspect = false;
        iconTransform.SetAsLastSibling();
        closeButton.transform.SetAsLastSibling();
    }

    private void Update()
    {
        if (panel != null && inputBlocker != null && inputBlocker.gameObject.activeSelf != panel.activeSelf)
        {
            inputBlocker.gameObject.SetActive(panel.activeSelf);
            // Recover cleanly if an old scene callback hid the panel directly.
            if (!panel.activeSelf && settingsPausedBattle) CloseSettings();
        }
        if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape) &&
            !GameControlsGuide.TryCloseSettingsGuide(panel)) CloseSettings();
    }

    private void AttachClickSoundToSceneButtons()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button == null || button.gameObject.scene != activeScene) continue;
            AttachClickSound(button);
        }
    }

    /// <summary>Registers a runtime-created battle button for the shared Scene 2 click sound.</summary>
    public static void AttachClickSound(Button button)
    {
        if (button == null || activeInstance == null) return;
        BattleButtonClickSound clickSound = button.GetComponent<BattleButtonClickSound>();
        if (clickSound == null) clickSound = button.gameObject.AddComponent<BattleButtonClickSound>();

        // Some copied Scene 2 buttons already invoke AudioManager.PlayButtonSound.
        // Do not play a second copy of the same click when that legacy event exists.
        bool alreadyPlaysScene2Click = false;
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentMethodName(i) == "PlayButtonSound")
            {
                alreadyPlaysScene2Click = true;
                break;
            }
        }

        clickSound.Configure(activeInstance.buttonAudioSource,
            alreadyPlaysScene2Click ? null : activeInstance.buttonClickSound,
            activeInstance.buttonClickVolume);
    }

    private void OnDestroy()
    {
        if (activeInstance == this) activeInstance = null;
    }

    private static Button FindButton(Transform root, string objectName)
    {
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
            if (button.gameObject.name == objectName) return button;
        return null;
    }

    private static bool IsWithin(Transform child, Transform ancestor)
    {
        return child != null && ancestor != null && (child == ancestor || child.IsChildOf(ancestor));
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        if (root == null) return null;
        if (root.name == objectName) return root;
        foreach (Transform child in root)
        {
            Transform found = FindDescendant(child, objectName);
            if (found != null) return found;
        }
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
        counterRect.anchorMin = counterRect.anchorMax = new Vector2(1f, 1f);
        counterRect.pivot = new Vector2(1f, 1f);
        counterRect.sizeDelta = new Vector2(142f, 54f);

        float settingsWidth = settingsRect != null
            ? settingsRect.rect.width * Mathf.Abs(settingsRect.localScale.x)
            : 100f;
        Vector2 settingsPosition = settingsRect != null ? settingsRect.anchoredPosition : new Vector2(-30f, -26f);
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
        // The authored Battle Scene stores Turn Count directly under the counter.
        // Reuse and move it into the inset background instead of creating a
        // second, overlapping TMP label (which makes the digits look crossed out).
        if (existingLabel == null) existingLabel = counter.transform.Find("Turn Count");
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
        turnCountLabel.enableAutoSizing = true;
        turnCountLabel.fontSizeMin = 18f;
        turnCountLabel.fontSizeMax = 25f;
        turnCountLabel.textWrappingMode = TextWrappingModes.NoWrap;
        turnCountLabel.overflowMode = TextOverflowModes.Overflow;
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
        // Restore the source card before visibility snapshots are captured.
        CardViewHoverSystem.Instance?.Hide();
        SettingsUiVisibility.EnsureInstance();
        previousTimeScale = Time.timeScale;
        settingsPausedBattle = true;
        panel.SetActive(true);
        if (menuRoot != null) menuRoot.SetAsLastSibling();
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
