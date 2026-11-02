using System;
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

    public CardData BoundCard { get; private set; }
    public int BoundQuantity { get; private set; }
    public int BoundLevel { get; private set; }
    public event Action<CardInventoryItemUI> Clicked;
    private Button clickButton;

    private void Awake()
    {
        ConfigureLabels();
        ConfigureClick();
    }

    public void Bind(CardData card, int quantity, int level)
    {
        if (card == null) return;
        BoundCard = card;
        BoundQuantity = quantity;
        BoundLevel = level;
        ConfigureLabels();
        ConfigureClick();
        // The frame sprite is authored for a tall card. Match the card's exact
        // rect so a few transparent edge pixels don't expose gaps around it.
        if (frame != null) frame.preserveAspect = false;
        if (icon != null)
        {
            icon.sprite = card.Image;
            icon.enabled = card.Image != null;
            // Fill the illustration cutout. The skill icons are square while
            // the frame window is slightly wider, so preserving the full
            // square would leave visible strips of empty space at the sides.
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }
        // The frame already has a dedicated cost medallion. Keep only the
        // numeric cost in it so the value cannot wrap or clip into "ANA".
        if (manaLabel != null) manaLabel.text = card.Mana.ToString();
        if (titleLabel != null) titleLabel.text = card.name;
        if (descriptionLabel != null) descriptionLabel.text = string.IsNullOrWhiteSpace(card.Description)
            ? "No description available."
            : CardView.FormatEffectDescription(card.Description.Trim());
        if (ownershipLabel != null)
            ownershipLabel.text = "OWNED ×" + Mathf.Max(1, quantity) + "  ·  LV " + Mathf.Max(1, level);
    }

    public void SetClickEnabled(bool enabled)
    {
        ConfigureClick();
        if (clickButton != null) clickButton.interactable = enabled;
    }

    private void ConfigureClick()
    {
        clickButton = GetComponent<Button>();
        if (clickButton == null) clickButton = gameObject.AddComponent<Button>();
        clickButton.transition = Selectable.Transition.None;
        Image rootImage = GetComponent<Image>();
        if (rootImage != null)
        {
            rootImage.color = new Color(1f, 1f, 1f, 0f);
            rootImage.enabled = true;
            rootImage.raycastTarget = true;
            clickButton.targetGraphic = rootImage;
        }
        clickButton.onClick.RemoveListener(HandleClick);
        clickButton.onClick.AddListener(HandleClick);
    }

    private void HandleClick() => Clicked?.Invoke(this);

    private void ConfigureLabels()
    {
        ConfigureFrameLayout();

        if (manaLabel != null)
        {
            // Center the cost over the circular medallion in the gold frame.
            SetTopLeft(manaLabel.rectTransform, new Vector2(18f, -19f), new Vector2(38f, 38f));
            manaLabel.alignment = TextAlignmentOptions.Center;
            manaLabel.verticalAlignment = VerticalAlignmentOptions.Middle;
            manaLabel.color = new Color(0.22f, 0.72f, 1f, 1f);
            manaLabel.enableAutoSizing = true;
            manaLabel.fontSizeMin = 13f;
            manaLabel.fontSizeMax = 20f;
            manaLabel.overflowMode = TextOverflowModes.Truncate;
            manaLabel.margin = Vector4.zero;
        }

        if (titleLabel != null)
        {
            // The gold frame's title ribbon is narrower than the full card.
            SetTopLeft(titleLabel.rectTransform, new Vector2(27f, -172f), new Vector2(156f, 28f));
            titleLabel.alignment = TextAlignmentOptions.Center;
            titleLabel.verticalAlignment = VerticalAlignmentOptions.Middle;
            titleLabel.color = new Color(0.16f, 0.085f, 0.035f, 1f);
            titleLabel.enableAutoSizing = true;
            titleLabel.fontSizeMin = 9f;
            titleLabel.fontSizeMax = 15f;
            titleLabel.textWrappingMode = TextWrappingModes.NoWrap;
            titleLabel.overflowMode = TextOverflowModes.Ellipsis;
            titleLabel.margin = Vector4.zero;
        }

        if (descriptionLabel != null)
        {
            // Keep the full description inside the paper panel below the title.
            SetTopLeft(descriptionLabel.rectTransform, new Vector2(30f, -207f), new Vector2(150f, 55f));
            descriptionLabel.alignment = TextAlignmentOptions.Center;
            descriptionLabel.verticalAlignment = VerticalAlignmentOptions.Middle;
            descriptionLabel.richText = true;
            descriptionLabel.textWrappingMode = TextWrappingModes.Normal;
            descriptionLabel.enableAutoSizing = true;
            descriptionLabel.fontSizeMin = 8f;
            descriptionLabel.fontSizeMax = 14f;
            descriptionLabel.overflowMode = TextOverflowModes.Ellipsis;
            descriptionLabel.margin = new Vector4(4f, 3f, 4f, 3f);
        }

        if (ownershipLabel != null)
        {
            SetTopLeft(ownershipLabel.rectTransform, new Vector2(16f, -303f), new Vector2(178f, 23f));
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
            frameImage.preserveAspect = false;
            frameImage.fillCenter = frame.fillCenter;
            frameImage.fillAmount = frame.fillAmount;
            frameImage.raycastTarget = false;
            frame.enabled = false;
            frame.raycastTarget = true;
            frame = frameImage;
        }

        // Crop artwork to the frame's transparent illustration window. Without
        // a viewport the icon draws over the gold corners and looks like it has
        // protruding edges. The square 500 Skill Icons remain undistorted and
        // are cropped slightly to fill this wider window.
        Transform viewportTransform = transform.Find("Artwork Viewport");
        if (viewportTransform == null)
        {
            var viewportObject = new GameObject("Artwork Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportObject.transform.SetParent(transform, false);
            viewportTransform = viewportObject.transform;
        }

        RectTransform viewportRect = (RectTransform)viewportTransform;
        SetTopLeft(viewportRect, new Vector2(37f, -46f), new Vector2(136f, 121f));
        viewportTransform.SetAsFirstSibling();

        if (icon != null)
        {
            if (icon.transform.parent != viewportTransform)
                icon.transform.SetParent(viewportTransform, false);

            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(140f, 140f);
            icon.preserveAspect = true;
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
