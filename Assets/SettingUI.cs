using UnityEngine;
using UnityEngine.SceneManagement;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;

    [Header("Game Save")]
    [SerializeField] private Transform player;

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OpenMainMenu()
    {
        // Save current player position
        GameSave.Instance.SaveGame(player);

        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}