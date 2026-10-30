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

    public void Bind(CardData card, int quantity, int level)
    {
        if (card == null) return;
        if (frame != null) frame.preserveAspect = true;
        if (icon != null)
        {
            icon.sprite = card.Image;
            icon.enabled = card.Image != null;
            icon.preserveAspect = true;
        }
        if (manaLabel != null) manaLabel.text = card.Mana + " MANA";
        if (titleLabel != null) titleLabel.text = card.name;
        if (descriptionLabel != null) descriptionLabel.text = string.IsNullOrWhiteSpace(card.Description)
            ? "No description available."
            : card.Description.Trim();
        if (ownershipLabel != null)
            ownershipLabel.text = "OWNED ×" + Mathf.Max(1, quantity) + "  ·  LV " + Mathf.Max(1, level);
    }
}
