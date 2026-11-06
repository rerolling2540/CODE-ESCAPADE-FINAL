using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>The farewell and credits after the final victory report.</summary>
public sealed class FinalVictoryEnding : MonoBehaviour
{
    public static bool IsOpen { get; private set; }
    private static readonly Color Gold = new Color(.96f, .82f, .53f);
    private CanvasGroup farewell;
    private CanvasGroup credits;
    private float elapsed;
    private bool leaving;

    public static void Show()
    {
        if (IsOpen) return;
        IsOpen = true;
        var root = new GameObject("Final Victory Ending", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        root.AddComponent<FinalVictoryEnding>().Build();
    }

    private void Build()
    {
        var backdrop = new GameObject("Nightfall", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(transform, false);
        var rect = (RectTransform)backdrop.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        backdrop.GetComponent<Image>().color = new Color(.035f, .055f, .065f);

        farewell = Page("Farewell");
        Label(farewell.transform, "FOREST OF DAWN", 23, 120, Gold);
        Label(farewell.transform, "You made it out.", 64, 22, Gold);
        Label(farewell.transform, "The path ahead is yours.", 29, -70, Color.white);
        Label(farewell.transform, "ENTER or ESC to skip", 18, -180, Gold);

        credits = Page("Credits");
        Label(credits.transform, "END CREDITS", 23, 350, Gold);
        Label(credits.transform, "Code Escapade", 60, 277, Gold);
        Label(credits.transform, "GAME DESIGN & DEVELOPMENT", 22, 183, Gold);
        Label(credits.transform, "Code Escapade", 28, 145, Color.white);
        Label(credits.transform, "LEARNING CONTENT", 22, 76, Gold);
        Label(credits.transform, "The Scholar’s Almanac and coding challenges", 28, 38, Color.white);
        Label(credits.transform, "Java · C# · Python", 24, 0, Color.white);
        Label(credits.transform, "ARTWORK ACKNOWLEDGMENTS", 22, -65, Gold);
        Label(credits.transform, "Fantasy Swamp Forest — The Flavare", 26, -105, Color.white);
        Label(credits.transform, "Pixel Cave artwork — Szadi Art", 26, -141, Color.white);
        Label(credits.transform, "Built with Unity and TextMesh Pro", 23, -207, Gold);
        Label(credits.transform, "Thank you for playing and learning with us.", 27, -259, Color.white);
        var button = new GameObject("Return to Title", typeof(RectTransform), typeof(Image), typeof(Button));
        button.transform.SetParent(credits.transform, false);
        Position((RectTransform)button.transform, 320, 62, -342);
        var image = button.GetComponent<Image>();
        image.color = new Color(.30f, .21f, .11f);
        var control = button.GetComponent<Button>();
        control.targetGraphic = image;
        control.onClick.AddListener(ReturnToTitle);
        Label(button.transform, "Return to Title", 27, 0, Gold);
        ApplyFade();
    }

    private CanvasGroup Page(string name)
    {
        var page = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        page.transform.SetParent(transform, false);
        return page.GetComponent<CanvasGroup>();
    }

    private static void Label(Transform parent, string value, float size, float y, Color color)
    {
        var label = new GameObject(value, typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(parent, false);
        Position((RectTransform)label.transform, 1040, size * 1.7f, y);
        var text = label.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
    }

    private static void Position(RectTransform rect, float width, float height, float y)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(0, y);
    }

    private void Update()
    {
        if (leaving || GameControlsGuide.SettingsOpen) return;
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape))
        {
            if (elapsed >= 3.5f) ReturnToTitle();
            else elapsed = 3.5f;
        }
        elapsed += Time.unscaledDeltaTime;
        ApplyFade();
    }

    private void ApplyFade()
    {
        farewell.alpha = Mathf.Clamp01(elapsed / .6f) * (1f - Mathf.Clamp01((elapsed - 2.9f) / .6f));
        farewell.interactable = farewell.blocksRaycasts = false;
        credits.alpha = Mathf.Clamp01((elapsed - 3.5f) / .6f);
        credits.interactable = credits.blocksRaycasts = credits.alpha >= 1f;
    }

    private void ReturnToTitle()
    {
        if (leaving || GameControlsGuide.SettingsOpen) return;
        leaving = true;
        Time.timeScale = 1f;
        SceneTransitionFader.Load("MainMenu");
    }

    private void OnDestroy() => IsOpen = false;
}
