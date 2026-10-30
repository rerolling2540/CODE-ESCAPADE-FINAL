using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>Creates the battle codex as saved, editable objects in Battle Scene.</summary>
public static class BattleInfoPanelAuthoring
{
    private const string ScenePath = "Assets/Scenes/Battle Scene.unity";
    private static readonly Color Ink = new Color(0.12f, 0.075f, 0.045f, 1f);
    private static readonly Color Brass = new Color(0.78f, 0.55f, 0.23f, 1f);
    private static readonly Color Parchment = new Color(0.88f, 0.79f, 0.60f, 1f);
    private const string BronzeCardFramePath = "Assets/free_fantasy_card_frames_deckbuilder_ui_sampler/Frames/512x720/action_card_bronze.png";

    [MenuItem("Code Escapade/Repair Battle Settings Button")]
    public static void RepairSettingsButton()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform settingsCanvas = FindInScene(scene, "Battle Settings Canvas");
        Transform buttonTransform = settingsCanvas != null ? settingsCanvas.Find("setting") : null;
        Transform panelTransform = settingsCanvas != null ? settingsCanvas.Find("Panel") : null;
        if (buttonTransform == null || panelTransform == null)
            throw new System.InvalidOperationException("The Battle Settings Canvas must contain the setting button and Panel objects.");

        if (buttonTransform.GetComponent<Button>() == null)
            buttonTransform.gameObject.AddComponent<Button>();
        if (buttonTransform.GetComponent<BattleSettingsMenu>() == null)
            Undo.AddComponent<BattleSettingsMenu>(buttonTransform.gameObject);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Battle settings button now has a saved BattleSettingsMenu component and toggles the reused settings panel.");
    }

    [MenuItem("Code Escapade/Author Battle Character and Enemy Info")]
    public static void AuthorPanel()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform portrait = FindInScene(scene, "PortraitFrame");
        if (portrait == null) throw new System.InvalidOperationException("PortraitFrame was not found in Battle Scene.");

        Transform oldCanvas = FindInScene(scene, "Battle Info Canvas");
        if (oldCanvas != null) Object.DestroyImmediate(oldCanvas.gameObject);

        int order = 0;
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            order = Mathf.Max(order, canvas.sortingOrder + 1);

        GameObject canvasObject = new GameObject("Battle Info Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(canvasObject, scene);
        Canvas canvasComponent = canvasObject.GetComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasComponent.overrideSorting = true;
        canvasComponent.sortingOrder = order;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject modal = CreateRect("Battle Info Modal", canvasObject.transform, Vector2.zero, Vector2.zero, true);
        Stretch((RectTransform)modal.transform);
        CanvasGroup group = modal.AddComponent<CanvasGroup>();
        group.interactable = true;
        group.blocksRaycasts = true;
        group.ignoreParentGroups = false;
        GameObject backdropObject = CreateRect("Dismiss Backdrop", modal.transform, Vector2.zero, Vector2.zero, true);
        Stretch((RectTransform)backdropObject.transform);
        Image backdrop = backdropObject.AddComponent<Image>();
        backdrop.color = new Color(0.015f, 0.012f, 0.01f, 0.78f);
        backdrop.raycastTarget = true;
        Button backdropButton = backdropObject.AddComponent<Button>();
        ConfigureButton(backdropButton, backdrop);

        GameObject window = CreateRect("Battle Info Window", modal.transform, Vector2.zero, new Vector2(1320f, 790f), false);
        Center((RectTransform)window.transform, Vector2.zero, new Vector2(1320f, 790f));
        AddImage(window, Ink, true);
        GameObject trim = CreateRect("Brass Frame", window.transform, Vector2.zero, new Vector2(1304f, 774f), false);
        Center((RectTransform)trim.transform, Vector2.zero, new Vector2(1304f, 774f));
        AddImage(trim, Brass, true);
        GameObject paper = CreateRect("Parchment Panel", trim.transform, Vector2.zero, new Vector2(1288f, 758f), false);
        Center((RectTransform)paper.transform, Vector2.zero, new Vector2(1288f, 758f));
        AddImage(paper, Parchment, true);

        TMP_Text banner = CreateText("Panel Title", paper.transform, "BATTLE CODEX", 32f, new Color(1f, 0.87f, 0.59f, 1f), FontStyles.Bold);
        SetTopLeft(banner.rectTransform, new Vector2(34f, -27f), new Vector2(650f, 52f));
        TMP_Text subtitle = CreateText("Panel Subtitle", paper.transform, "CHARACTER  /  CARDS  /  ENEMIES", 16f, new Color(0.94f, 0.86f, 0.71f, 1f), FontStyles.Normal);
        SetTopLeft(subtitle.rectTransform, new Vector2(36f, -76f), new Vector2(650f, 28f));

        Button closeButton = CreateButton("Close Button", paper.transform, "CLOSE  [ESC]", new Vector2(1168f, -30f), new Vector2(96f, 46f), new Color(0.34f, 0.19f, 0.09f, 1f));

        Button[] tabs = new Button[3];
        Image[] tabImages = new Image[3];
        string[] tabNames = { "CHARACTER INFO", "CARDS", "ENEMY DETAILS" };
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i] = CreateButton("Tab - " + tabNames[i], paper.transform, tabNames[i],
                new Vector2(28f, -145f - i * 100f), new Vector2(238f, 82f), new Color(0.25f, 0.15f, 0.08f, 1f));
            tabImages[i] = tabs[i].GetComponent<Image>();
        }

        TMP_Text pageHeading = CreateText("Content Heading", paper.transform, "CHARACTER INFORMATION", 27f, Ink, FontStyles.Bold);
        pageHeading.alignment = TextAlignmentOptions.Left;
        SetTopLeft(pageHeading.rectTransform, new Vector2(298f, -112f), new Vector2(940f, 48f));

        GameObject portraitBox = CreateRect("Character Portrait Panel", paper.transform, Vector2.zero, new Vector2(220f, 490f), false);
        SetTopLeft((RectTransform)portraitBox.transform, new Vector2(298f, -174f), new Vector2(220f, 490f));
        AddImage(portraitBox, new Color(0.22f, 0.14f, 0.08f, 1f), true);
        GameObject portraitGold = CreateRect("Portrait Brass Border", portraitBox.transform, Vector2.zero, new Vector2(208f, 478f), false);
        Center((RectTransform)portraitGold.transform, Vector2.zero, new Vector2(208f, 478f));
        AddImage(portraitGold, Brass, true);
        GameObject portraitPaper = CreateRect("Portrait Parchment", portraitGold.transform, Vector2.zero, new Vector2(198f, 468f), false);
        Center((RectTransform)portraitPaper.transform, Vector2.zero, new Vector2(198f, 468f));
        AddImage(portraitPaper, Parchment, true);
        GameObject portraitImageObject = CreateRect("Character Portrait", portraitPaper.transform, Vector2.zero, new Vector2(180f, 420f), false);
        Center((RectTransform)portraitImageObject.transform, Vector2.zero, new Vector2(180f, 420f));
        Image portraitImage = portraitImageObject.AddComponent<Image>();
        portraitImage.preserveAspect = true;
        portraitImage.raycastTarget = false;

        GameObject viewportObject = CreateRect("Info Viewport", paper.transform, Vector2.zero, Vector2.zero, false);
        SetTopLeft((RectTransform)viewportObject.transform, new Vector2(550f, -174f), new Vector2(680f, 490f));
        Image viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = new Color(0.93f, 0.86f, 0.70f, 0.95f);
        viewportImage.raycastTarget = true;
        viewportObject.AddComponent<RectMask2D>();
        ScrollRect scroll = viewportObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        GameObject contentObject = CreateRect("Info Content", viewportObject.transform, Vector2.zero, new Vector2(0f, 490f), false);
        RectTransform contentRect = (RectTransform)contentObject.transform;
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 490f);
        TMP_Text body = CreateText("Info Text", contentObject.transform, "", 22f, Ink, FontStyles.Normal);
        body.alignment = TextAlignmentOptions.TopLeft;
        body.textWrappingMode = TextWrappingModes.Normal;
        body.margin = Vector4.zero;
        RectTransform bodyRect = body.rectTransform;
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyRect.offsetMin = new Vector2(26f, 22f);
        bodyRect.offsetMax = new Vector2(-26f, -22f);
        scroll.viewport = (RectTransform)viewportObject.transform;
        scroll.content = contentRect;

        GameObject gridObject = CreateRect("Card Grid Content", viewportObject.transform, Vector2.zero, new Vector2(0f, 490f), false);
        RectTransform gridRect = (RectTransform)gridObject.transform;
        gridRect.anchorMin = new Vector2(0f, 1f);
        gridRect.anchorMax = new Vector2(1f, 1f);
        gridRect.pivot = new Vector2(0.5f, 1f);
        gridRect.anchoredPosition = Vector2.zero;
        gridRect.sizeDelta = new Vector2(0f, 490f);
        GridLayoutGroup cardGrid = gridObject.AddComponent<GridLayoutGroup>();
        cardGrid.padding = new RectOffset(18, 18, 18, 18);
        cardGrid.cellSize = new Vector2(210f, 300f);
        cardGrid.spacing = new Vector2(15f, 16f);
        cardGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        cardGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
        cardGrid.childAlignment = TextAnchor.UpperLeft;
        cardGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        cardGrid.constraintCount = 4;
        ContentSizeFitter gridFitter = gridObject.AddComponent<ContentSizeFitter>();
        gridFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        gridFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text emptyCards = CreateText("Card Inventory Empty Message", viewportObject.transform,
            "This player's inventory has no available cards.", 24f, Ink, FontStyles.Bold);
        emptyCards.alignment = TextAlignmentOptions.Center;
        emptyCards.enableAutoSizing = true;
        emptyCards.fontSizeMin = 16f;
        emptyCards.fontSizeMax = 24f;
        Center(emptyCards.rectTransform, Vector2.zero, new Vector2(820f, 100f));
        emptyCards.gameObject.SetActive(false);

        CardInventoryItemUI cardTemplate = CreateCardItemTemplate(paper.transform);

        BattleInfoPanel panel = modal.AddComponent<BattleInfoPanel>();
        panel.panelGroup = group;
        panel.backdropButton = backdropButton;
        panel.closeButton = closeButton;
        panel.tabButtons = tabs;
        panel.tabBackgrounds = tabImages;
        panel.pageHeading = pageHeading;
        panel.bodyText = body;
        panel.bodyScroll = scroll;
        panel.bodyContent = contentRect;
        panel.bodyViewport = (RectTransform)viewportObject.transform;
        panel.cardGridContent = gridRect;
        panel.cardItemTemplate = cardTemplate;
        panel.cardEmptyState = emptyCards;
        panel.portraitContainer = portraitBox;
        panel.characterPortrait = portraitImage;

        Button portraitButton = portrait.GetComponent<Button>();
        if (portraitButton == null) portraitButton = portrait.gameObject.AddComponent<Button>();
        Image portraitFrame = portrait.GetComponent<Image>();
        portraitButton.targetGraphic = portraitFrame;
        portraitButton.transition = Selectable.Transition.None;
        portraitButton.navigation = new Navigation { mode = Navigation.Mode.None };
        if (portraitFrame != null) portraitFrame.raycastTarget = true;
        BattleInfoPortraitButton portraitHook = portrait.GetComponent<BattleInfoPortraitButton>();
        if (portraitHook == null) portraitHook = portrait.gameObject.AddComponent<BattleInfoPortraitButton>();
        SerializedObject hookSerialized = new SerializedObject(portraitHook);
        hookSerialized.FindProperty("panel").objectReferenceValue = panel;
        hookSerialized.ApplyModifiedPropertiesWithoutUndo();

        modal.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Battle character and enemy info panel authored under Battle Info Canvas. Click PortraitFrame to open it during battle.");
    }

    private static CardInventoryItemUI CreateCardItemTemplate(Transform parent)
    {
        GameObject card = CreateRect("Card Item Template (edit styles)", parent, Vector2.zero, new Vector2(210f, 300f), false);
        Center((RectTransform)card.transform, Vector2.zero, new Vector2(210f, 300f));
        Image frame = card.AddComponent<Image>();
        frame.sprite = LoadSpriteAsset(BronzeCardFramePath, "action_card_bronze_0");
        frame.type = Image.Type.Simple;
        frame.preserveAspect = true;
        frame.raycastTarget = false;

        Image icon = CreateCardImage("Card Icon", card.transform, new Vector2(54f, -52f), new Vector2(102f, 104f), Color.white);
        icon.preserveAspect = true;

        TMP_Text mana = CreateText("Card Mana", card.transform, "1", 25f, new Color(1f, 0.9f, 0.62f, 1f), FontStyles.Bold);
        mana.alignment = TextAlignmentOptions.Center;
        SetTopLeft(mana.rectTransform, new Vector2(8f, -5f), new Vector2(38f, 34f));

        TMP_Text title = CreateText("Card Title", card.transform, "Card Name", 20f, Ink, FontStyles.Bold);
        title.alignment = TextAlignmentOptions.Center;
        title.enableAutoSizing = true;
        title.fontSizeMin = 13f;
        title.fontSizeMax = 20f;
        SetTopLeft(title.rectTransform, new Vector2(18f, -164f), new Vector2(174f, 32f));

        TMP_Text description = CreateText("Card Description", card.transform, "Card effect description", 16f, Ink, FontStyles.Normal);
        description.alignment = TextAlignmentOptions.Center;
        description.enableAutoSizing = true;
        description.fontSizeMin = 11f;
        description.fontSizeMax = 16f;
        description.margin = new Vector4(3f, 2f, 3f, 2f);
        SetTopLeft(description.rectTransform, new Vector2(18f, -201f), new Vector2(174f, 59f));

        TMP_Text ownership = CreateText("Card Ownership", card.transform, "OWNED ×1  ·  LV 1", 13f, new Color(1f, 0.9f, 0.65f, 1f), FontStyles.Bold);
        ownership.alignment = TextAlignmentOptions.Center;
        ownership.enableAutoSizing = true;
        ownership.fontSizeMin = 10f;
        ownership.fontSizeMax = 13f;
        SetTopLeft(ownership.rectTransform, new Vector2(18f, -265f), new Vector2(174f, 22f));

        CardInventoryItemUI presenter = card.AddComponent<CardInventoryItemUI>();
        presenter.frame = frame;
        presenter.icon = icon;
        presenter.manaLabel = mana;
        presenter.titleLabel = title;
        presenter.descriptionLabel = description;
        presenter.ownershipLabel = ownership;
        card.SetActive(false);
        return presenter;
    }

    private static Image CreateCardImage(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject imageObject = CreateRect(name, parent, position, size, false);
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Sprite LoadSpriteAsset(string assetPath, string spriteName)
    {
        Sprite direct = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (direct != null) return direct;
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            if (asset is Sprite sprite && sprite.name == spriteName) return sprite;
        Debug.LogWarning("Could not load the bronze card frame sprite at " + assetPath + ". The card tiles will use their fallback tint until the asset is reimported.");
        return null;
    }

    private static Button CreateButton(string name, Transform parent, string text, Vector2 position, Vector2 size, Color color)
    {
        GameObject buttonObject = CreateRect(name, parent, position, size, false);
        Image image = buttonObject.AddComponent<Image>();
        image.color = color;
        Button button = buttonObject.AddComponent<Button>();
        ConfigureButton(button, image);
        TMP_Text label = CreateText("Label", buttonObject.transform, text, 18f, new Color(1f, 0.88f, 0.62f, 1f), FontStyles.Bold);
        label.alignment = TextAlignmentOptions.Center;
        Stretch((RectTransform)label.transform);
        return button;
    }

    private static void ConfigureButton(Button button, Graphic target)
    {
        button.targetGraphic = target;
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, float size, Color color, FontStyles style)
    {
        GameObject textObject = CreateRect(name, parent, Vector2.zero, Vector2.zero, false);
        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 position, Vector2 size, bool stretch)
    {
        GameObject item = new GameObject(name, typeof(RectTransform));
        item.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)item.transform;
        if (stretch) Stretch(rect);
        else SetTopLeft(rect, position, size);
        return item;
    }

    private static Image AddImage(GameObject item, Color color, bool raycast)
    {
        Image image = item.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void Center(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static Transform FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform result = FindRecursive(root.transform, name);
            if (result != null) return result;
        }
        return null;
    }

    private static Transform FindRecursive(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            Transform result = FindRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
