using UnityEngine;

public class NewGameButton : MonoBehaviour
{
    public string gameScene = "Scene 2";
    public void StartNewGame()
    {
        var menu = FindFirstObjectByType<MainMenuSaveSlots>();
        if (menu != null) { menu.ShowNewGame(); return; }
        var save = GameSave.EnsureInstance();
        save.NewGame();
        save.ContinueGame();
    }
}
