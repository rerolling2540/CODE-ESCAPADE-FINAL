using UnityEngine;

public class NewGameButton : MonoBehaviour
{
    public string gameScene = "Whispering Hollow";
    public void StartNewGame()
    {
        var menu = FindFirstObjectByType<MainMenuSaveSlots>();
        if (menu != null) { menu.ShowNewGame(); return; }
        var save = GameSave.EnsureInstance();
        save.NewGame();
        save.ContinueGame();
    }
}
