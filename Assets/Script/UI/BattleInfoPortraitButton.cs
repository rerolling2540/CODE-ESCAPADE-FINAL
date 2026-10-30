using UnityEngine;
using UnityEngine.UI;

/// <summary>Opens the battle codex when the player portrait is clicked.</summary>
[RequireComponent(typeof(Button))]
public sealed class BattleInfoPortraitButton : MonoBehaviour
{
    [SerializeField] private BattleInfoPanel panel;
    private Button portraitButton;

    private void Awake()
    {
        portraitButton = GetComponent<Button>();
        if (panel == null)
            panel = FindFirstObjectByType<BattleInfoPanel>(FindObjectsInactive.Include);
        if (portraitButton != null && panel != null)
            portraitButton.onClick.AddListener(panel.ToggleFromPortrait);
    }

    private void OnDestroy()
    {
        if (portraitButton != null && panel != null)
            portraitButton.onClick.RemoveListener(panel.ToggleFromPortrait);
    }
}
