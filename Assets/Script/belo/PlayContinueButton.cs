using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayContinueButton : MonoBehaviour
{
    public TMP_Text buttonText;
    public string gameScene = "GameScene";

    void Start()
    {
        UpdateButtonText();
    }

    void UpdateButtonText()
    {
        if (PlayerPrefs.GetInt("HasGame", 0) == 1)
        {
            buttonText.text = "CONTINUE";
        }
        else
        {
            buttonText.text = "PLAY";
        }
    }

    public void PlayOrContinue()
    {
        if (PlayerPrefs.GetInt("HasGame", 0) == 1)
        {
            // Continue
            SceneManager.LoadScene(gameScene);
        }
        else
        {
            // First time Play
            PlayerPrefs.SetInt("HasGame", 1);
            PlayerPrefs.Save();

            SceneManager.LoadScene(gameScene);
        }
    }
}