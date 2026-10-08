using UnityEngine;
using UnityEngine.SceneManagement;

public class Setiing : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject historyPanel;

    public void OpenSettings()
    {
        historyPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    public void OpenHistory()
    {
        settingsPanel.SetActive(false);
        historyPanel.SetActive(true);
    }

    public void CloseHistory()
    {
        historyPanel.SetActive(false);
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void ContinueGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void NewGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void OpenMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}