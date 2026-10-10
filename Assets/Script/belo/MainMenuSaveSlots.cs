using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuSaveSlots : MonoBehaviour
{
    public CharacterSelector characterSelector;
    private bool open;
    private bool confirmOverwrite;
    private bool confirmDelete;
    private readonly List<CanvasGroup> groups = new List<CanvasGroup>();
    private readonly List<bool> previousInteraction = new List<bool>();
    private readonly List<bool> previousRaycasts = new List<bool>();
    private GUIStyle titleStyle, bodyStyle, slotStyle;

    void Awake() { GameSave.EnsureInstance(); }
    public void ShowSlots()
    {
        if (open) return;
        open = true;
        confirmOverwrite = false;
        confirmDelete = false;
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!canvas.isRootCanvas) continue;
            var group = canvas.GetComponent<CanvasGroup>();
            if (group == null) group = canvas.gameObject.AddComponent<CanvasGroup>();
            groups.Add(group);
            previousInteraction.Add(group.interactable);
            previousRaycasts.Add(group.blocksRaycasts);
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }
    public void ShowNewGame() { ShowSlots(); }
    public void ShowContinue() { ShowSlots(); }

    void Close()
    {
        open = false;
        confirmOverwrite = false;
        confirmDelete = false;
        for (int i = 0; i < groups.Count; i++)
            if (groups[i] != null)
            {
                groups[i].interactable = previousInteraction[i];
                groups[i].blocksRaycasts = previousRaycasts[i];
            }
        groups.Clear(); previousInteraction.Clear(); previousRaycasts.Clear();
    }
    void OnDisable() { Close(); }
    void Update() { if (open && Input.GetKeyDown(KeyCode.Escape)) Close(); }

    void StartNew()
    {
        int character = characterSelector != null ? characterSelector.SelectedCharacter : PlayerPrefs.GetInt("SelectedCharacter", 0);
        Close();
        SaveSlots.BeginNew(character);
        GameSave.EnsureInstance().ContinueGame();
    }

    void OnGUI()
    {
        if (!open) return;
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            titleStyle.normal.textColor = new Color(1f, 0.88f, 0.6f);
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, wordWrap = true, alignment = TextAnchor.MiddleCenter };
            slotStyle = new GUIStyle(GUI.skin.button) { fontSize = 21, wordWrap = true, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(20, 20, 10, 10) };
        }
        Matrix4x4 old = GUI.matrix;
        float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280f * scale) / 2f,
            (Screen.height - 720f * scale) / 2f, 0f), Quaternion.identity, Vector3.one * scale);
        if (open)
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(0.045f, 0.04f, 0.035f, 0.97f);
            GUI.DrawTexture(new Rect(235, 65, 810, 590), Texture2D.whiteTexture);
            GUI.color = oldColor;
            GUI.Label(new Rect(270, 85, 740, 55), "Choose Your Adventure", titleStyle);
            GUI.Label(new Rect(270, 145, 740, 35), "Select a save slot to continue or start a new journey.", bodyStyle);
            if (confirmDelete)
            {
                GUI.Label(new Rect(310, 250, 660, 110), "Delete Slot " + (SaveSlots.Active + 1) + "?\nIts saved adventure, cards and currencies will be permanently removed.", bodyStyle);
                if (GUI.Button(new Rect(350, 410, 280, 50), "Delete Save"))
                {
                    SaveSlots.Delete(SaveSlots.Active);
                    confirmDelete = false;
                }
                if (GUI.Button(new Rect(650, 410, 280, 50), "Cancel")) confirmDelete = false;
            }
            else if (confirmOverwrite)
            {
                GUI.Label(new Rect(310, 250, 660, 110), "Replace the adventure in Slot " + (SaveSlots.Active + 1) + "?\nIts saved position, cards and currencies will be reset.", bodyStyle);
                if (GUI.Button(new Rect(350, 410, 280, 50), "Replace and Start New")) StartNew();
                if (GUI.Button(new Rect(650, 410, 280, 50), "Keep Existing Save")) confirmOverwrite = false;
            }
            else
            {
                for (int i = 0; i < SaveSlots.Count; i++)
                {
                    var saved = SaveSlots.Read(i);
                    string detail = saved == null ? "Empty slot" : saved.scene + "  |  Character " + (saved.character + 1) + "\nSaved " + saved.savedAt;
                    GUI.color = i == SaveSlots.Active ? new Color(1f, 0.86f, 0.5f) : Color.white;
                    if (GUI.Button(new Rect(285, 200 + i * 100, 710, 86), "Slot " + (i + 1) + "  -  " + detail, slotStyle)) SaveSlots.Active = i;
                }
                GUI.color = oldColor;
                var selected = SaveSlots.Read(SaveSlots.Active);
                GUI.enabled = selected != null && Application.CanStreamedLevelBeLoaded(selected.scene);
                if (GUI.Button(new Rect(285, 525, 165, 50), "Continue"))
                {
                    Close();
                    GameSave.EnsureInstance().ContinueGame();
                }
                GUI.enabled = true;
                if (GUI.Button(new Rect(465, 525, 165, 50), "New Game"))
                {
                    if (selected == null) StartNew();
                    else confirmOverwrite = true;
                }
                GUI.enabled = selected != null;
                if (GUI.Button(new Rect(645, 525, 165, 50), "Delete Slot")) confirmDelete = true;
                GUI.enabled = true;
                if (GUI.Button(new Rect(825, 525, 165, 50), "Back")) Close();
                GUI.Label(new Rect(285, 595, 710, 30), "Progress autosaves during exploration. Esc returns to the menu.", bodyStyle);
            }
        }
        GUI.matrix = old;
    }
}
