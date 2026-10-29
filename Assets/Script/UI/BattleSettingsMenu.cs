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

    public static void EnsureCreated()
    {
        if (FindFirstObjectByType<BattleSettingsMenu>() != null) return;

        Scene activeScene = SceneManager.GetActiveScene();
        Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Transform panelTransform = null;
        Transform buttonTransform = null;
        foreach (Transform item in all)
        {
            if (item.gameObject.scene != activeScene) continue;
            if (item.name == "Panel") panelTransform = item;
            else if (item.name == "setting") buttonTransform = item;
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
        Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform item in all)
        {
            if (item.gameObject.scene == activeScene && item.name == "Panel")
            {
                panel = item.gameObject;
                break;
            }
        }
        if (panel == null)
        {
            Debug.LogError("The reused Scene 2 settings panel is missing from Battle Scene.");
            return;
        }

        GameObject canvasObject = new GameObject("Battle Settings Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

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

        // These are the original Scene 2 objects and their sprites/components; only their
        // canvas parent and battle positioning change.
        settingsButton.transform.SetParent(canvasObject.transform, false);
        RectTransform buttonRect = settingsButton.GetComponent<RectTransform>();
        if (buttonRect != null)
        {
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 1f);
            buttonRect.anchoredPosition = new Vector2(-30f, -26f);
        }

        BuildHamburgerIcon();
        BuildTurnCounter(canvasObject.transform, buttonRect);

        panel.transform.SetParent(canvasObject.transform, false);
        panel.transform.SetAsLastSibling();
        settingsButton.transform.SetAsLastSibling();

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
        if (lobbyButton != null) lobbyButton.onClick.AddListener(LeaveBattle);
    }

    private void Update()
    {
        if (panel != null && inputBlocker != null && inputBlocker.gameObject.activeSelf != panel.activeSelf)
            inputBlocker.gameObject.SetActive(panel.activeSelf);
        if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) CloseSettings();
    }

    private static Button FindButton(Transform root, string objectName)
    {
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
            if (button.gameObject.name == objectName) return button;
        return null;
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
        }

        foreach (Image image in settingsButton.GetComponentsInChildren<Image>(true))
            if (image.gameObject != settingsButton) image.enabled = false;

        RectTransform inner = CreateImage("Menu Icon Background", settingsButton.transform,
            new Color(0.12f, 0.075f, 0.035f, 1f), Vector2.zero, Vector2.one);
        inner.offsetMin = new Vector2(5f, 5f);
        inner.offsetMax = new Vector2(-5f, -5f);

        for (int i = 0; i < 3; i++)
        {
            RectTransform bar = CreateImage("Menu Icon Line " + (i + 1), inner,
                new Color(1f, 0.82f, 0.42f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            bar.sizeDelta = new Vector2(30f, 4f);
            bar.anchoredPosition = new Vector2(0f, (1 - i) * 9f);
        }
    }

    private void BuildTurnCounter(Transform canvasTransform, RectTransform settingsRect)
    {
        GameObject counter = new GameObject("Battle Turn Counter", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform counterRect = counter.GetComponent<RectTransform>();
        counterRect.SetParent(canvasTransform, false);
        counterRect.anchorMin = counterRect.anchorMax = new Vector2(1f, 1f);
        counterRect.pivot = new Vector2(1f, 1f);
        counterRect.sizeDelta = new Vector2(132f, 54f);

        float settingsWidth = settingsRect != null
            ? settingsRect.rect.width * Mathf.Abs(settingsRect.localScale.x)
            : 100f;
        Vector2 settingsPosition = settingsRect != null ? settingsRect.anchoredPosition : new Vector2(-30f, -26f);
        counterRect.anchoredPosition = new Vector2(
            settingsPosition.x - settingsWidth - 10f,
            settingsPosition.y - Mathf.Max(0f, (settingsRect != null ? settingsRect.rect.height * Mathf.Abs(settingsRect.localScale.y) : 80f) - counterRect.sizeDelta.y) * 0.5f);

        Image counterImage = counter.GetComponent<Image>();
        counterImage.color = new Color(0.67f, 0.42f, 0.13f, 1f);
        counterImage.raycastTarget = false;
        Button counterButton = counter.GetComponent<Button>();
        counterButton.transition = Selectable.Transition.None;
        counterButton.interactable = false;

        RectTransform inner = CreateImage("Turn Counter Background", counter.transform,
            new Color(0.12f, 0.075f, 0.035f, 1f), Vector2.zero, Vector2.one);
        inner.offsetMin = new Vector2(4f, 4f);
        inner.offsetMax = new Vector2(-4f, -4f);

        GameObject labelObject = new GameObject("Turn Count", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(inner, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(3f, 0f);
        labelRect.offsetMax = new Vector2(-3f, 0f);

        turnCountLabel = labelObject.GetComponent<TextMeshProUGUI>();
        turnCountLabel.font = TMP_Settings.defaultFontAsset;
        turnCountLabel.fontSize = 20f;
        turnCountLabel.fontStyle = FontStyles.Bold;
        turnCountLabel.color = new Color(1f, 0.9f, 0.68f, 1f);
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
        if (panel == null) return;
        previousTimeScale = Time.timeScale;
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        if (settingsButton != null) settingsButton.transform.SetAsLastSibling();
        Time.timeScale = 0f;
    }

    public void CloseSettings()
    {
        if (panel != null) panel.SetActive(false);
        Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
    }

    private void LeaveBattle()
    {
        Time.timeScale = 1f;
        EncounterBattleReturn battleReturn = FindFirstObjectByType<EncounterBattleReturn>();
        if (battleReturn != null) battleReturn.ReturnToLobby();
        else SceneManager.LoadScene("MainMenu");
    }
}
