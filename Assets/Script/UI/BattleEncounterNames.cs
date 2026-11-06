using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Introduces the actual battle roster as the arena fades into view.</summary>
public sealed class BattleEncounterNames : MonoBehaviour
{
    private Texture2D shadowTexture;
    private static readonly Regex LocationPrefix = new Regex(
        @"^(?:Forest of Dawn|Forest|Cave|Map \d+|Scene \d+)\s+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static void Show(IEnumerable<EnemyView> enemies)
    {
        var names = new List<string>();
        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.Data == null) continue;
            var name = LocationPrefix.Replace(enemy.Data.name.Trim(), "");
            if (!string.IsNullOrWhiteSpace(name)) names.Add(name);
        }
        if (names.Count == 0) return;

        var root = new GameObject("Battle Encounter Names", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(BattleEncounterNames));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 24;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        root.GetComponent<BattleEncounterNames>().Build(names);
    }

    private void Build(List<string> names)
    {
        var panel = new GameObject("Encounter Banner", typeof(RectTransform),
            typeof(CanvasGroup), typeof(RawImage));
        panel.transform.SetParent(transform, false);
        var rect = (RectTransform)panel.transform;
        rect.anchorMin = new Vector2(.19f, 1f);
        rect.anchorMax = new Vector2(.81f, 1f);
        rect.pivot = new Vector2(.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -105f);
        rect.sizeDelta = new Vector2(0f, 72f + Mathf.Min(names.Count, 6) * 57f);
        var backdrop = panel.GetComponent<RawImage>();
        backdrop.texture = shadowTexture = CreateShadowTexture();
        backdrop.color = new Color(.025f, .018f, .014f, .88f);
        backdrop.raycastTarget = false;

        var group = panel.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        AddText(rect, "ENCOUNTER", 25f, 1f, -49f, 12f, new Color(.93f, .74f, .39f));
        AddText(rect, string.Join("\n", names), 48f, 0f, 12f, 49f, new Color(1f, .93f, .78f));
        StartCoroutine(Reveal(group));
    }

    private static Texture2D CreateShadowTexture()
    {
        const int width = 96;
        const int height = 48;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "Feathered Encounter Shadow",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float horizontal = Mathf.Min(x, width - 1 - x) / 19f;
            float vertical = Mathf.Min(y, height - 1 - y) / 9f;
            float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Min(horizontal, vertical));
            pixels[y * width + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * alpha));
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private static void AddText(RectTransform panel, string value, float size,
        float lowerAnchor, float bottom, float top, Color color)
    {
        AddTextLayer(panel, value, size, lowerAnchor, bottom, top, new Color(0f, 0f, 0f, .9f),
            new Vector2(2f, -3f), "Text Shadow");
        AddTextLayer(panel, value, size, lowerAnchor, bottom, top, color, Vector2.zero, "Encounter Text");
    }

    private static void AddTextLayer(RectTransform panel, string value, float size,
        float lowerAnchor, float bottom, float top, Color color, Vector2 offset, string objectName)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(panel, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        var rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, lowerAnchor);
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(20f, bottom) + offset;
        rect.offsetMax = new Vector2(-20f, -top) + offset;
        text.text = value;
        text.richText = false;
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = 16f;
        text.fontSizeMax = size;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.raycastTarget = false;
    }

    private IEnumerator Reveal(CanvasGroup group)
    {
        const float entrance = .4f;
        for (float time = 0f; time < entrance || SceneTransitionFader.IsTransitioning;
             time += Time.unscaledDeltaTime)
        {
            group.alpha = GameControlsGuide.SettingsOpen ? 0f :
                Mathf.SmoothStep(0f, 1f, time / entrance);
            yield return null;
        }

        const float hold = 1.1f;
        const float fade = .55f;
        for (float time = 0f; time < hold + fade; time += Time.unscaledDeltaTime)
        {
            group.alpha = GameControlsGuide.SettingsOpen ? 0f :
                1f - Mathf.SmoothStep(0f, 1f, (time - hold) / fade);
            yield return null;
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (shadowTexture != null) Destroy(shadowTexture);
    }
}
