using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Hosts and wires the original Scene 2 settings objects in the battle scene.</summary>
public sealed class BattleSettingsMenu : MonoBehaviour
{
    private GameObject panel;
    private GameObject settingsButton;
    private Image inputBlocker;
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

    public void OpenSettings()
    {
        if (panel == null) return;
        GameControlsGuide.EnsureSettings(panel);
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
