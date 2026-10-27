using System;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Runtime Supabase/PostgREST editor for the cards table. Project URL and
/// publishable key can be configured once in the Inspector. Auth reuses the
/// current game session; bearer tokens stay in memory.
/// </summary>
public class CardDataEditorScreen : MonoBehaviour
{
    private const string DefaultTableName = "cards";
    private static readonly Color BackgroundColor = new Color32(22, 27, 35, 255);
    private static readonly Color PanelColor = new Color32(36, 43, 53, 255);
    private static readonly Color InputColor = new Color32(27, 33, 42, 255);
    private static readonly Color GoldColor = new Color32(205, 164, 91, 255);
    private static readonly Color TextColor = new Color32(239, 232, 215, 255);
    private static readonly Color MutedTextColor = new Color32(168, 174, 181, 255);

    [Serializable]
    private class CardRecord
    {
        public string id;
        public string game_card_key;
        public string name;
        public string description;
        public int mana;
        public string image_url;
        public int damage_type;
        public int base_damage;
        public float percent_damage;
        public float damage_multiplier = 1f;
    }

    [Serializable]
    private class CardWriteRecord
    {
        public string game_card_key;
        public string name;
        public string description;
        public int mana;
        public string image_url;
        public int damage_type;
        public int base_damage;
        public float percent_damage;
        public float damage_multiplier;
    }

    [Serializable]
    private class CardListEnvelope
    {
        public CardRecord[] items;
    }

    [Header("Supabase Card Editor")]
    [SerializeField] private string supabaseProjectUrl = "";
    [SerializeField] private string supabasePublishableKey = "";

    private readonly System.Collections.Generic.List<CardRecord> cards = new();
    private string projectUrl = string.Empty;
    private string anonKey = string.Empty;
    private string accessToken = string.Empty;
    private string selectedId = string.Empty;
    private bool requestInProgress;
    private bool lastRefreshSucceeded;
    private Coroutine imagePreviewRoutine;
    private Texture2D previewTexture;
    private Sprite previewSprite;

    private TMP_InputField projectUrlInput;
    private TMP_InputField anonKeyInput;
    private TMP_InputField nameInput;
    private TMP_InputField gameCardKeyInput;
    private TMP_InputField descriptionInput;
    private TMP_InputField manaInput;
    private TMP_InputField damageTypeInput;
    private TMP_InputField baseDamageInput;
    private TMP_InputField percentDamageInput;
    private TMP_InputField multiplierInput;
    private TMP_InputField imageUrlInput;
    private TMP_Text statusText;
    private TMP_Text selectedIdText;
    private TMP_Text cardCountText;
    private TMP_Text previewPlaceholderText;
    private Image previewImage;
    private RectTransform cardsContent;
    private Button connectButton;
    private Button refreshButton;
    private Button saveButton;
    private Button newButton;
    private Button deleteButton;

    private void Start()
    {
        EnsureRenderCamera();
        BuildInterface();
        if (!string.IsNullOrWhiteSpace(MainMenuSaveSlots.CurrentAccessToken))
        {
            accessToken = MainMenuSaveSlots.CurrentAccessToken;
            SetStatus("Using the signed-in game account. Connecting…", false);
            StartCoroutine(RefreshRoutine());
        }
        else
        {
            SetStatus("Return to the Main Menu and sign in to the admin account before editing cards.", true);
        }
    }

    private void EnsureRenderCamera()
    {
        if (Camera.allCamerasCount > 0) return;

        var cameraObject = new GameObject("Card Data Editor Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Camera editorCamera = cameraObject.AddComponent<Camera>();
        editorCamera.clearFlags = CameraClearFlags.SolidColor;
        editorCamera.backgroundColor = BackgroundColor;
        editorCamera.orthographic = true;
        editorCamera.orthographicSize = 5f;
        editorCamera.nearClipPlane = 0.1f;
        editorCamera.farClipPlane = 100f;
    }

    private void BuildInterface()
    {
        EnsureEventSystem();
        var canvasObject = new GameObject("Card Data Editor Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform root = canvasObject.GetComponent<RectTransform>();
        Image backdrop = canvasObject.AddComponent<Image>();
        backdrop.color = BackgroundColor;
        backdrop.raycastTarget = true;

        AddText(root, "Title", "CARD DATA STUDIO", 34, FontStyles.Bold, TextAlignmentOptions.Left,
            new Vector2(30, -20), new Vector2(800, 48), GoldColor);
        AddText(root, "Subtitle", "Supabase card catalog editor  /  REST API", 17, FontStyles.Normal,
            TextAlignmentOptions.Left, new Vector2(32, -63), new Vector2(700, 28), MutedTextColor);
        AddButton(root, "BACK TO GAME", new Vector2(1370, -20), new Vector2(200, 44), BackToGame);

        projectUrlInput = AddInput(root, "Supabase project URL", "https://your-project.supabase.co",
            new Vector2(30, -102), new Vector2(420, 46));
        projectUrlInput.text = supabaseProjectUrl;
        anonKeyInput = AddInput(root, "Anon / publishable key", "Supabase public key",
            new Vector2(460, -102), new Vector2(700, 46));
        anonKeyInput.text = supabasePublishableKey;
        connectButton = AddButton(root, "Connect", new Vector2(1170, -102), new Vector2(170, 46), Connect);
        refreshButton = AddButton(root, "Refresh", new Vector2(1350, -102), new Vector2(220, 46), Refresh);

        RectTransform listPanel = AddPanel(root, "Cards List Panel", new Vector2(30, -170), new Vector2(430, 690));
        AddText(listPanel, "List Heading", "CARDS", 23, FontStyles.Bold, TextAlignmentOptions.Left,
            new Vector2(20, -18), new Vector2(270, 36), GoldColor);
        cardCountText = AddText(listPanel, "Card Count", "0 records", 15, FontStyles.Normal,
            TextAlignmentOptions.Right, new Vector2(290, -20), new Vector2(115, 30), MutedTextColor);
        BuildCardList(listPanel);
        newButton = AddButton(listPanel, "New Card", new Vector2(18, -632), new Vector2(394, 42), NewCard);

        RectTransform editPanel = AddPanel(root, "Card Edit Panel", new Vector2(480, -170), new Vector2(1090, 690));
        AddText(editPanel, "Editor Heading", "CARD DETAILS", 23, FontStyles.Bold, TextAlignmentOptions.Left,
            new Vector2(20, -18), new Vector2(400, 36), GoldColor);
        selectedIdText = AddText(editPanel, "Selected ID", "New card", 14, FontStyles.Normal,
            TextAlignmentOptions.Right, new Vector2(690, -20), new Vector2(375, 30), MutedTextColor);

        AddText(editPanel, "Name Label", "Name", 16, FontStyles.Bold, TextAlignmentOptions.Left,
            new Vector2(22, -66), new Vector2(420, 25), TextColor);
        nameInput = AddInput(editPanel, "Card name", "Card name", new Vector2(20, -94), new Vector2(430, 48));
        AddText(editPanel, "Game Card Key Label", "Inventory key (Unity catalog ID)", 16,
            FontStyles.Bold, TextAlignmentOptions.Left, new Vector2(480, -66), new Vector2(540, 25), TextColor);
        gameCardKeyInput = AddInput(editPanel, "Game Card Key", "Required to grant this card", new Vector2(478, -94), new Vector2(272, 48));

        AddText(editPanel, "Description Label", "Description", 16, FontStyles.Bold, TextAlignmentOptions.Left,
            new Vector2(22, -153), new Vector2(650, 25), TextColor);
        descriptionInput = AddInput(editPanel, "Card description", "Card effect and rules text",
            new Vector2(20, -181), new Vector2(710, 145), false, true);

        manaInput = AddLabeledInput(editPanel, "Mana", "Mana cost", new Vector2(20, -345), new Vector2(130, 52));
        damageTypeInput = AddLabeledInput(editPanel, "Damage type", "0 flat / 1 attack %",
            new Vector2(170, -345), new Vector2(190, 52));
        baseDamageInput = AddLabeledInput(editPanel, "Base damage", "Flat damage", new Vector2(380, -345), new Vector2(150, 52));
        percentDamageInput = AddLabeledInput(editPanel, "Attack %", "Percent", new Vector2(550, -345), new Vector2(150, 52));
        multiplierInput = AddLabeledInput(editPanel, "Multiplier", "1.0 = 100%", new Vector2(720, -345), new Vector2(150, 52));

        AddText(editPanel, "Image URL Label", "Image URL (public URL or Supabase Storage URL)", 16,
            FontStyles.Bold, TextAlignmentOptions.Left, new Vector2(22, -430), new Vector2(700, 25), TextColor);
        imageUrlInput = AddInput(editPanel, "Image URL", "https://...", new Vector2(20, -458), new Vector2(710, 48));
        imageUrlInput.onEndEdit.AddListener(LoadPreview);

        previewImage = AddImage(editPanel, "Card Image Preview", new Vector2(760, -70), new Vector2(290, 330),
            new Color32(28, 34, 42, 255));
        previewImage.preserveAspect = true;
        previewPlaceholderText = AddText(editPanel, "Image Placeholder", "IMAGE PREVIEW", 14, FontStyles.Bold,
            TextAlignmentOptions.Center, new Vector2(770, -198), new Vector2(270, 34), MutedTextColor);

        saveButton = AddButton(editPanel, "Save Changes", new Vector2(20, -555), new Vector2(190, 48), Save);
        deleteButton = AddButton(editPanel, "Delete", new Vector2(222, -555), new Vector2(140, 48), Delete);
        AddButton(editPanel, "Clear Form", new Vector2(372, -555), new Vector2(160, 48), NewCard);
        statusText = AddText(editPanel, "Status", "Enter your Supabase settings, then connect.", 15,
            FontStyles.Normal, TextAlignmentOptions.Left, new Vector2(20, -615), new Vector2(1010, 48), MutedTextColor);

        ClearEditor();
    }

    private void BuildCardList(RectTransform panel)
    {
        var viewportObject = new GameObject("Cards Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        viewport.SetParent(panel, false);
        SetTopLeftRect(viewport, new Vector2(18, -58), new Vector2(394, 548));
        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = InputColor;
        viewportImage.raycastTarget = true;
        viewportObject.GetComponent<Mask>().showMaskGraphic = false;

        var contentObject = new GameObject("Cards Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        cardsContent = contentObject.GetComponent<RectTransform>();
        cardsContent.SetParent(viewport, false);
        cardsContent.anchorMin = new Vector2(0, 1);
        cardsContent.anchorMax = new Vector2(1, 1);
        cardsContent.pivot = new Vector2(0.5f, 1);
        cardsContent.anchoredPosition = Vector2.zero;
        cardsContent.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 7;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = viewportObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = cardsContent;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 35f;
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

    }

    private RectTransform AddPanel(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetTopLeftRect(rect, position, size);
        obj.GetComponent<Image>().color = PanelColor;
        return rect;
    }

    private TMP_Text AddText(Transform parent, string name, string value, float size, FontStyles style,
        TextAlignmentOptions alignment, Vector2 position, Vector2 dimensions, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetTopLeftRect(rect, position, dimensions);
        TMP_Text text = obj.GetComponent<TMP_Text>();
        text.text = value;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    private Image AddImage(Transform parent, string name, Vector2 position, Vector2 dimensions, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetTopLeftRect(rect, position, dimensions);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private TMP_InputField AddInput(Transform parent, string name, string placeholder, Vector2 position,
        Vector2 dimensions, bool password = false, bool multiline = false)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetTopLeftRect(rect, position, dimensions);
        Image background = obj.GetComponent<Image>();
        background.color = InputColor;
        background.raycastTarget = true;

        var viewportObject = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        viewport.SetParent(rect, false);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(10, 4);
        viewport.offsetMax = new Vector2(-10, -4);

        TMP_Text valueText = AddText(viewport, "Value", string.Empty, 17, FontStyles.Normal,
            TextAlignmentOptions.Left, Vector2.zero, new Vector2(dimensions.x - 20, dimensions.y - 8), TextColor);
        TMP_Text placeholderText = AddText(viewport, "Placeholder", placeholder, 16, FontStyles.Normal,
            TextAlignmentOptions.Left, Vector2.zero, new Vector2(dimensions.x - 20, dimensions.y - 8), MutedTextColor);

        TMP_InputField input = obj.GetComponent<TMP_InputField>();
        input.targetGraphic = background;
        input.textComponent = valueText;
        input.textViewport = viewport;
        input.placeholder = placeholderText;
        input.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
        input.contentType = password ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
        input.richText = false;
        return input;
    }

    private TMP_InputField AddLabeledInput(Transform parent, string label, string placeholder, Vector2 position, Vector2 size)
    {
        AddText(parent, label + " Label", label, 15, FontStyles.Bold, TextAlignmentOptions.Left,
            new Vector2(position.x, position.y), new Vector2(size.x, 23), TextColor);
        return AddInput(parent, label + " Input", placeholder,
            new Vector2(position.x, position.y - 26), new Vector2(size.x, size.y));
    }

    private Button AddButton(Transform parent, string label, Vector2 position, Vector2 size, Action action)
    {
        var obj = new GameObject(label + " Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetTopLeftRect(rect, position, size);
        Image image = obj.GetComponent<Image>();
        image.color = label == "Delete" ? new Color32(135, 59, 53, 255) : new Color32(110, 78, 43, 255);
        Button button = obj.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => action?.Invoke());
        TMP_Text text = AddText(rect, "Label", label, 17, FontStyles.Bold, TextAlignmentOptions.Center,
            new Vector2(5, -3), new Vector2(size.x - 10, size.y - 6), TextColor);
        text.raycastTarget = false;
        return button;
    }

    private static void SetTopLeftRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(position.x, position.y);
        rect.sizeDelta = size;
    }

    private void Connect()
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            accessToken = MainMenuSaveSlots.CurrentAccessToken;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            SetStatus("Return to the Main Menu and sign in to the admin account before editing cards.", true);
            return;
        }
        StartCoroutine(RefreshRoutine());
    }

    private void BackToGame()
    {
        if (!Application.CanStreamedLevelBeLoaded("MainMenu"))
        {
            SetStatus("MainMenu scene is not included in Build Settings.", true);
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    private void Refresh() => StartCoroutine(RefreshRoutine());
    private void Save() => StartCoroutine(SaveRoutine());
    private void Delete() => StartCoroutine(DeleteRoutine());

    private IEnumerator RefreshRoutine()
    {
        if (requestInProgress) yield break;
        lastRefreshSucceeded = false;
        if (!TryBuildEndpoint(out string endpoint)) yield break;
        SetBusy(true, "Connecting and loading cards…");
        using (UnityWebRequest request = UnityWebRequest.Get(endpoint + "?select=*&order=name.asc"))
        {
            SetHeaders(request);
            yield return request.SendWebRequest();
            if (!Succeeded(request))
            {
                SetStatus(FormatRequestError("Could not load cards", request), true);
                SetBusy(false);
                yield break;
            }

            try
            {
                string wrapped = "{\"items\":" + request.downloadHandler.text + "}";
                CardListEnvelope result = JsonUtility.FromJson<CardListEnvelope>(wrapped);
                cards.Clear();
                if (result != null && result.items != null) cards.AddRange(result.items);
                RebuildCardList();
                lastRefreshSucceeded = true;
                SetStatus($"Connected. Loaded {cards.Count} card(s) from public.{DefaultTableName}.", false);
            }
            catch (Exception exception)
            {
                SetStatus("Supabase returned JSON that does not match the cards schema: " + exception.Message, true);
            }
        }
        SetBusy(false);
    }

    private IEnumerator SaveRoutine()
    {
        if (requestInProgress) yield break;
        if (!TryBuildEndpoint(out string endpoint)) yield break;
        if (!TryReadEditor(out CardWriteRecord record)) yield break;

        bool isNew = string.IsNullOrWhiteSpace(selectedId);
        string url = isNew ? endpoint : endpoint + "?id=eq." + UnityWebRequest.EscapeURL(selectedId);
        byte[] json = Encoding.UTF8.GetBytes(JsonUtility.ToJson(record));
        using (var request = new UnityWebRequest(url, isNew ? UnityWebRequest.kHttpVerbPOST : "PATCH"))
        {
            request.uploadHandler = new UploadHandlerRaw(json);
            request.downloadHandler = new DownloadHandlerBuffer();
            SetHeaders(request);
            request.SetRequestHeader("Prefer", "return=representation");
            SetBusy(true, isNew ? "Creating card…" : "Saving card changes…");
            yield return request.SendWebRequest();
            if (!Succeeded(request))
            {
                SetStatus(FormatRequestError("Save failed", request), true);
                SetBusy(false);
                yield break;
            }
        }

        SetBusy(false);
        yield return RefreshRoutine();
        if (lastRefreshSucceeded)
            SetStatus(isNew ? "Card created and list refreshed." : "Card updated and list refreshed.", false);
    }

    private IEnumerator DeleteRoutine()
    {
        if (requestInProgress) yield break;
        if (string.IsNullOrWhiteSpace(selectedId))
        {
            SetStatus("Select a saved card before deleting it.", true);
            yield break;
        }
        if (!TryBuildEndpoint(out string endpoint)) yield break;
        string url = endpoint + "?id=eq." + UnityWebRequest.EscapeURL(selectedId);
        using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbDELETE))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            SetHeaders(request);
            request.SetRequestHeader("Prefer", "return=minimal");
            SetBusy(true, "Deleting card…");
            yield return request.SendWebRequest();
            if (!Succeeded(request))
            {
                SetStatus(FormatRequestError("Delete failed", request), true);
                SetBusy(false);
                yield break;
            }
        }

        ClearEditor();
        SetBusy(false);
        yield return RefreshRoutine();
        if (lastRefreshSucceeded) SetStatus("Card deleted and list refreshed.", false);
    }

    private bool TryBuildEndpoint(out string endpoint)
    {
        endpoint = string.Empty;
        if (!TryBuildBaseUrl(out _))
        {
            endpoint = string.Empty;
            return false;
        }
        projectUrl = projectUrlInput.text.Trim().TrimEnd('/');
        anonKey = anonKeyInput.text.Trim();
        if (string.IsNullOrWhiteSpace(accessToken))
            accessToken = MainMenuSaveSlots.CurrentAccessToken;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            SetStatus("Sign in first so the editor can use an authenticated account for database access.", true);
            return false;
        }
        endpoint = projectUrl + "/rest/v1/" + DefaultTableName;
        return true;
    }

    private bool TryBuildBaseUrl(out string baseUrl)
    {
        baseUrl = string.Empty;
        projectUrl = projectUrlInput.text.Trim().TrimEnd('/');
        anonKey = anonKeyInput.text.Trim();
        if (!projectUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            !Uri.TryCreate(projectUrl, UriKind.Absolute, out Uri uri) ||
            !Regex.IsMatch(uri.Host, @"^[A-Za-z0-9.-]+$"))
        {
            SetStatus("Enter a valid HTTPS Supabase project URL.", true);
            return false;
        }
        if (string.IsNullOrWhiteSpace(anonKey))
        {
            SetStatus("Enter the Supabase anon / publishable key.", true);
            return false;
        }
        baseUrl = projectUrl;
        return true;
    }

    private void SetHeaders(UnityWebRequest request)
    {
        request.SetRequestHeader("apikey", anonKey);
        request.SetRequestHeader("Authorization", "Bearer " +
            (string.IsNullOrWhiteSpace(accessToken) ? anonKey : accessToken));
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "application/json");
    }

    private bool TryReadEditor(out CardWriteRecord record)
    {
        record = null;
        if (string.IsNullOrWhiteSpace(nameInput.text))
        {
            SetStatus("Card name is required.", true);
            return false;
        }
        if (string.IsNullOrWhiteSpace(gameCardKeyInput.text))
        {
            SetStatus("Inventory key is required. Use the matching ID from InventoryCardCatalog (or a chest./merchant. card key).", true);
            return false;
        }
        if (!int.TryParse(manaInput.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int mana) || mana < 0 ||
            !int.TryParse(damageTypeInput.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int damageType) || damageType < 0 || damageType > 1 ||
            !int.TryParse(baseDamageInput.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int baseDamage) || baseDamage < 0 ||
            !float.TryParse(percentDamageInput.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float percentDamage) || percentDamage < 0 ||
            !float.TryParse(multiplierInput.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float multiplier) || multiplier < 0)
        {
            SetStatus("Enter valid numbers. Damage type is 0 (flat) or 1 (attack percent).", true);
            return false;
        }

        record = new CardWriteRecord
        {
            game_card_key = gameCardKeyInput.text.Trim(),
            name = nameInput.text.Trim(),
            description = descriptionInput.text,
            mana = mana,
            image_url = imageUrlInput.text.Trim(),
            damage_type = damageType,
            base_damage = baseDamage,
            percent_damage = percentDamage,
            damage_multiplier = multiplier
        };
        return true;
    }

    private void RebuildCardList()
    {
        foreach (Transform child in cardsContent) Destroy(child.gameObject);
        foreach (CardRecord record in cards)
        {
            if (record == null) continue;
            var row = new GameObject("Card Row", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            RectTransform rect = row.GetComponent<RectTransform>();
            rect.SetParent(cardsContent, false);
            LayoutElement layout = row.GetComponent<LayoutElement>();
            layout.preferredHeight = 64;
            layout.minHeight = 64;
            Image image = row.GetComponent<Image>();
            image.color = PanelColor;
            Button button = row.GetComponent<Button>();
            button.targetGraphic = image;
            CardRecord selected = record;
            button.onClick.AddListener(() => LoadCard(selected));
            AddText(rect, "Card Name", string.IsNullOrEmpty(record.name) ? "Unnamed card" : record.name,
                17, FontStyles.Bold, TextAlignmentOptions.Left, new Vector2(12, -7), new Vector2(290, 28), TextColor);
            AddText(rect, "Card Summary", $"Mana {record.mana}   •   {record.description}", 13,
                FontStyles.Normal, TextAlignmentOptions.Left, new Vector2(12, -34), new Vector2(365, 24), MutedTextColor);
        }
        if (cardCountText != null) cardCountText.text = $"{cards.Count} records";
        LayoutRebuilder.ForceRebuildLayoutImmediate(cardsContent);
    }

    private void LoadCard(CardRecord record)
    {
        selectedId = record.id ?? string.Empty;
        selectedIdText.text = "ID: " + selectedId;
        gameCardKeyInput.text = record.game_card_key ?? string.Empty;
        nameInput.text = record.name ?? string.Empty;
        descriptionInput.text = record.description ?? string.Empty;
        manaInput.text = record.mana.ToString(CultureInfo.InvariantCulture);
        damageTypeInput.text = record.damage_type.ToString(CultureInfo.InvariantCulture);
        baseDamageInput.text = record.base_damage.ToString(CultureInfo.InvariantCulture);
        percentDamageInput.text = record.percent_damage.ToString(CultureInfo.InvariantCulture);
        multiplierInput.text = record.damage_multiplier.ToString(CultureInfo.InvariantCulture);
        imageUrlInput.text = record.image_url ?? string.Empty;
        LoadPreview(record.image_url);
        SetStatus("Editing “" + (record.name ?? "card") + "”. Save to update the Supabase row.", false);
    }

    private void NewCard()
    {
        ClearEditor();
        SetStatus("New card. Save to insert it into Supabase.", false);
    }

    private void ClearEditor()
    {
        selectedId = string.Empty;
        if (selectedIdText != null) selectedIdText.text = "New card";
        if (nameInput != null) nameInput.text = string.Empty;
        if (gameCardKeyInput != null) gameCardKeyInput.text = string.Empty;
        if (descriptionInput != null) descriptionInput.text = string.Empty;
        if (manaInput != null) manaInput.text = "0";
        if (damageTypeInput != null) damageTypeInput.text = "0";
        if (baseDamageInput != null) baseDamageInput.text = "10";
        if (percentDamageInput != null) percentDamageInput.text = "100";
        if (multiplierInput != null) multiplierInput.text = "1";
        if (imageUrlInput != null) imageUrlInput.text = string.Empty;
        if (previewImage != null) previewImage.sprite = null;
        if (previewPlaceholderText != null) previewPlaceholderText.gameObject.SetActive(true);
        if (imagePreviewRoutine != null) StopCoroutine(imagePreviewRoutine);
        ClearPreviewTexture();
    }

    private void LoadPreview(string url)
    {
        if (imagePreviewRoutine != null) StopCoroutine(imagePreviewRoutine);
        ClearPreviewTexture();
        if (string.IsNullOrWhiteSpace(url))
        {
            previewImage.sprite = null;
            previewPlaceholderText.gameObject.SetActive(true);
            return;
        }
        previewPlaceholderText.text = "LOADING IMAGE…";
        previewPlaceholderText.gameObject.SetActive(true);
        imagePreviewRoutine = StartCoroutine(LoadPreviewRoutine(url));
    }

    private IEnumerator LoadPreviewRoutine(string url)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();
            if (!Succeeded(request))
            {
                previewPlaceholderText.text = "IMAGE UNAVAILABLE";
                previewPlaceholderText.gameObject.SetActive(true);
                imagePreviewRoutine = null;
                yield break;
            }
            previewTexture = DownloadHandlerTexture.GetContent(request);
            previewSprite = Sprite.Create(previewTexture, new Rect(0, 0, previewTexture.width, previewTexture.height),
                new Vector2(0.5f, 0.5f));
            previewImage.sprite = previewSprite;
            previewPlaceholderText.gameObject.SetActive(false);
            imagePreviewRoutine = null;
        }
    }

    private void ClearPreviewTexture()
    {
        if (previewSprite != null) Destroy(previewSprite);
        if (previewTexture != null) Destroy(previewTexture);
        previewSprite = null;
        previewTexture = null;
    }

    private void SetBusy(bool busy, string message = null)
    {
        requestInProgress = busy;
        if (connectButton != null) connectButton.interactable = !busy;
        if (refreshButton != null) refreshButton.interactable = !busy;
        if (saveButton != null) saveButton.interactable = !busy;
        if (deleteButton != null) deleteButton.interactable = !busy;
        if (newButton != null) newButton.interactable = !busy;
        if (!string.IsNullOrEmpty(message)) SetStatus(message, false);
    }

    private void SetStatus(string message, bool error)
    {
        if (statusText == null) return;
        statusText.text = message;
        statusText.color = error ? new Color32(241, 137, 119, 255) : MutedTextColor;
    }

    private static bool Succeeded(UnityWebRequest request) => request.result == UnityWebRequest.Result.Success;

    private static string FormatRequestError(string prefix, UnityWebRequest request)
    {
        string body = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
        if (body.Length > 360) body = body.Substring(0, 360) + "…";
        return string.IsNullOrWhiteSpace(body)
            ? $"{prefix}: {request.error} (HTTP {request.responseCode})."
            : $"{prefix}: HTTP {request.responseCode}. {body}";
    }

    private void OnDestroy()
    {
        ClearPreviewTexture();
    }
}
