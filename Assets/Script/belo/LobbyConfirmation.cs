
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyConfirmation : MonoBehaviour
{
    [Header("Confirmation Panel")]
    public GameObject confirmPanel;

    [Header("Scene Name")]
    public string lobbySceneName = "MainMenu";

    public void ShowConfirmation()
    {
        GameControlsGuide.RegisterMapBlocker(confirmPanel);
        confirmPanel.SetActive(true);
    }

    public void ConfirmYes()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(lobbySceneName);
    }

    public void ConfirmNo()
    {
        confirmPanel.SetActive(false);
    }
}
