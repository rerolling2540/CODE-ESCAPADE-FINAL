using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

/// <summary>
/// Runtime question-bank editor. The UI is available only to the configured
/// admin session; Supabase RLS separately enforces the same restriction.
/// </summary>
public class QuestionDataEditorScreen : MonoBehaviour
{
    private const string TableName = "question_bank";
    private static readonly Color32 Background = new Color32(22, 27, 35, 255);
    private static readonly Color32 Panel = new Color32(36, 43, 53, 255);
    private static readonly Color32 Input = new Color32(27, 33, 42, 255);
    private static readonly Color32 Gold = new Color32(205, 164, 91, 255);
    private static readonly Color32 Text = new Color32(239, 232, 215, 255);
    private static readonly Color32 Muted = new Color32(168, 174, 181, 255);
    private static readonly string[] Languages = { "java", "csharp", "python" };
    private static readonly string[] LanguageLabels = { "Java", "C#", "Python" };
    private static readonly string[] Difficulties = { "easy", "medium", "hard" };
    private static readonly string[] DifficultyLabels = { "Easy (1–5)", "Medium (6–15)", "Hard (16+)" };
    private static readonly string[] QuestionTypes =
    {
        "multiple_choice", "fill_blank", "missing_code", "fix_code", "predict_output"
    };
    private static readonly string[] QuestionTypeLabels =
    {
        "Multiple choice", "Fill in the blank", "Find missing code", "Fix the code", "Predict the output"
    };

    [Serializable]
    private sealed class QuestionRecord
    {
        public string id;
        public string language;
        public string topic;
        public string difficulty;
        public int min_level;
        public int max_level;
        public string question_type;
        public string prompt;
        public string code_snippet;
        public string[] choices;
        public int correct_index;
        public string explanation;
        public bool published;
    }

    [Serializable]
    private sealed class QuestionRows { public QuestionRecord[] items; }

    private readonly List<QuestionRecord> questions = new List<QuestionRecord>();
    private string projectUrl;
    private string publishableKey;
    private string accessToken;
    private string selectedId = string.Empty;
    private string filterText = string.Empty;
    private string status = "Connecting to Supabase…";
    private bool statusIsError;
    private bool requestInProgress;
    private bool confirmDelete;
    private bool stylesReady;
    private Vector2 listScroll;
    private Vector2 editorScroll;

    private string idInput;
    private int languageIndex;
    private int difficultyIndex;
    private int questionTypeIndex;
    private string minLevelInput;
    private string maxLevelInput;
    private string topicInput;
    private string promptInput;
    private string codeInput;
    private readonly string[] choiceInputs = new string[4];
    private int correctIndex;
    private string explanationInput;
    private bool publishedInput;

    private GUIStyle rootStyle;
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle headingStyle;
    private GUIStyle labelStyle;
    private GUIStyle mutedStyle;
    private GUIStyle buttonStyle;
    private GUIStyle selectedButtonStyle;
    private GUIStyle fieldStyle;
    private GUIStyle areaStyle;
    private GUIStyle toolbarStyle;
    private GUIStyle statusStyle;
    private GUIStyle errorStatusStyle;
    private Texture2D rootTexture;
    private Texture2D panelTexture;
    private Texture2D inputTexture;
    private Texture2D buttonTexture;
    private Texture2D selectedTexture;
    private Texture2D dangerTexture;
    private Texture2D dangerHoverTexture;

    private void Start()
    {
        EnsureRenderCamera();
        projectUrl = MainMenuSaveSlots.CurrentSupabaseProjectUrl;
        publishableKey = MainMenuSaveSlots.CurrentSupabasePublishableKey;
        accessToken = MainMenuSaveSlots.CurrentAccessToken;

        if (!MainMenuSaveSlots.HasAdminSession)
        {
            SetStatus("Admin sign-in required. Return to the Main Menu and sign in as the administrator.", true);
            return;
        }

        ResetForm();
        StartCoroutine(RefreshRoutine());
    }

    private void EnsureRenderCamera()
    {
        if (Camera.allCamerasCount > 0) return;

        GameObject cameraObject = new GameObject("Question Data Editor Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Background;
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
    }

    private void OnGUI()
    {
        EnsureStyles();
        Matrix4x4 oldMatrix = GUI.matrix;
        float scale = Mathf.Min(Screen.width / 1600f, Screen.height / 900f);
        float offsetX = (Screen.width - 1600f * scale) * 0.5f;
        float offsetY = (Screen.height - 900f * scale) * 0.5f;
        GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity,
            new Vector3(scale, scale, 1f));

        GUI.DrawTexture(new Rect(0, 0, 1600, 900), rootTexture);
        GUILayout.BeginArea(new Rect(28, 20, 1544, 860), rootStyle);
        DrawHeader();

        if (!MainMenuSaveSlots.HasAdminSession)
        {
            GUILayout.Space(40);
            GUILayout.Label("ADMIN ACCESS REQUIRED", headingStyle);
            GUILayout.Label("Return to the Main Menu and sign in with the admin account.", labelStyle);
            GUILayout.Space(20);
            if (GUILayout.Button("BACK TO GAME", buttonStyle, GUILayout.Width(220), GUILayout.Height(46)))
                BackToGame();
        }
        else
        {
            GUILayout.BeginHorizontal();
            DrawQuestionList();
            DrawQuestionForm();
            GUILayout.EndHorizontal();
            GUILayout.Space(8);
            GUIStyle currentStatusStyle = statusIsError ? errorStatusStyle : statusStyle;
            GUILayout.Label(status, currentStatusStyle, GUILayout.Height(28));
        }

        GUILayout.EndArea();
        if (confirmDelete) DrawDeleteConfirmation();
        GUI.matrix = oldMatrix;
    }

    private void DrawHeader()
    {
        GUILayout.BeginHorizontal(GUILayout.Height(76));
        GUILayout.BeginVertical();
        GUILayout.Label("QUESTION BANK STUDIO", titleStyle, GUILayout.Height(44));
        GUILayout.Label("Supabase question editor  /  languages, levels, formats and answers", mutedStyle,
            GUILayout.Height(24));
        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("BACK TO GAME", buttonStyle, GUILayout.Width(205), GUILayout.Height(46)))
            BackToGame();
        GUILayout.EndHorizontal();
        GUILayout.Space(12);
    }

    private void DrawQuestionList()
    {
        GUILayout.BeginVertical(panelStyle, GUILayout.Width(405), GUILayout.ExpandHeight(true));
        GUILayout.BeginHorizontal();
        GUILayout.Label("QUESTIONS", headingStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label(questions.Count + " records", mutedStyle, GUILayout.Width(95));
        GUILayout.EndHorizontal();
        filterText = GUILayout.TextField(filterText, fieldStyle, GUILayout.Height(36));
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("NEW QUESTION", buttonStyle, GUILayout.Height(38))) ResetForm();
        if (GUILayout.Button("REFRESH", buttonStyle, GUILayout.Height(38)) && !requestInProgress)
            StartCoroutine(RefreshRoutine());
        GUILayout.EndHorizontal();

        listScroll = GUILayout.BeginScrollView(listScroll, GUILayout.ExpandHeight(true));
        for (int i = 0; i < questions.Count; i++)
        {
            QuestionRecord row = questions[i];
            string search = (row.id + " " + row.prompt + " " + row.language + " " + row.difficulty + " " + row.topic)
                .ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(filterText) && !search.Contains(filterText.Trim().ToLowerInvariant()))
                continue;

            string title = string.IsNullOrWhiteSpace(row.prompt) ? "Untitled question" : row.prompt.Trim();
            if (title.Length > 80) title = title.Substring(0, 77) + "…";
            string subtitle = string.Format("{0}  •  {1}  •  level {2}{3}",
                row.language, row.difficulty + " · " + (string.IsNullOrWhiteSpace(row.topic) ? "general" : row.topic), row.min_level,
                row.max_level > 0 ? "–" + row.max_level : "+");
            string label = "<b>" + EscapeRichText(title) + "</b>\n<size=13>" +
                           EscapeRichText(row.id + "   " + subtitle + (row.published ? "" : "   DRAFT")) + "</size>";
            GUIStyle rowStyle = string.Equals(selectedId, row.id, StringComparison.Ordinal)
                ? selectedButtonStyle
                : buttonStyle;
            if (GUILayout.Button(label, rowStyle, GUILayout.Height(66))) LoadQuestion(row);
        }
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void DrawQuestionForm()
    {
        GUILayout.BeginVertical(panelStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        GUILayout.Label(string.IsNullOrEmpty(selectedId) ? "NEW QUESTION" : "EDIT QUESTION", headingStyle,
            GUILayout.Height(32));
        editorScroll = GUILayout.BeginScrollView(editorScroll, GUILayout.ExpandHeight(true));

        GUILayout.Label("Question ID  (generated automatically for new questions)", labelStyle);
        bool idEnabled = GUI.enabled;
        GUI.enabled = idEnabled && string.IsNullOrEmpty(selectedId);
        idInput = GUILayout.TextField(idInput ?? string.Empty, fieldStyle, GUILayout.Height(36));
        GUI.enabled = idEnabled;

        GUILayout.Space(8);
        GUILayout.Label("Programming language", labelStyle);
        int selectedLanguage = GUILayout.SelectionGrid(languageIndex, LanguageLabels, 3, toolbarStyle,
            GUILayout.Height(38));
        if (selectedLanguage != languageIndex) languageIndex = selectedLanguage;

        GUILayout.Space(8);
        GUILayout.Label("Difficulty  (selecting one sets the suggested level range below)", labelStyle);
        int selectedDifficulty = GUILayout.SelectionGrid(difficultyIndex, DifficultyLabels, 3, toolbarStyle,
            GUILayout.Height(38));
        if (selectedDifficulty != difficultyIndex)
        {
            difficultyIndex = selectedDifficulty;
            SetDefaultLevels();
        }

        GUILayout.Space(8);
        GUILayout.Label("Learning topic  (used in player progress reports)", labelStyle);
        topicInput = GUILayout.TextField(topicInput ?? "general", fieldStyle, GUILayout.Height(36));

        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical(GUILayout.Width(180));
        GUILayout.Label("Minimum level", labelStyle);
        minLevelInput = GUILayout.TextField(minLevelInput ?? "1", fieldStyle, GUILayout.Height(36));
        GUILayout.EndVertical();
        GUILayout.Space(14);
        GUILayout.BeginVertical(GUILayout.Width(180));
        GUILayout.Label("Maximum level  (blank = no limit)", labelStyle);
        maxLevelInput = GUILayout.TextField(maxLevelInput ?? string.Empty, fieldStyle, GUILayout.Height(36));
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        GUILayout.Label("Question format", labelStyle);
        questionTypeIndex = GUILayout.SelectionGrid(questionTypeIndex, QuestionTypeLabels, 3, toolbarStyle,
            GUILayout.Height(68));

        GUILayout.Space(8);
        GUILayout.Label("Question prompt", labelStyle);
        promptInput = GUILayout.TextArea(promptInput ?? string.Empty, areaStyle, GUILayout.Height(76));
        GUILayout.Label("Code snippet  (optional; include [___] or ___ for blanks)", labelStyle);
        codeInput = GUILayout.TextArea(codeInput ?? string.Empty, areaStyle, GUILayout.Height(92));

        GUILayout.Space(6);
        GUILayout.Label("Answer choices  (enter at least two; keep choices in A–D order)", labelStyle);
        for (int i = 0; i < choiceInputs.Length; i++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(((char)('A' + i)) + ".", mutedStyle, GUILayout.Width(28));
            choiceInputs[i] = GUILayout.TextField(choiceInputs[i] ?? string.Empty, fieldStyle, GUILayout.Height(34));
            GUILayout.EndHorizontal();
        }
        GUILayout.BeginHorizontal();
        GUILayout.Label("Correct answer", labelStyle, GUILayout.Width(145));
        int choiceCount = GetChoiceCount(out _);
        string[] correctLabels = { "A", "B", "C", "D" };
        correctIndex = Mathf.Clamp(correctIndex, 0, 3);
        correctIndex = GUILayout.SelectionGrid(correctIndex, correctLabels, 4, toolbarStyle,
            GUILayout.Height(34), GUILayout.Width(280));
        GUILayout.Label(choiceCount + " choice(s)", mutedStyle, GUILayout.Width(120));
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        GUILayout.Label("Explanation shown after answering  (optional)", labelStyle);
        explanationInput = GUILayout.TextArea(explanationInput ?? string.Empty, areaStyle, GUILayout.Height(64));
        publishedInput = GUILayout.Toggle(publishedInput, "Published  —  visible to players in the game", labelStyle);
        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal(GUILayout.Height(44));
        bool oldEnabled = GUI.enabled;
        GUI.enabled = oldEnabled && !requestInProgress;
        if (GUILayout.Button("SAVE QUESTION", buttonStyle, GUILayout.Width(190), GUILayout.Height(40)))
            StartCoroutine(SaveRoutine());
        if (!string.IsNullOrEmpty(selectedId) && GUILayout.Button("DELETE", dangerButtonStyle,
                GUILayout.Width(120), GUILayout.Height(40)))
            confirmDelete = true;
        GUI.enabled = oldEnabled;
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

    private GUIStyle dangerButtonStyle;

    private void EnsureStyles()
    {
        if (stylesReady) return;
        rootTexture = MakeTexture(Background);
        panelTexture = MakeTexture(Panel);
        inputTexture = MakeTexture(Input);
        buttonTexture = MakeTexture(new Color32(110, 78, 43, 255));
        selectedTexture = MakeTexture(new Color32(139, 101, 53, 255));
        dangerTexture = MakeTexture(new Color32(135, 59, 53, 255));
        dangerHoverTexture = MakeTexture(new Color32(164, 69, 61, 255));

        rootStyle = new GUIStyle { normal = { background = panelTexture } };
        panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = panelTexture, textColor = Text },
            padding = new RectOffset(16, 16, 12, 12),
            alignment = TextAnchor.UpperLeft
        };
        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 30, fontStyle = FontStyle.Bold, normal = { textColor = Gold },
            alignment = TextAnchor.MiddleLeft
        };
        headingStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 21, fontStyle = FontStyle.Bold, normal = { textColor = Gold },
            alignment = TextAnchor.MiddleLeft
        };
        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15, fontStyle = FontStyle.Bold, normal = { textColor = Text },
            wordWrap = true
        };
        mutedStyle = new GUIStyle(labelStyle)
        {
            fontSize = 13, fontStyle = FontStyle.Normal, normal = { textColor = Muted }
        };
        statusStyle = new GUIStyle(labelStyle)
        {
            fontSize = 14, fontStyle = FontStyle.Normal, normal = { textColor = Muted }
        };
        errorStatusStyle = new GUIStyle(statusStyle);
        errorStatusStyle.normal.textColor = new Color32(241, 137, 119, 255);
        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15, fontStyle = FontStyle.Bold, richText = true,
            alignment = TextAnchor.MiddleLeft, wordWrap = true,
            normal = { background = buttonTexture, textColor = Text },
            hover = { background = selectedTexture, textColor = Text },
            active = { background = selectedTexture, textColor = Text },
            padding = new RectOffset(12, 12, 6, 6)
        };
        selectedButtonStyle = new GUIStyle(buttonStyle)
        {
            normal = { background = selectedTexture, textColor = Text }
        };
        dangerButtonStyle = new GUIStyle(buttonStyle)
        {
            normal = { background = dangerTexture, textColor = Text },
            hover = { background = dangerHoverTexture, textColor = Text },
            active = { background = dangerHoverTexture, textColor = Text }
        };
        fieldStyle = new GUIStyle(GUI.skin.textField)
        {
            fontSize = 15, normal = { background = inputTexture, textColor = Text },
            focused = { background = inputTexture, textColor = Text },
            padding = new RectOffset(10, 10, 7, 7)
        };
        areaStyle = new GUIStyle(GUI.skin.textArea)
        {
            fontSize = 15, wordWrap = true,
            normal = { background = inputTexture, textColor = Text },
            focused = { background = inputTexture, textColor = Text },
            padding = new RectOffset(10, 10, 8, 8)
        };
        toolbarStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13, fontStyle = FontStyle.Bold,
            normal = { background = buttonTexture, textColor = Text },
            hover = { background = selectedTexture, textColor = Text },
            onNormal = { background = selectedTexture, textColor = Text },
            onHover = { background = selectedTexture, textColor = Text }
        };
        stylesReady = true;
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }

    private static Texture2D MakeTexture(Color32 color) { return MakeTexture((Color)color); }

    private void ResetForm()
    {
        selectedId = string.Empty;
        languageIndex = 0;
        difficultyIndex = 0;
        questionTypeIndex = 0;
        idInput = "java-custom-" + Guid.NewGuid().ToString("N").Substring(0, 12);
        promptInput = string.Empty;
        codeInput = string.Empty;
        topicInput = "general";
        explanationInput = string.Empty;
        correctIndex = 0;
        publishedInput = true;
        for (int i = 0; i < choiceInputs.Length; i++) choiceInputs[i] = string.Empty;
        SetDefaultLevels();
        editorScroll = Vector2.zero;
        SetStatus("New question. Fill in its prompt, answers, and level range, then save.", false);
    }

    private void SetDefaultLevels()
    {
        switch (difficultyIndex)
        {
            case 0: minLevelInput = "1"; maxLevelInput = "5"; break;
            case 1: minLevelInput = "6"; maxLevelInput = "15"; break;
            default: minLevelInput = "16"; maxLevelInput = string.Empty; break;
        }
    }

    private void LoadQuestion(QuestionRecord row)
    {
        if (row == null) return;
        selectedId = row.id ?? string.Empty;
        idInput = row.id ?? string.Empty;
        languageIndex = Mathf.Max(0, Array.IndexOf(Languages, row.language));
        difficultyIndex = Mathf.Max(0, Array.IndexOf(Difficulties, row.difficulty));
        questionTypeIndex = Mathf.Max(0, Array.IndexOf(QuestionTypes, row.question_type));
        topicInput = string.IsNullOrWhiteSpace(row.topic) ? "general" : row.topic;
        minLevelInput = row.min_level.ToString();
        maxLevelInput = row.max_level > 0 ? row.max_level.ToString() : string.Empty;
        promptInput = row.prompt ?? string.Empty;
        codeInput = row.code_snippet ?? string.Empty;
        explanationInput = row.explanation ?? string.Empty;
        correctIndex = Mathf.Clamp(row.correct_index, 0, 3);
        publishedInput = row.published;
        for (int i = 0; i < choiceInputs.Length; i++)
            choiceInputs[i] = row.choices != null && i < row.choices.Length ? row.choices[i] ?? string.Empty : string.Empty;
        editorScroll = Vector2.zero;
        SetStatus("Editing " + selectedId + ". Save changes to update the database row.", false);
    }

    private void BackToGame()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private void Refresh()
    {
        if (!requestInProgress) StartCoroutine(RefreshRoutine());
    }

    private IEnumerator RefreshRoutine()
    {
        if (!EnsureAdminConfiguration()) yield break;
        requestInProgress = true;
        SetStatus("Loading questions from Supabase…", false);
        string endpoint = BuildEndpoint();
        string url = endpoint + "?select=id,language,topic,difficulty,min_level,max_level,question_type,prompt,code_snippet,choices,correct_index,explanation,published&order=id.asc";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            SetHeaders(request);
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();
            if (!Succeeded(request))
            {
                SetStatus(FormatError("Could not load questions", request), true);
                requestInProgress = false;
                yield break;
            }

            try
            {
                QuestionRows result = JsonUtility.FromJson<QuestionRows>("{\"items\":" + request.downloadHandler.text + "}");
                questions.Clear();
                if (result != null && result.items != null) questions.AddRange(result.items);
                SetStatus("Connected. Loaded " + questions.Count + " question(s) from public." + TableName + ".", false);
            }
            catch (Exception exception)
            {
                SetStatus("Supabase returned question data in an unexpected format: " + exception.Message, true);
            }
        }
        requestInProgress = false;
    }

    private IEnumerator SaveRoutine()
    {
        if (requestInProgress || !EnsureAdminConfiguration()) yield break;
        if (!TryBuildJson(out string json)) yield break;

        bool creating = string.IsNullOrWhiteSpace(selectedId);
        string rowId = creating ? idInput.Trim() : selectedId;
        if (string.IsNullOrWhiteSpace(rowId))
        {
            SetStatus("Question ID cannot be blank.", true);
            yield break;
        }

        string url = creating ? BuildEndpoint() : BuildEndpoint() + "?id=eq." + UnityWebRequest.EscapeURL(selectedId);
        using (UnityWebRequest request = new UnityWebRequest(url, creating ? "POST" : "PATCH"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            SetHeaders(request);
            request.SetRequestHeader("Prefer", "return=representation");
            requestInProgress = true;
            SetStatus(creating ? "Adding question…" : "Saving question changes…", false);
            yield return request.SendWebRequest();
            if (!Succeeded(request))
            {
                SetStatus(FormatError("Save failed", request), true);
                requestInProgress = false;
                yield break;
            }
        }

        selectedId = rowId;
        requestInProgress = false;
        yield return RefreshRoutine();
        QuestionRecord saved = questions.Find(q => q != null && string.Equals(q.id, rowId, StringComparison.Ordinal));
        if (saved != null) LoadQuestion(saved);
        SetStatus(creating ? "Question created and list refreshed." : "Question updated and list refreshed.", false);
    }

    private IEnumerator DeleteRoutine()
    {
        if (requestInProgress || !EnsureAdminConfiguration()) yield break;
        if (string.IsNullOrWhiteSpace(selectedId))
        {
            SetStatus("Select a saved question before deleting it.", true);
            yield break;
        }

        string deletingId = selectedId;
        using (UnityWebRequest request = new UnityWebRequest(
                   BuildEndpoint() + "?id=eq." + UnityWebRequest.EscapeURL(deletingId), "DELETE"))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            SetHeaders(request);
            request.SetRequestHeader("Prefer", "return=minimal");
            requestInProgress = true;
            SetStatus("Deleting question…", false);
            yield return request.SendWebRequest();
            if (!Succeeded(request))
            {
                SetStatus(FormatError("Delete failed", request), true);
                requestInProgress = false;
                yield break;
            }
        }

        requestInProgress = false;
        ResetForm();
        yield return RefreshRoutine();
        SetStatus("Question deleted and list refreshed.", false);
    }

    private bool TryBuildJson(out string json)
    {
        json = string.Empty;
        string id = (idInput ?? string.Empty).Trim();
        string prompt = (promptInput ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(id)) { SetStatus("Question ID is required.", true); return false; }
        if (string.IsNullOrWhiteSpace(prompt)) { SetStatus("Enter the question prompt.", true); return false; }
        if (!int.TryParse(minLevelInput, out int minLevel) || minLevel < 1)
        {
            SetStatus("Minimum level must be a whole number greater than zero.", true);
            return false;
        }
        int maxLevel = 0;
        if (!string.IsNullOrWhiteSpace(maxLevelInput) &&
            (!int.TryParse(maxLevelInput, out maxLevel) || maxLevel < minLevel))
        {
            SetStatus("Maximum level must be blank (no limit) or at least the minimum level.", true);
            return false;
        }

        if (!GetChoiceCount(out List<string> choices, out bool hasGap) || hasGap)
        {
            SetStatus("Enter at least two choices, with no blank choice between filled choices.", true);
            return false;
        }
        if (correctIndex >= choices.Count)
        {
            SetStatus("The correct answer must point to one of the choices you entered.", true);
            return false;
        }

        StringBuilder builder = new StringBuilder(512);
        builder.Append('{');
        AppendJsonProperty(builder, "id", id, true);
        AppendJsonProperty(builder, "language", Languages[languageIndex], true);
        AppendJsonProperty(builder, "topic", string.IsNullOrWhiteSpace(topicInput) ? "general" : topicInput.Trim(), true);
        AppendJsonProperty(builder, "difficulty", Difficulties[difficultyIndex], true);
        builder.Append("\"min_level\":").Append(minLevel).Append(',');
        builder.Append("\"max_level\":").Append(maxLevel > 0 ? maxLevel.ToString() : "null").Append(',');
        AppendJsonProperty(builder, "question_type", QuestionTypes[questionTypeIndex], true);
        AppendJsonProperty(builder, "prompt", prompt, true);
        AppendJsonProperty(builder, "code_snippet", codeInput ?? string.Empty, true);
        builder.Append("\"choices\":[");
        for (int i = 0; i < choices.Count; i++)
        {
            if (i > 0) builder.Append(',');
            builder.Append('"').Append(EscapeJson(choices[i])).Append('"');
        }
        builder.Append("],\"correct_index\":").Append(correctIndex).Append(',');
        AppendJsonProperty(builder, "explanation", explanationInput ?? string.Empty, true);
        builder.Append("\"published\":").Append(publishedInput ? "true" : "false").Append('}');
        json = builder.ToString();
        return true;
    }

    private int GetChoiceCount(out List<string> choices)
    {
        GetChoiceCount(out choices, out bool gap);
        return gap ? -1 : choices.Count;
    }

    private bool GetChoiceCount(out List<string> choices, out bool hasGap)
    {
        choices = new List<string>();
        hasGap = false;
        bool sawBlank = false;
        for (int i = 0; i < choiceInputs.Length; i++)
        {
            string value = (choiceInputs[i] ?? string.Empty).Trim();
            if (value.Length == 0)
            {
                sawBlank = true;
                continue;
            }
            if (sawBlank) hasGap = true;
            choices.Add(value);
        }
        return choices.Count >= 2 && !hasGap;
    }

    private static void AppendJsonProperty(StringBuilder builder, string name, string value, bool comma)
    {
        builder.Append('"').Append(name).Append("\":\"").Append(EscapeJson(value ?? string.Empty)).Append('"');
        if (comma) builder.Append(',');
    }

    private static string EscapeJson(string value)
    {
        StringBuilder escaped = new StringBuilder(value.Length + 8);
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            switch (c)
            {
                case '"': escaped.Append("\\\""); break;
                case '\\': escaped.Append("\\\\"); break;
                case '\b': escaped.Append("\\b"); break;
                case '\f': escaped.Append("\\f"); break;
                case '\n': escaped.Append("\\n"); break;
                case '\r': escaped.Append("\\r"); break;
                case '\t': escaped.Append("\\t"); break;
                default:
                    if (c < 32) escaped.Append("\\u").Append(((int)c).ToString("x4"));
                    else escaped.Append(c);
                    break;
            }
        }
        return escaped.ToString();
    }

    private bool EnsureAdminConfiguration()
    {
        accessToken = MainMenuSaveSlots.CurrentAccessToken;
        if (!MainMenuSaveSlots.HasAdminSession)
        {
            SetStatus("Your admin session is missing or expired. Return to the Main Menu and sign in again.", true);
            return false;
        }
        if (string.IsNullOrWhiteSpace(projectUrl) || string.IsNullOrWhiteSpace(publishableKey))
        {
            SetStatus("Supabase project URL or publishable key is missing from the Main Menu configuration.", true);
            return false;
        }
        return true;
    }

    private string BuildEndpoint()
    {
        return projectUrl.Trim().TrimEnd('/') + "/rest/v1/" + TableName;
    }

    private void SetHeaders(UnityWebRequest request)
    {
        request.SetRequestHeader("apikey", publishableKey);
        request.SetRequestHeader("Authorization", "Bearer " + accessToken);
        request.SetRequestHeader("Accept", "application/json");
        request.SetRequestHeader("Content-Type", "application/json");
    }

    private static bool Succeeded(UnityWebRequest request)
    {
        return request.result == UnityWebRequest.Result.Success;
    }

    private static string FormatError(string prefix, UnityWebRequest request)
    {
        string body = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
        if (body.Length > 360) body = body.Substring(0, 360) + "…";
        return string.IsNullOrWhiteSpace(body)
            ? prefix + ": " + request.error + " (HTTP " + request.responseCode + ")."
            : prefix + ": HTTP " + request.responseCode + ". " + body;
    }

    private void SetStatus(string value, bool isError)
    {
        status = value;
        statusIsError = isError;
    }

    private static string EscapeRichText(string value)
    {
        return (value ?? string.Empty).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    private static string MakeTintRichText(string value, Color color)
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + EscapeRichText(value) + "</color>";
    }

    private void DrawDeleteConfirmation()
    {
        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(new Rect(0, 0, 1600, 900), rootTexture);
        GUI.color = oldColor;
        Rect dialog = new Rect(470, 345, 660, 220);
        GUI.Box(dialog, GUIContent.none, panelStyle);
        GUILayout.BeginArea(new Rect(dialog.x + 28, dialog.y + 24, dialog.width - 56, dialog.height - 48));
        GUILayout.Label("DELETE THIS QUESTION?", headingStyle);
        GUILayout.Label("This permanently removes it from the Supabase question bank.", labelStyle);
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("CANCEL", buttonStyle, GUILayout.Height(42))) confirmDelete = false;
        if (GUILayout.Button("DELETE QUESTION", dangerButtonStyle, GUILayout.Height(42)) && !requestInProgress)
        {
            confirmDelete = false;
            StartCoroutine(DeleteRoutine());
        }
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void OnDestroy()
    {
        if (rootTexture != null) Destroy(rootTexture);
        if (panelTexture != null) Destroy(panelTexture);
        if (inputTexture != null) Destroy(inputTexture);
        if (buttonTexture != null) Destroy(buttonTexture);
        if (selectedTexture != null) Destroy(selectedTexture);
        if (dangerTexture != null) Destroy(dangerTexture);
        if (dangerHoverTexture != null) Destroy(dangerHoverTexture);
    }
}
