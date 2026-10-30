using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Presentation and modal navigation for the MainMenu scene.</summary>
public sealed class MainMenuPresentation : MonoBehaviour
{
    public CanvasGroup home;
    public GameObject modalLayer;
    public GameObject settingsPanel, storyPanel, helpPanel, aboutPanel, quitPanel;
    public UnityEngine.UI.Button playButton;
    public UnityEngine.UI.Toggle fullscreen;
    public TMP_Text musicValue, effectsValue, saveStatus, versionLabel;
    public MainMenuSaveSlots slots;
    private AudioManager audioManager;
    private bool slotsWereOpen;
    private GameObject returnSelection;

    private void Start()
    {
        audioManager = FindFirstObjectByType<AudioManager>();
        Time.timeScale = 1f;
        ClosePanels();
        if (fullscreen != null)
        {
#if !UNITY_EDITOR
            if (PlayerPrefs.HasKey("CodeEscapade.Fullscreen"))
                Screen.fullScreen = PlayerPrefs.GetInt("CodeEscapade.Fullscreen") == 1;
#endif
            fullscreen.SetIsOnWithoutNotify(Screen.fullScreen);
            fullscreen.onValueChanged.AddListener(SetFullscreen);
        }
        if (versionLabel != null) versionLabel.text = "CODE ESCAPADE  /  v" + Application.version;
        RefreshSaveStatus();
        Select(playButton != null ? playButton.gameObject : null);
    }

    private void RefreshSaveStatus()
    {
        int used = 0;
        for (int i = 0; i < SaveSlots.Count; i++) if (SaveSlots.Read(i) != null) used++;
        if (saveStatus != null) saveStatus.text = used == 0
            ? "Your first adventure awaits · " + SaveSlots.Count + " save slots"
            : used + " / " + SaveSlots.Count + " save slots used · Continue or begin anew";
    }

    private void Update()
    {
        if (CharacterAlmanac.IsOpen) return;
        bool savesOpen = slots != null && slots.IsOpen;
        if (savesOpen || slotsWereOpen)
        {
            if (!savesOpen)
            {
                RefreshSaveStatus();
                Select(playButton.gameObject);
            }
            slotsWereOpen = savesOpen;
            return;
        }
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            if (musicValue != null) musicValue.text = Mathf.RoundToInt(GameAudio.MusicVolume * 100) + "%";
            if (effectsValue != null) effectsValue.text = Mathf.RoundToInt(GameAudio.SfxVolume * 100) + "%";
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if(settingsPanel!=null&&settingsPanel.activeInHierarchy&&GameControlsGuide.TryCloseSettingsGuide(settingsPanel))return;
            if (modalLayer.activeSelf) CloseModal();
            else ShowQuit();
        }
    }

    private void Select(GameObject target)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target);
    }

    private void ClosePanels()
    {
        foreach (var panel in new[] {settingsPanel, storyPanel, helpPanel, aboutPanel, quitPanel})
            if (panel != null) panel.SetActive(false);
        modalLayer.SetActive(false);
        GameControlsGuide.TryCloseSettingsGuide(settingsPanel);
        SettingsUiVisibility.RefreshNow();
        home.interactable = true;
        home.blocksRaycasts = true;
    }

    private void Show(GameObject panel)
    {
        if (slots != null && slots.IsOpen) return;
        returnSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        ClosePanels();
        home.interactable = false;
        home.blocksRaycasts = false;
        modalLayer.SetActive(true);
        panel.SetActive(true);
        var first = panel.GetComponentInChildren<UnityEngine.UI.Selectable>();
        Select(first != null ? first.gameObject : null);
        audioManager?.PlayButtonSound();
    }

    public void ShowSettings() { GameControlsGuide.EnsureSettings(settingsPanel); Show(settingsPanel); }
    public void ShowStory() { Show(storyPanel); }
    public void ShowHelp() { GameControlsGuide.FillHelp(helpPanel); Show(helpPanel); }
    public void ShowAbout() { Show(aboutPanel); }
    public void ShowQuit() { Show(quitPanel); }
    public void CloseModal()
    {
        ClosePanels();
        Select(returnSelection != null && returnSelection.activeInHierarchy ? returnSelection : playButton.gameObject);
        audioManager?.PlayButtonSound();
    }

    private void SetFullscreen(bool enabled)
    {
        Screen.fullScreen = enabled;
        PlayerPrefs.SetInt("CodeEscapade.Fullscreen", enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ConfirmQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        if (fullscreen != null) fullscreen.onValueChanged.RemoveListener(SetFullscreen);
    }
}
