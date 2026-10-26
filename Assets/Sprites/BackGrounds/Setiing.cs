using UnityEngine;
using UnityEngine.SceneManagement;

public class Setiing : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject historyPanel;
    private float previousTimeScale = 1f;
    private bool pausedBySettings;

    public void OpenSettings()
    {
        if (settingsPanel == null) return;
        GameControlsGuide.EnsureSettings(settingsPanel);
        if (historyPanel != null) historyPanel.SetActive(false);
        if (!pausedBySettings) previousTimeScale = Time.timeScale;
        pausedBySettings = true;
        Time.timeScale = 0f;
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        ResumeGame();
    }

    public void OpenHistory()
    {
        settingsPanel.SetActive(false);
        historyPanel.SetActive(true);
    }

    public void CloseHistory()
    {
        historyPanel.SetActive(false);
        ResumeGame();
    }

    private void ResumeGame()
    {
        if (!pausedBySettings) return;
        Time.timeScale = previousTimeScale;
        pausedBySettings = false;
    }

    public void PlayGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameScene");
    }

    public void ContinueGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameScene");
    }

    public void NewGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameScene");
    }

    public void OpenMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
