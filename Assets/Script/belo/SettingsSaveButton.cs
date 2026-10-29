using TMPro;
using UnityEngine;

/// <summary>Manual save uses the same slot and position writer as autosave.</summary>
public sealed class SettingsSaveButton : MonoBehaviour
{
    private UnityEngine.UI.Button button;
    private TMP_Text label;
    private float savedUntil;
    public void Configure(UnityEngine.UI.Button target, TMP_Text caption)
    {
        button=target; label=caption;
        button.onClick.AddListener(Save);
    }
    void Update()
    {
        if (button == null) return;
        bool available=GameSave.EnsureInstance().CanSaveCurrentPlayer;
        button.interactable=available;
        if(label!=null)label.text=Time.unscaledTime<savedUntil ? "SAVED!" : available ? "SAVE GAME" : "SAVE UNAVAILABLE";
    }
    void Save()
    {
        if(GameSave.EnsureInstance().ManualSave())savedUntil=Time.unscaledTime+2.5f;
    }
}
