using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CharacterSelector : MonoBehaviour
{
    public Image characterImage;
    public TMP_Text characterName;

    public Sprite[] characters;
    public string[] characterNames;

    private int currentCharacter = 0;

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
        characterName.text = characterNames[currentCharacter];
    }
}