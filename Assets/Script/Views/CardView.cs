using UnityEngine;
using UnityEngine.Rendering;
using TMPro;
using System.Text.RegularExpressions;

public class CardView : MonoBehaviour
{
    private static readonly Regex EffectDescriptionPattern = new Regex(
        @"\(([^)]*)\)|\b\d+(?:\.\d+)?(?:%|\s+(?:damage|health|armor|mana|cards?|turns?))|\b(?:burn|bleed|frost|poison|nature|armor|mana|heal(?:ing)?|damage|draw)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description;
    [SerializeField] private TMP_Text mana;
    [SerializeField] private SpriteRenderer imageSR;
    [SerializeField] private GameObject wrapper;
    [SerializeField] private LayerMask dropLayer;
    [SerializeField] private Sprite cardFrame;
    public Card Card { get; private set; }
    private Vector3 draStartPosition;
    private Quaternion dragStartRotation;
    private SortingGroup dragSortingGroup;
    private int dragStartSortingOrder;
    private bool hasDragSortingOrder;
    private static Sprite medievalBackground;

    void Awake()
    {
        ApplyMedievalTheme();
    }

    void ApplyMedievalTheme()
    {
        var background = wrapper != null ? wrapper.transform.Find("CardBackground") : null;
        var renderer = background != null ? background.GetComponent<SpriteRenderer>() : null;
        if (renderer != null && cardFrame != null)
        {
            renderer.sprite = cardFrame;
            // Match the new tall frame to the existing card footprint.
            renderer.transform.localScale = new Vector3(0.74f, 0.65f, 1f);
            // The sample frame sprite uses a bottom-left pivot; center its bounds on the card.
            renderer.transform.localPosition = new Vector3(
                -cardFrame.bounds.center.x * renderer.transform.localScale.x,
                -cardFrame.bounds.center.y * renderer.transform.localScale.y,
                0f);
            renderer.color = Color.white;
            renderer.sortingOrder = -1;

            if (imageSR != null)
            {
                imageSR.sortingOrder = -2;
                Vector3 imagePosition = imageSR.transform.localPosition;
                imagePosition.y = 0.74f;
                imageSR.transform.localPosition = imagePosition;
            }

            if (title != null)
            {
                Vector2 titlePosition = title.rectTransform.anchoredPosition;
                titlePosition.y = -0.55f;
                title.rectTransform.anchoredPosition = titlePosition;
            }

            if (description != null)
            {
                Vector2 descriptionPosition = description.rectTransform.anchoredPosition;
                descriptionPosition.y = -1.28f;
                description.rectTransform.anchoredPosition = descriptionPosition;
            }

            if (mana != null)
            {
                Vector2 manaPosition = mana.rectTransform.anchoredPosition;
                manaPosition.x = -1.24f;
                manaPosition.y = 1.69f;
                mana.rectTransform.anchoredPosition = manaPosition;
            }
        }
        else if (renderer != null && renderer.sprite != null)
        {
            if (medievalBackground == null)
            {
                const int width = 300, height = 360;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Bilinear;
                var pixels = new Color32[width * height];
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        if (x < 57 || x > 243 || y < 57 || y > 303) continue;
                        int edge = Mathf.Min(Mathf.Min(x - 57, 243 - x), Mathf.Min(y - 57, 303 - y));
                        int grain = ((x * 17 + y * 31) % 9) - 4;
                        Color32 colour = new Color32((byte)(221 + grain), (byte)(201 + grain), (byte)(158 + grain), 255);
                        if (edge < 3) colour = new Color32(53, 30, 15, 255);
                        else if (edge < 6) colour = new Color32(183, 137, 61, 255);
                        else if (edge < 9) colour = new Color32(89, 55, 28, 255);
                        if (x > 67 && x < 233 && y > 184 && y < 290)
                            colour = new Color32(164, 146, 108, 255);
                        if (y >= 142 && y <= 175)
                            colour = y < 145 || y > 172 ? new Color32(134, 91, 35, 255) : new Color32(235, 214, 167, 255);
                        if ((x - 82) * (x - 82) + (y - 280) * (y - 280) < 20 * 20)
                            colour = new Color32(102, 63, 29, 255);
                        int cornerDistanceX = Mathf.Min(Mathf.Abs(x - 66), Mathf.Abs(x - 234));
                        int cornerDistanceY = Mathf.Min(Mathf.Abs(y - 66), Mathf.Abs(y - 294));
                        if (cornerDistanceX + cornerDistanceY < 5)
                            colour = new Color32(244, 204, 107, 255);
                        pixels[y * width + x] = colour;
                    }
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                medievalBackground = Sprite.Create(texture, new Rect(0, 0, width, height),
                    new Vector2(0.5f, 0.5f), width / renderer.sprite.bounds.size.x);
            }
            renderer.sprite = medievalBackground;
            renderer.color = Color.white;
        }
        foreach (var label in new[] { title, description, mana })
        {
            if (label == null) continue;
            label.color = new Color(0.2f, 0.12f, 0.055f);
            label.enableAutoSizing = true;
            label.horizontalAlignment = HorizontalAlignmentOptions.Center;
            label.verticalAlignment = VerticalAlignmentOptions.Middle;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Truncate;
            label.margin = new Vector4(1f, 0.5f, 1f, 0.5f);
            label.fontSizeMin = label == description ? 11 : label == title ? 12 : 18;
            label.fontSizeMax = label == description ? 18 : label == title ? 24 : 30;
        }
        if (title != null)
        {
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            title.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            title.fontStyle = FontStyles.Bold;
            title.rectTransform.sizeDelta = new Vector2(26, 3.8f);
            Vector2 titlePosition = title.rectTransform.anchoredPosition;
            titlePosition.x = 0f;
            titlePosition.y = -0.55f;
            title.rectTransform.anchoredPosition = titlePosition;
        }
        if (description != null)
        {
            description.richText = true;
            description.fontSizeMin = 15;
            description.fontSizeMax = 22;
            description.rectTransform.sizeDelta = new Vector2(25.5f, 8.5f);
            Vector2 descriptionPosition = description.rectTransform.anchoredPosition;
            descriptionPosition.x = 0f;
            descriptionPosition.y = -1.28f;
            description.rectTransform.anchoredPosition = descriptionPosition;
        }
        if (mana != null)
        {
            mana.rectTransform.anchorMin = mana.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            mana.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            mana.rectTransform.sizeDelta = new Vector2(5.6f, 4.6f);
            mana.fontSizeMin = 26;
            mana.fontSizeMax = 36;
            mana.color = new Color(0.28f, 0.78f, 1f);
        }
    }

    public static string FormatEffectDescription(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return EffectDescriptionPattern.Replace(text, match =>
        {
            string value = match.Groups[1].Success ? match.Groups[1].Value : match.Value;
            string lower = value.ToLowerInvariant();
            string color = lower.Contains("frost") ? "#53BFFF" :
                lower.Contains("poison") || lower.Contains("nature") ? "#67C96B" :
                lower.Contains("burn") || lower.Contains("fire") ? "#FF7043" :
                lower.Contains("bleed") ? "#F04A54" :
                lower.Contains("heal") || lower.Contains("health") ? "#63C878" :
                lower.Contains("mana") || lower.Contains("armor") ? "#53BFFF" : "#F04A54";
            string highlighted = $"<color={color}>{value}</color>";
            return match.Groups[1].Success ? $"({highlighted})" : highlighted;
        });
    }

    public void Setup(Card card)
    {
        Card = card;
        title.text = card.Title;
        description.text = FormatEffectDescription(card.Description);
        mana.text = card.Mana.ToString();
        imageSR.sprite = card.Image;
        if (imageSR.sprite != null)
        {
            // Fit both small legacy icons and full-size 500FreeSkillIcons art to
            // the card frame's illustration window. Large source sprites scale
            // down by their bounds; small legacy sprites retain the 6x cap.
            float sourceSize = Mathf.Max(imageSR.sprite.bounds.size.x, imageSR.sprite.bounds.size.y);
            float iconScale = sourceSize > 0.001f ? Mathf.Min(6f, 2.2f / sourceSize) : 1f;
            imageSR.transform.localScale = Vector3.one * iconScale;
        }
    }

    public void RestoreHoverVisual()
    {
        if (wrapper != null) wrapper.SetActive(true);
    }

    void OnMouseEnter()
    {
        if (MatchSetupSystem.StarfallCutsceneActive) return;
        var interactions = Interactions.Instance;
        var hoverSystem = CardViewHoverSystem.Instance;
        if (interactions == null || !interactions.PlayerCanHover() || hoverSystem == null || Card == null) return;
        if (wrapper != null) wrapper.SetActive(false);
        Vector3 pos = new(transform.position.x, -2, 0);
        hoverSystem.Show(Card, pos, this);
    }

    void OnMouseExit()
    {
        // Hover callbacks can run while scene objects are being enabled/disabled.
        // Either reference may be unavailable during scene transitions or on cards
        // that are not connected to the hover preview.
        CardViewHoverSystem.Instance?.Hide();
        if (wrapper != null) wrapper.SetActive(true);
    }

    void OnMouseDown()
    {
        if (!Interactions.Instance.PlayerCanInteract()) return;
        if (!MouseUtil.TryGetMousePositionInWorldSpace(out Vector3 mousePosition, -1)) return;
        if (Card.RequiresManualTarget)
        {
            ManualTargetSystem.Instance.StartTargeting(transform.position);
        }
        else
        {
            Interactions.Instance.PlayerIsDragging = true;
            wrapper.SetActive(true);
            CardViewHoverSystem.Instance.Hide();
            draStartPosition = transform.position;
            dragStartRotation = transform.rotation;
            // Enemy health bars use a high sorting order. Lift the complete card
            // group above them while dragging so the card remains readable.
            dragSortingGroup = GetComponent<SortingGroup>();
            if (dragSortingGroup != null)
            {
                dragStartSortingOrder = dragSortingGroup.sortingOrder;
                hasDragSortingOrder = true;
                dragSortingGroup.sortingOrder = 120;
            }
            transform.rotation = Quaternion.Euler(0, 0, 0);
            transform.position = mousePosition;
        }
    }

    void OnMouseDrag()
    {
        if (!Interactions.Instance.PlayerCanInteract()) return;
        if (Card.RequiresManualTarget) return;
        if (MouseUtil.TryGetMousePositionInWorldSpace(out Vector3 mousePosition, -1))
            transform.position = mousePosition;
    }

    void OnMouseUp()
    {
        RestoreDragSortingOrder();
        if (!Interactions.Instance.PlayerCanInteract()) return;
        if (!MouseUtil.TryGetMousePositionInWorldSpace(out Vector3 mousePosition, -1))
        {
            if (Card.RequiresManualTarget) ManualTargetSystem.Instance.CancelTargeting();
            else
            {
                transform.position = draStartPosition;
                transform.rotation = dragStartRotation;
            }
            Interactions.Instance.PlayerIsDragging = false;
            return;
        }
        if (Card.RequiresManualTarget)
        {
            EnemyView target = ManualTargetSystem.Instance.EndTargeting(mousePosition);
            if (target != null && ManaSystem.Instance.HasEnoughMana(Card.Mana))
            {
                BattleAudio.Play(BattleSound.CardActivate);
                PlayCardGA playCard = new(Card, target);
                QuestionSystem.Instance.AskRandomQuestion(Card, correct =>
                {
                    playCard.QuestionAnsweredCorrectly = correct;
                    ActionSystem.Instance.Perform(playCard);
                });
            }
        }
        else
        {
            if (ManaSystem.Instance.HasEnoughMana(Card.Mana)
            && Physics.Raycast(transform.position, Vector3.forward, out RaycastHit hit, 10f, dropLayer))
            {
                BattleAudio.Play(BattleSound.CardActivate);
                PlayCardGA playCardGA = new(Card);

                QuestionSystem.Instance.AskRandomQuestion(Card, correct =>
                {
                    playCardGA.QuestionAnsweredCorrectly = correct;
                    ActionSystem.Instance.Perform(playCardGA);
                });
            }
            else
            {
                transform.position = draStartPosition;
                transform.rotation = dragStartRotation;
            }
            Interactions.Instance.PlayerIsDragging = false;
        }
    }

    private void RestoreDragSortingOrder()
    {
        if (!hasDragSortingOrder) return;
        if (dragSortingGroup != null) dragSortingGroup.sortingOrder = dragStartSortingOrder;
        hasDragSortingOrder = false;
        dragSortingGroup = null;
    }
}
