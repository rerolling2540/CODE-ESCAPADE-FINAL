using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Visual, read-only card tile used by the Battle Codex inventory page.</summary>
public sealed class CardInventoryItemUI : MonoBehaviour
{
    [Header("Card artwork")]
    public Image frame;
    public Image icon;

    [Header("Card labels")]
    public TMP_Text manaLabel;
    public TMP_Text titleLabel;
    public TMP_Text descriptionLabel;
    public TMP_Text ownershipLabel;

    private void Awake()
    {
        ConfigureLabels();
    }

    public void Bind(CardData card, int quantity, int level)
    {
        if (card == null) return;
        ConfigureLabels();
        if (frame != null) frame.preserveAspect = true;
        if (icon != null)
        {
            icon.sprite = card.Image;
            icon.enabled = card.Image != null;
            icon.preserveAspect = true;
        }
        // The frame already has a dedicated cost medallion. Keep only the
        // numeric cost in it so the value cannot wrap or clip into "ANA".
        if (manaLabel != null) manaLabel.text = card.Mana.ToString();
        if (titleLabel != null) titleLabel.text = card.name;
        if (descriptionLabel != null) descriptionLabel.text = string.IsNullOrWhiteSpace(card.Description)
            ? "No description available."
            : card.Description.Trim();
        if (ownershipLabel != null)
            ownershipLabel.text = "OWNED ×" + Mathf.Max(1, quantity) + "  ·  LV " + Mathf.Max(1, level);
    }

    private void ConfigureLabels()
    {
        ConfigureFrameLayout();

        if (manaLabel != null)
        {
            SetTopLeft(manaLabel.rectTransform, new Vector2(7f, -5f), new Vector2(42f, 36f));
            manaLabel.alignment = TextAlignmentOptions.Center;
            manaLabel.verticalAlignment = VerticalAlignmentOptions.Middle;
            manaLabel.enableAutoSizing = false;
            manaLabel.fontSize = 22f;
            manaLabel.margin = Vector4.zero;
        }

        if (titleLabel != null)
        {
            SetTopLeft(titleLabel.rectTransform, new Vector2(18f, -162f), new Vector2(174f, 30f));
            titleLabel.alignment = TextAlignmentOptions.Center;
            titleLabel.verticalAlignment = VerticalAlignmentOptions.Middle;
            titleLabel.enableAutoSizing = true;
            titleLabel.fontSizeMin = 10f;
            titleLabel.fontSizeMax = 16f;
            titleLabel.textWrappingMode = TextWrappingModes.NoWrap;
            titleLabel.margin = Vector4.zero;
        }

        if (descriptionLabel != null)
        {
            SetTopLeft(descriptionLabel.rectTransform, new Vector2(16f, -198f), new Vector2(178f, 54f));
            descriptionLabel.alignment = TextAlignmentOptions.Center;
            descriptionLabel.textWrappingMode = TextWrappingModes.Normal;
            descriptionLabel.enableAutoSizing = true;
            descriptionLabel.fontSizeMin = 10f;
            descriptionLabel.fontSizeMax = 14f;
            descriptionLabel.margin = new Vector4(3f, 2f, 3f, 2f);
        }

        if (ownershipLabel != null)
        {
            SetTopLeft(ownershipLabel.rectTransform, new Vector2(4f, -303f), new Vector2(202f, 23f));
            ownershipLabel.alignment = TextAlignmentOptions.Center;
            ownershipLabel.verticalAlignment = VerticalAlignmentOptions.Middle;
            ownershipLabel.color = new Color(0.16f, 0.085f, 0.035f, 1f);
            ownershipLabel.enableAutoSizing = true;
            ownershipLabel.fontSizeMin = 10f;
            ownershipLabel.fontSizeMax = 13f;
            ownershipLabel.margin = Vector4.zero;
        }
    }

    private void ConfigureFrameLayout()
    {
        RectTransform rootRect = transform as RectTransform;
        if (rootRect == null) return;

        // Older scene templates stored the frame Image on the card root, which
        // paints over labels in some Unity UI hierarchies. Move it to a child
        // so the frame and the ownership line can have separate bounds.
        if (frame != null && frame.gameObject == gameObject)
        {
            var frameObject = new GameObject("Card Frame", typeof(RectTransform), typeof(Image));
            frameObject.transform.SetParent(transform, false);
            frameObject.transform.SetAsFirstSibling();
            RectTransform frameRect = (RectTransform)frameObject.transform;
            SetTopLeft(frameRect, Vector2.zero, new Vector2(210f, 300f));

            Image frameImage = frameObject.GetComponent<Image>();
            frameImage.sprite = frame.sprite;
            frameImage.color = frame.color;
            frameImage.material = frame.material;
            frameImage.type = frame.type;
            frameImage.preserveAspect = frame.preserveAspect;
            frameImage.fillCenter = frame.fillCenter;
            frameImage.fillAmount = frame.fillAmount;
            frameImage.raycastTarget = false;
            frame.enabled = false;
            frame.raycastTarget = false;
            frame = frameImage;
        }

        GridLayoutGroup grid = GetComponentInParent<GridLayoutGroup>();
        if (grid != null)
            grid.cellSize = new Vector2(210f, 330f);
        else
            rootRect.sizeDelta = new Vector2(210f, 330f);
    }

    private static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
