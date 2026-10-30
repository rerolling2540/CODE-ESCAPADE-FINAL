using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Authors the battle HUD controls into Battle Scene so they remain editable between play sessions.</summary>
public static class BattleHudSceneAuthoring
{
    private const string ScenePath = "Assets/Scenes/Battle Scene.unity";

    [MenuItem("Code Escapade/Apply Reference Player HUD Layout")]
    public static void ApplyReferencePlayerHudLayout()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform hud = FindInScene(scene, "PlaayerHUD");
        if (hud == null) throw new System.InvalidOperationException("PlaayerHUD was not found in Battle Scene.");

        AuthorPortrait(hud);
        AuthorHealth(hud);
        AuthorManaAndStatus(hud);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Reference player HUD layout saved to Battle Scene. Edit Player Health Bar, PortraitFrame, ManaUI, and Player Status Effects under Canvas/PlaayerHUD.");
    }

    [MenuItem("Code Escapade/Author Battle HUD to Scene")]
    public static void AuthorBattleHudToScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform hud = FindInScene(scene, "PlaayerHUD");
        if (hud == null) throw new System.InvalidOperationException("PlaayerHUD was not found in Battle Scene.");
        RectTransform hudRoot = hud as RectTransform;
        hudRoot.anchorMin = hudRoot.anchorMax = new Vector2(0f, 1f);
        hudRoot.pivot = new Vector2(0f, 1f);
        hudRoot.anchoredPosition = new Vector2(16f, -14f);
        hudRoot.sizeDelta = new Vector2(456f, 118f);
        Canvas mainCanvas = hud.GetComponentInParent<Canvas>();
        if (mainCanvas == null) throw new System.InvalidOperationException("PlaayerHUD has no parent Canvas.");

        AuthorPortrait(hud);
        AuthorHealth(hud);
        AuthorManaAndStatus(hud);
        AuthorEpHud(mainCanvas.transform);
        AuthorSettings(scene);
        AuthorPile("DrawPilePoint", true);
        AuthorPile("DiscardPilePoint", false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Battle HUD scene objects are authored and saved. Edit them under Canvas, PlaayerHUD, Battle Settings Canvas, and the pile points in the Hierarchy.");
    }

    private static void AuthorPortrait(Transform hud)
    {
        Transform frame = FindChildRecursive(hud, "PortraitFrame");
        Transform icon = FindChildRecursive(hud, "PortraitIcon");
        if (frame == null || icon == null) return;
        RectTransform frameRect = frame as RectTransform;
        frame.SetParent(hud, false);
        frame.gameObject.SetActive(true);
        SetTopLeft(frameRect, new Vector2(0f, -40f), new Vector2(74f, 72f));
        Image frameImage = frame.GetComponent<Image>();
        if (frameImage != null) frameImage.raycastTarget = frame.GetComponent<Button>() != null;
        if (frame.GetComponent<RectMask2D>() == null) frame.gameObject.AddComponent<RectMask2D>();

        RectTransform iconRect = icon as RectTransform;
        icon.SetParent(frame, false);
        icon.gameObject.SetActive(true);
        SetCenter(iconRect, Vector2.zero, new Vector2(66f, 66f));
        Image iconImage = icon.GetComponent<Image>();
        if (iconImage != null) { iconImage.preserveAspect = true; iconImage.raycastTarget = false; }
    }

    private static void AuthorHealth(Transform hud)
    {
        GameObject barObject = GetOrCreate("Player Health Bar", hud, typeof(Slider));
        RectTransform barRect = (RectTransform)barObject.transform;
        barObject.SetActive(true);
        // Match the reference: a wide health meter above the portrait row.
        SetTopLeft(barRect, new Vector2(0f, -4f), new Vector2(340f, 28f));
        Slider slider = barObject.GetComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 100f;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;

        GameObject backgroundObject = GetOrCreate("Background", barObject.transform, typeof(Image));
        RectTransform backgroundRect = (RectTransform)backgroundObject.transform;
        Stretch(backgroundRect, Vector2.zero, Vector2.one);
        Image background = backgroundObject.GetComponent<Image>();
        background.color = new Color(0.12f, 0.2f, 0.12f, 1f);
        background.raycastTarget = false;

        GameObject fillAreaObject = GetOrCreate("Fill Area", barObject.transform);
        RectTransform fillAreaRect = (RectTransform)fillAreaObject.transform;
        Stretch(fillAreaRect, new Vector2(0.025f, 0.12f), new Vector2(0.975f, 0.88f));
        GameObject fillObject = GetOrCreate("Fill", fillAreaObject.transform, typeof(Image));
        RectTransform fillRect = (RectTransform)fillObject.transform;
        Stretch(fillRect, Vector2.zero, Vector2.one);
        Image fill = fillObject.GetComponent<Image>();
        fill.color = new Color(0.25f, 0.78f, 0.28f, 1f);
        fill.raycastTarget = false;
        slider.fillRect = fillRect;
        slider.targetGraphic = background;

        Transform existingLabel = FindChildRecursive(hud, "Player Health Text");
        GameObject textObject = existingLabel != null
            ? existingLabel.gameObject
            : GetOrCreate("Player Health Text", barObject.transform, typeof(TextMeshProUGUI));
        textObject.transform.SetParent(barObject.transform, false);
        RectTransform textRect = (RectTransform)textObject.transform;
        Stretch(textRect, Vector2.zero, Vector2.one);
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        TMP_Text label = textObject.GetComponent<TMP_Text>();
        label.text = "HP: 400 / 400";
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = 20f;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(1f, 0.9f, 0.68f, 1f);
        label.alignment = TextAlignmentOptions.Center;
        label.horizontalAlignment = HorizontalAlignmentOptions.Center;
        label.verticalAlignment = VerticalAlignmentOptions.Middle;
        label.enableAutoSizing = true;
        label.fontSizeMin = 12f;
        label.fontSizeMax = 18f;
        label.margin = Vector4.zero;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
    }

    private static void AuthorManaAndStatus(Transform hud)
    {
        Transform mana = FindSceneObject("ManaUI");
        if (mana != null)
        {
            RectTransform rect = mana as RectTransform;
            mana.SetParent(hud, false);
            SetTopLeft(rect, new Vector2(88f, -48f), new Vector2(140f, 28f));
            int index = 0;
            foreach (RectTransform child in rect)
            {
                if (child.GetComponent<Image>() == null) continue;
                SetTopLeft(child, new Vector2(index * 23f, -4f), new Vector2(20f, 20f));
                index++;
            }
        }

        GameObject status = GetOrCreate("Player Status Effects", hud);
        SetTopLeft((RectTransform)status.transform, new Vector2(88f, -96f), new Vector2(220f, 32f));
        if (status.GetComponent<HorizontalLayoutGroup>() == null)
        {
            HorizontalLayoutGroup layout = status.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 6f;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        }
    }

    private static void AuthorEpHud(Transform canvas)
    {
        GameObject meter = GetOrCreate("EP Star Meter", canvas);
        RectTransform meterRect = (RectTransform)meter.transform;
        meterRect.anchorMin = meterRect.anchorMax = Vector2.zero;
        meterRect.pivot = Vector2.zero;
        meterRect.anchoredPosition = new Vector2(8f, 14f);
        meterRect.sizeDelta = new Vector2(84f, 320f);

        for (int i = 0; i < 5; i++)
        {
            Vector2 pos = new Vector2(38f, 16f + i * 52f);
            Image empty = GetImage($"EP Star {i + 1} Empty", meter.transform);
            SetBottomLeft(empty.rectTransform, pos, new Vector2(48f, 48f));
            empty.color = new Color(0.35f, 0.75f, 1f, 0.82f);
            empty.preserveAspect = true;

            Image fill = GetImage($"EP Star {i + 1} Fill", meter.transform);
            SetBottomLeft(fill.rectTransform, pos, new Vector2(41f, 41f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Vertical;
            fill.fillOrigin = (int)Image.OriginVertical.Bottom;
            fill.fillAmount = 0f;
            fill.color = new Color(0.18f, 0.68f, 1f, 1f);
            fill.preserveAspect = true;
        }

        Image backing = GetImage("EP Counter Background", meter.transform);
        SetBottomLeft(backing.rectTransform, new Vector2(2f, 280f), new Vector2(72f, 34f));
        backing.color = new Color(0.12f, 0.075f, 0.035f, 1f);
        Outline outline = backing.GetComponent<Outline>() ?? backing.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.35f, 0.78f, 1f, 0.95f);
        outline.effectDistance = new Vector2(2f, -2f);

        GameObject labelObject = GetOrCreate("EP Label", meter.transform, typeof(TextMeshProUGUI));
        SetBottomLeft((RectTransform)labelObject.transform, new Vector2(2f, 280f), new Vector2(72f, 34f));
        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        label.text = "EP 0/5";
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = 16f;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(0.72f, 0.93f, 1f, 1f);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        GameObject card = GetOrCreate("EP Gold Skill Card", canvas, typeof(Image), typeof(Button), typeof(CanvasGroup), typeof(Outline));
        RectTransform cardRect = (RectTransform)card.transform;
        cardRect.anchorMin = cardRect.anchorMax = Vector2.zero;
        cardRect.pivot = Vector2.zero;
        cardRect.anchoredPosition = new Vector2(98f, 100f);
        cardRect.sizeDelta = new Vector2(116f, 154f);
        Image cardImage = card.GetComponent<Image>();
        cardImage.color = new Color(0.92f, 0.76f, 0.5f, 1f);
        cardImage.raycastTarget = true;
        Button button = card.GetComponent<Button>();
        button.targetGraphic = cardImage;
        button.transition = Selectable.Transition.None;
        CanvasGroup group = card.GetComponent<CanvasGroup>();
        group.alpha = 0.34f;
        group.interactable = true;
        group.blocksRaycasts = true;
        group.ignoreParentGroups = false;
        Outline cardOutline = card.GetComponent<Outline>();
        cardOutline.effectColor = new Color(0.72f, 0.52f, 0.22f, 1f);
        cardOutline.effectDistance = new Vector2(2f, -2f);
        AddText(card.transform, "EP Skill Title", "STARFALL", new Vector2(0f, -7f), new Vector2(106f, 24f), 15f, TextAlignmentOptions.Center, true);
        Image skillIcon = GetImage("EP Skill Icon", card.transform);
        SetCenter(skillIcon.rectTransform, new Vector2(0f, 8f), new Vector2(42f, 42f));
        skillIcon.preserveAspect = true;
        AddText(card.transform, "EP Skill Description", "Deal 2x ATK\nto all enemies.", new Vector2(0f, 31f), new Vector2(106f, 42f), 12f, TextAlignmentOptions.Center, false);
    }

    private static void AuthorSettings(Scene scene)
    {
        Transform panel = FindInScene(scene, "Panel");
        Transform button = FindInScene(scene, "setting");
        if (panel == null || button == null) return;

        GameObject canvasObject = GetOrCreate("Battle Settings Canvas", null, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject blocker = GetOrCreate("Settings Pause Input Blocker", canvasObject.transform, typeof(Image));
        RectTransform blockerRect = (RectTransform)blocker.transform;
        Stretch(blockerRect, Vector2.zero, Vector2.one);
        Image blockerImage = blocker.GetComponent<Image>();
        blockerImage.color = new Color(0f, 0f, 0f, 0.56f);
        blockerImage.raycastTarget = true;
        blocker.transform.SetAsFirstSibling();
        blocker.SetActive(false);

        panel.SetParent(canvasObject.transform, false);
        panel.SetAsLastSibling();
        if (panel.gameObject.activeSelf) panel.gameObject.SetActive(false);
        button.SetParent(canvasObject.transform, false);
        RectTransform buttonRect = button as RectTransform;
        buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(1f, 1f);
        buttonRect.anchoredPosition = new Vector2(-30f, -26f);
        if (buttonRect.sizeDelta == Vector2.zero) buttonRect.sizeDelta = new Vector2(112f, 96f);
        button.SetAsLastSibling();
        if (button.GetComponent<BattleSettingsMenu>() == null) button.gameObject.AddComponent<BattleSettingsMenu>();

        Image rootImage = button.GetComponent<Image>();
        if (rootImage != null) { rootImage.color = new Color(0.67f, 0.42f, 0.13f, 1f); rootImage.raycastTarget = true; }
        GameObject inner = GetOrCreate("Menu Icon Background", button, typeof(Image));
        Stretch((RectTransform)inner.transform, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));
        inner.GetComponent<Image>().color = new Color(0.12f, 0.075f, 0.035f, 1f);
        for (int i = 0; i < 3; i++)
        {
            GameObject line = GetOrCreate("Menu Icon Line " + (i + 1), inner.transform, typeof(Image));
            RectTransform lineRect = (RectTransform)line.transform;
            lineRect.anchorMin = lineRect.anchorMax = new Vector2(0.5f, 0.5f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);
            lineRect.sizeDelta = new Vector2(36f, 5f);
            lineRect.anchoredPosition = new Vector2(0f, (1 - i) * 12f);
            line.GetComponent<Image>().color = new Color(1f, 0.82f, 0.42f, 1f);
        }

        GameObject turn = GetOrCreate("Battle Turn Counter", canvasObject.transform, typeof(Image), typeof(Button));
        RectTransform turnRect = (RectTransform)turn.transform;
        turnRect.anchorMin = turnRect.anchorMax = new Vector2(1f, 1f);
        turnRect.pivot = new Vector2(1f, 1f);
        turnRect.sizeDelta = new Vector2(132f, 54f);
        turnRect.anchoredPosition = new Vector2(-152f, -47f);
        turn.GetComponent<Image>().color = new Color(0.67f, 0.42f, 0.13f, 1f);
        Button turnButton = turn.GetComponent<Button>();
        turnButton.interactable = false;
        turnButton.transition = Selectable.Transition.None;
        GameObject turnBg = GetOrCreate("Turn Counter Background", turn.transform, typeof(Image));
        Stretch((RectTransform)turnBg.transform, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f));
        turnBg.GetComponent<Image>().color = new Color(0.12f, 0.075f, 0.035f, 1f);
        AddText(turn.transform, "Turn Count", "TURN 1", Vector2.zero, new Vector2(124f, 46f), 20f, TextAlignmentOptions.Center, false);
    }

    private static void AuthorPile(string pointName, bool isDraw)
    {
        Transform point = FindSceneObject(pointName);
        if (point == null) return;
        Camera camera = Camera.main;
        if (camera == null) camera = Object.FindFirstObjectByType<Camera>();
        if (camera != null)
        {
            float distance = Mathf.Abs(camera.transform.position.z);
            Vector3 p = isDraw
                ? camera.ViewportToWorldPoint(new Vector3(0.03f, 0.38f, distance))
                : camera.ViewportToWorldPoint(new Vector3(0.955f, 0.20f, distance));
            p.z = 0f;
            point.position = p;
        }
        point.name = isDraw ? "DrawPilePoint" : "DiscardPilePoint";
        Transform display = point.Find(isDraw ? "Draw Pile Display" : "Discard Pile Display");
        if (display == null)
        {
            GameObject displayObject = new GameObject(isDraw ? "Draw Pile Display" : "Discard Pile Display");
            displayObject.transform.SetParent(point, false);
            displayObject.transform.localPosition = Vector3.zero;
            displayObject.transform.localScale = Vector3.one * (isDraw ? 0.095f : 0.12f);
            display = displayObject.transform;
        }
        // Use Undo.AddComponent explicitly in the editor. The pile display can
        // already contain CardPileView but no renderer, and the null-coalescing
        // GetComponent/AddComponent expression was leaving that authored object
        // without its renderer in this scene-authoring pass.
        SpriteRenderer renderer = display.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = Undo.AddComponent<SpriteRenderer>(display.gameObject);
        if (renderer == null)
            throw new System.InvalidOperationException($"Could not add SpriteRenderer to {display.name}.");
        Sprite icon = Resources.Load<Sprite>(isDraw ? "PileIcons/DrawPile" : "PileIcons/DiscardPile");
        if (icon != null) renderer.sprite = icon;
        renderer.sortingOrder = 40;
        BoxCollider collider = display.GetComponent<BoxCollider>();
        if (collider == null) collider = Undo.AddComponent<BoxCollider>(display.gameObject);
        if (collider == null)
            throw new System.InvalidOperationException($"Could not add BoxCollider to {display.name}.");
        if (renderer.sprite != null) collider.size = renderer.sprite.bounds.size * 1.1f;
        if (display.GetComponent<CardPileView>() == null) display.gameObject.AddComponent<CardPileView>();
    }

    private static void AddText(Transform parent, string name, string value, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions alignment, bool top)
    {
        GameObject go = GetOrCreate(name, parent, typeof(TextMeshProUGUI));
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = top ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0.5f);
        rect.pivot = top ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        TMP_Text text = go.GetComponent<TMP_Text>();
        text.text = value;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(1f, 0.9f, 0.68f, 1f);
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
    }

    private static Image GetImage(string name, Transform parent)
    {
        GameObject go = GetOrCreate(name, parent, typeof(Image));
        Image image = go.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private static GameObject GetOrCreate(string name, Transform parent, params System.Type[] components)
    {
        Transform found = parent != null ? parent.Find(name) : FindSceneObject(name);
        if (found != null) return found.gameObject;
        System.Type[] types = new System.Type[components.Length + 1];
        types[0] = typeof(RectTransform);
        for (int i = 0; i < components.Length; i++) types[i + 1] = components[i];
        GameObject go = new GameObject(name, types);
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    private static Transform FindSceneObject(string name)
    {
        Scene scene = SceneManager.GetActiveScene();
        return FindInScene(scene, name);
    }

    private static Transform FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform result = FindChildRecursive(root.transform, name);
            if (result != null) return result;
        }
        return null;
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildRecursive(root.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }

    private static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetBottomLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetCenter(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
