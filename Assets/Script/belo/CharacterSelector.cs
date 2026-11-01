
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CharacterSelector : MonoBehaviour
{
    public Image characterImage;
    public TMP_Text characterName;

    // LOGO NG BAWAT CHARACTER
    public Image logoImage;
    public Sprite[] characterLogos;

    public Sprite[] characters;
    public string[] characterNames;

    private int currentCharacter = 0;
    public int SelectedCharacter => currentCharacter;

    void Start()
    {
        UpdateCharacter();
    }

    public void NextCharacter()
    {
        currentCharacter++;

        if (currentCharacter >= characters.Length)
        {
            currentCharacter = 0;
        }

        UpdateCharacter();
    }

    public void PreviousCharacter()
    {
        currentCharacter--;

        if (currentCharacter < 0)
        {
            currentCharacter = characters.Length - 1;
        }

        UpdateCharacter();
    }

    void UpdateCharacter()
    {
        characterImage.sprite = characters[currentCharacter];
        // Fit each portrait into the same display area without stretching its artwork.
        characterImage.preserveAspect = true;
        characterName.text = characterNames[currentCharacter];

        // I-UPDATE DIN ANG LOGO
        logoImage.sprite = characterLogos[currentCharacter];
        GetComponent<MainMenuCharacterIdle>()?.ShowCharacter(currentCharacter);
    }

    // PLAY BUTTON
    public void PlayGame()
    {
        PlayerPrefs.SetInt("SelectedCharacter", currentCharacter);
        PlayerPrefs.Save();
        GameSave.EnsureInstance().NewGame();
        SceneManager.LoadScene("StoryScene");
    }
}
