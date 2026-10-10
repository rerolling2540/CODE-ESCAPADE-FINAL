using UnityEngine;
using TMPro;

public class PlayContinueButton : MonoBehaviour
{
    public TMP_Text buttonText;
    public string gameScene = "Scene 2";
    void Start()
    {
        SaveSlots.MigrateLegacy();
        if (buttonText != null) buttonText.text = SaveSlots.Read(SaveSlots.Active) != null ? "CONTINUE" : "PLAY";
    }
    public void PlayOrContinue()
    {
        var menu = FindFirstObjectByType<MainMenuSaveSlots>();
        if (menu != null) menu.ShowSlots();
        else GameSave.EnsureInstance().ContinueGame();
    }
}
