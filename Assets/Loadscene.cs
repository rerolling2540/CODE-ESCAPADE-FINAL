using UnityEngine;
using UnityEngine.SceneManagement;

public class Settings : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;

    // OPEN SETTINGS
    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    // CLOSE SETTINGS
    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    // BACK TO MAIN MENU
    public void OpenMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    // PLAY GAME - GO TO SCENE 2
    public void PlayGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Scene 2");
    }
}
