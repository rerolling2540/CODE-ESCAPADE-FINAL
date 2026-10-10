using UnityEngine;
using UnityEngine.SceneManagement;

public class NewGameButton : MonoBehaviour
{
    public string gameScene = "GameScene";

    public void StartNewGame()
    {
        // Delete old save
        PlayerPrefs.DeleteKey("HasGame");
        PlayerPrefs.DeleteKey("SavedScene");
        PlayerPrefs.DeleteKey("PlayerX");
        PlayerPrefs.DeleteKey("PlayerY");
        PlayerPrefs.DeleteKey("PlayerZ");

        PlayerPrefs.Save();

        Debug.Log("NEW GAME - OLD SAVE DELETED");

        // Start fresh game
        PlayerPrefs.SetInt("HasGame", 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene(gameScene);
    }
}