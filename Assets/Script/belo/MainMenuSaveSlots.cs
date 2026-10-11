using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuSaveSlots : MonoBehaviour
{
    public CharacterSelector characterSelector;

    [Header("Button Sound Effects")]
    public AudioSource buttonAudioSource;
    public AudioClip buttonClickSound;

    [Range(0f, 1f)]
    public float clickVolume = 0.8f;

    private bool open;
    private bool confirmOverwrite;
    private bool confirmDelete;

    private readonly List<CanvasGroup> groups =
        new List<CanvasGroup>();

    private readonly List<bool> previousInteraction =
        new List<bool>();

    private readonly List<bool> previousRaycasts =
        new List<bool>();

    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle slotTitleStyle;
    private GUIStyle slotDetailStyle;
    private GUIStyle buttonStyle;
    private GUIStyle dangerButtonStyle;
    private GUIStyle confirmTitleStyle;
    private GUIStyle confirmBodyStyle;
    private GUIStyle footerStyle;
    private GUIStyle statusStyle;

    private Texture2D panelTexture;
    private Texture2D innerTexture;
    private Texture2D normalTexture;
    private Texture2D hoverTexture;
    private Texture2D selectedTexture;
    private Texture2D selectedHoverTexture;
    private Texture2D goldTexture;
    private Texture2D buttonTexture;
    private Texture2D buttonHoverTexture;
    private Texture2D dangerTexture;
    private Texture2D dangerHoverTexture;
    private Texture2D backdropTexture;

    private readonly Color gold =
        new Color(1f, 0.76f, 0.34f);

    private readonly Color paleGold =
        new Color(1f, 0.91f, 0.69f);

    private readonly Color muted =
        new Color(0.70f, 0.67f, 0.61f);

    void Awake()
    {
        GameSave.EnsureInstance();
    }

    void PlayClickSound()
    {
        if (buttonAudioSource != null &&
            buttonClickSound != null)
        {
            buttonAudioSource.PlayOneShot(
                buttonClickSound,
                clickVolume
            );
        }
    }

    Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.hideFlags = HideFlags.HideAndDontSave;

        return texture;
    }

    void CreateUIStyles()
    {
        panelTexture = MakeTexture(
            new Color(0.055f, 0.045f, 0.032f, 0.99f));

        innerTexture = MakeTexture(
            new Color(0.085f, 0.068f, 0.047f, 1f));

        normalTexture = MakeTexture(
            new Color(0.13f, 0.10f, 0.065f, 1f));

        hoverTexture = MakeTexture(
            new Color(0.24f, 0.18f, 0.095f, 1f));

        selectedTexture = MakeTexture(
            new Color(0.30f, 0.22f, 0.095f, 1f));

        selectedHoverTexture = MakeTexture(
            new Color(0.40f, 0.29f, 0.12f, 1f));

        goldTexture = MakeTexture(gold);

        buttonTexture = MakeTexture(
            new Color(0.20f, 0.15f, 0.08f, 1f));

        buttonHoverTexture = MakeTexture(
            new Color(0.38f, 0.28f, 0.12f, 1f));

        dangerTexture = MakeTexture(
            new Color(0.32f, 0.075f, 0.055f, 1f));

        dangerHoverTexture = MakeTexture(
            new Color(0.50f, 0.11f, 0.075f, 1f));

        backdropTexture = MakeTexture(
            new Color(0f, 0f, 0f, 0.76f));

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 30;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = paleGold;

        subtitleStyle = new GUIStyle(GUI.skin.label);
        subtitleStyle.fontSize = 15;
        subtitleStyle.alignment = TextAnchor.MiddleCenter;
        subtitleStyle.wordWrap = true;
        subtitleStyle.normal.textColor = muted;

        slotTitleStyle = new GUIStyle(GUI.skin.label);
        slotTitleStyle.fontSize = 19;
        slotTitleStyle.fontStyle = FontStyle.Bold;
        slotTitleStyle.alignment = TextAnchor.MiddleLeft;
        slotTitleStyle.normal.textColor = paleGold;

        slotDetailStyle = new GUIStyle(GUI.skin.label);
        slotDetailStyle.fontSize = 12;
        slotDetailStyle.alignment = TextAnchor.MiddleLeft;
        slotDetailStyle.wordWrap = true;
        slotDetailStyle.normal.textColor = muted;

        buttonStyle = CreateButtonStyle(
            buttonTexture,
            buttonHoverTexture,
            paleGold
        );

        dangerButtonStyle = CreateButtonStyle(
            dangerTexture,
            dangerHoverTexture,
            Color.white
        );

        confirmTitleStyle = new GUIStyle(titleStyle);
        confirmTitleStyle.fontSize = 24;

        confirmBodyStyle = new GUIStyle(subtitleStyle);
        confirmBodyStyle.fontSize = 15;
        confirmBodyStyle.normal.textColor =
            new Color(0.90f, 0.86f, 0.78f);

        footerStyle = new GUIStyle(subtitleStyle);
        footerStyle.fontSize = 11;

        statusStyle = new GUIStyle(GUI.skin.label);
        statusStyle.fontSize = 10;
        statusStyle.fontStyle = FontStyle.Bold;
        statusStyle.alignment = TextAnchor.MiddleRight;
        statusStyle.normal.textColor = gold;
    }

    GUIStyle CreateButtonStyle(
        Texture2D normal,
        Texture2D hover,
        Color textColor)
    {
        GUIStyle style = new GUIStyle(GUI.skin.button);

        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.wordWrap = true;
        style.padding = new RectOffset(8, 8, 6, 6);

        style.normal.background = normal;
        style.hover.background = hover;
        style.active.background = hover;
        style.focused.background = normal;

        style.normal.textColor = textColor;
        style.hover.textColor = Color.white;
        style.active.textColor = Color.white;
        style.focused.textColor = textColor;

        return style;
    }

    void DrawRect(Rect rect, Texture2D texture)
    {
        GUI.DrawTexture(rect, texture);
    }

    void DrawFrame()
    {
        DrawRect(
            new Rect(0, 0, 1280, 720),
            backdropTexture
        );

        // Main gold frame
        DrawRect(
            new Rect(222, 43, 836, 634),
            goldTexture
        );

        // Main dark panel
        DrawRect(
            new Rect(226, 47, 828, 626),
            panelTexture
        );

        DrawRect(
            new Rect(240, 61, 800, 606),
            innerTexture
        );

        // Decorative lines
        DrawRect(
            new Rect(270, 68, 740, 2),
            goldTexture
        );

        DrawRect(
            new Rect(270, 657, 740, 2),
            goldTexture
        );

        // Corner decorations
        DrawRect(
            new Rect(254, 78, 3, 35),
            goldTexture
        );

        DrawRect(
            new Rect(1023, 78, 3, 35),
            goldTexture
        );

        DrawRect(
            new Rect(254, 610, 3, 35),
            goldTexture
        );

        DrawRect(
            new Rect(1023, 610, 3, 35),
            goldTexture
        );
    }

    public void ShowSlots()
    {
        if (open) return;

        open = true;
        confirmOverwrite = false;
        confirmDelete = false;

        groups.Clear();
        previousInteraction.Clear();
        previousRaycasts.Clear();

        foreach (Canvas canvas in FindObjectsByType<Canvas>(
                     FindObjectsSortMode.None))
        {
            if (!canvas.isRootCanvas) continue;

            CanvasGroup group =
                canvas.GetComponent<CanvasGroup>();

            if (group == null)
            {
                group = canvas.gameObject
                    .AddComponent<CanvasGroup>();
            }

            groups.Add(group);
            previousInteraction.Add(group.interactable);
            previousRaycasts.Add(group.blocksRaycasts);

            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }

    public void ShowNewGame()
    {
        ShowSlots();
    }

    public void ShowContinue()
    {
        ShowSlots();
    }

    void Close()
    {
        open = false;
        confirmOverwrite = false;
        confirmDelete = false;

        for (int i = 0; i < groups.Count; i++)
        {
            if (groups[i] == null) continue;

            groups[i].interactable =
                previousInteraction[i];

            groups[i].blocksRaycasts =
                previousRaycasts[i];
        }

        groups.Clear();
        previousInteraction.Clear();
        previousRaycasts.Clear();
    }

    void OnDisable()
    {
        Close();
    }

    void Update()
    {
        if (open && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    void StartNew()
    {
        int character = characterSelector != null
            ? characterSelector.SelectedCharacter
            : PlayerPrefs.GetInt("SelectedCharacter", 0);

        Close();

        SaveSlots.BeginNew(character);
        GameSave.EnsureInstance().ContinueGame();
    }

    void DrawSlot(int index)
    {
        var saved = SaveSlots.Read(index);

        float y = 198 + index * 94;
        bool isSelected = index == SaveSlots.Active;

        Rect slotRect = new Rect(277, y, 726, 80);

        Texture2D background = isSelected
            ? selectedTexture
            : normalTexture;

        Texture2D hoverBackground = isSelected
            ? selectedHoverTexture
            : hoverTexture;

        GUIStyle slotButton = new GUIStyle(GUI.skin.button);

        slotButton.normal.background = background;
        slotButton.hover.background = hoverBackground;
        slotButton.active.background = hoverBackground;
        slotButton.focused.background = background;
        slotButton.border = new RectOffset(2, 2, 2, 2);

        if (GUI.Button(
            slotRect,
            GUIContent.none,
            slotButton))
        {
            PlayClickSound();
            SaveSlots.Active = index;
        }

        // Left accent
        DrawRect(
            new Rect(
                slotRect.x,
                slotRect.y,
                4,
                slotRect.height),
            isSelected ? goldTexture : normalTexture
        );

        string slotName = "SLOT " + (index + 1);
        string detail;

        if (saved == null)
        {
            detail = "Empty slot - begin a new adventure.";
        }
        else
        {
            detail =
                "Scene: " + saved.scene
                + "   |   Character: " + (saved.character + 1)
                + "\nLast saved: " + saved.savedAt;
        }

        GUI.Label(
            new Rect(300, y + 8, 430, 30),
            slotName,
            slotTitleStyle
        );

        GUI.Label(
            new Rect(300, y + 36, 540, 38),
            detail,
            slotDetailStyle
        );

        GUI.Label(
            new Rect(755, y + 15, 225, 25),
            isSelected ? "◆ SELECTED" :
                (saved == null ? "EMPTY SLOT" : "SAVED"),
            statusStyle
        );
    }

    void DrawActionButton(
        Rect rect,
        string label,
        System.Action action,
        bool danger = false)
    {
        GUIStyle style = danger
            ? dangerButtonStyle
            : buttonStyle;

        if (GUI.Button(rect, label, style))
        {
            PlayClickSound();
            action();
        }
    }

    void DrawConfirmation(
        string heading,
        string message,
        string confirmLabel,
        System.Action confirmAction,
        System.Action cancelAction)
    {
        DrawRect(
            new Rect(300, 220, 680, 310),
            panelTexture
        );

        DrawRect(
            new Rect(300, 220, 680, 2),
            goldTexture
        );

        GUI.Label(
            new Rect(330, 247, 620, 48),
            heading,
            confirmTitleStyle
        );

        GUI.Label(
            new Rect(350, 305, 580, 115),
            message,
            confirmBodyStyle
        );

        DrawActionButton(
            new Rect(350, 435, 270, 52),
            confirmLabel,
            confirmAction,
            true
        );

        DrawActionButton(
            new Rect(650, 435, 270, 52),
            "CANCEL",
            cancelAction
        );
    }

    void OnGUI()
    {
        if (!open) return;

        if (titleStyle == null)
        {
            CreateUIStyles();
        }

        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;
        bool oldEnabled = GUI.enabled;

        float scale = Mathf.Min(
            Screen.width / 1280f,
            Screen.height / 720f
        );

        GUI.matrix = Matrix4x4.TRS(
            new Vector3(
                (Screen.width - 1280f * scale) / 2f,
                (Screen.height - 720f * scale) / 2f,
                0f
            ),
            Quaternion.identity,
            Vector3.one * scale
        );

        DrawFrame();

        GUI.Label(
            new Rect(270, 78, 740, 52),
            "CHOOSE YOUR ADVENTURE",
            titleStyle
        );

        GUI.Label(
            new Rect(290, 132, 700, 28),
            "Select your save slot and continue your journey.",
            subtitleStyle
        );

        if (confirmDelete)
        {
            DrawConfirmation(
                "DELETE ADVENTURE?",
                "Delete Slot " + (SaveSlots.Active + 1) + "?\n\n"
                + "Its saved adventure, cards and currencies "
                + "will be permanently removed.",
                "DELETE SAVE",
                () =>
                {
                    SaveSlots.Delete(SaveSlots.Active);
                    confirmDelete = false;
                },
                () => confirmDelete = false
            );
        }
        else if (confirmOverwrite)
        {
            DrawConfirmation(
                "START A NEW ADVENTURE?",
                "Replace the adventure in Slot "
                + (SaveSlots.Active + 1) + "?\n\n"
                + "Its saved position, cards and currencies "
                + "will be reset.",
                "REPLACE AND START",
                StartNew,
                () => confirmOverwrite = false
            );
        }
        else
        {
            for (int i = 0; i < SaveSlots.Count; i++)
            {
                DrawSlot(i);
            }

            var selected = SaveSlots.Read(SaveSlots.Active);

            GUI.enabled = selected != null
                && Application.CanStreamedLevelBeLoaded(
                    selected.scene
                );

            DrawActionButton(
                new Rect(277, 488, 166, 48),
                "▶  CONTINUE",
                () =>
                {
                    Close();
                    GameSave.EnsureInstance().ContinueGame();
                }
            );

            GUI.enabled = true;

            DrawActionButton(
                new Rect(454, 488, 166, 48),
                "+  NEW GAME",
                () =>
                {
                    if (selected == null)
                    {
                        StartNew();
                    }
                    else
                    {
                        confirmOverwrite = true;
                    }
                }
            );

            GUI.enabled = selected != null;

            DrawActionButton(
                new Rect(631, 488, 166, 48),
                "DELETE SLOT",
                () => confirmDelete = true,
                true
            );

            GUI.enabled = true;

            DrawActionButton(
                new Rect(808, 488, 195, 48),
                "←  BACK",
                Close
            );

            GUI.Label(
                new Rect(280, 555, 720, 28),
                "PROGRESS AUTOSAVES DURING EXPLORATION",
                footerStyle
            );

            GUI.Label(
                new Rect(280, 583, 720, 25),
                "Press ESC to return to the main menu.",
                footerStyle
            );
        }

        GUI.enabled = oldEnabled;
        GUI.color = oldColor;
        GUI.matrix = oldMatrix;
    }

    void OnDestroy()
    {
        DestroyTexture(panelTexture);
        DestroyTexture(innerTexture);
        DestroyTexture(normalTexture);
        DestroyTexture(hoverTexture);
        DestroyTexture(selectedTexture);
        DestroyTexture(selectedHoverTexture);
        DestroyTexture(goldTexture);
        DestroyTexture(buttonTexture);
        DestroyTexture(buttonHoverTexture);
        DestroyTexture(dangerTexture);
        DestroyTexture(dangerHoverTexture);
        DestroyTexture(backdropTexture);
    }

    void DestroyTexture(Texture2D texture)
    {
        if (texture != null)
        {
            Destroy(texture);
        }
    }
}