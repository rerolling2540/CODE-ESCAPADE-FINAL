using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Inherits the HUD CanvasGroups, so settings and cutscenes also block this card.
public sealed class SpecialSkillCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, Min(1f)] private float hoverScale = 1.6f;
    private RectTransform rect;
    private Button button;
    private Vector3 restingScale;
    private bool hovered;

    public void Configure()
    {
        rect = GetComponent<RectTransform>();
        button = GetComponent<Button>();
        restingScale = rect.localScale;
        rect.sizeDelta = new Vector2(168f, 238f);
        BattleUITheme.DecoratePanel(GetComponent<Image>());

        LayoutLabel("EP Skill Title", new Vector2(0f, -12f), new Vector2(148f, 48f), true, 21f);
        LayoutLabel("EP Skill Description", new Vector2(0f, 14f), new Vector2(144f, 94f), false, 18f);
        Transform icon = transform.Find("EP Skill Icon");
        if (icon != null)
        {
            var iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(.5f, 0f);
            iconRect.pivot = new Vector2(.5f, 0f);
            iconRect.anchoredPosition = new Vector2(0f, 118f);
            iconRect.sizeDelta = new Vector2(64f, 64f);
        }
    }

    private void LayoutLabel(string name, Vector2 position, Vector2 size, bool top, float fontSize)
    {
        Transform child = transform.Find(name);
        if (child == null) return;
        var label = child.GetComponent<TMP_Text>();
        if (label == null) return;
        var r = label.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(.5f, top ? 1f : 0f);
        r.pivot = new Vector2(.5f, top ? 1f : 0f);
        r.anchoredPosition = position;
        r.sizeDelta = size;
        label.color = new Color(1f, .93f, .75f);
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSize = fontSize;
        label.fontSizeMin = fontSize - 2f;
        label.fontSizeMax = fontSize;
        label.overflowMode = TextOverflowModes.Truncate;
        label.fontStyle = top ? FontStyles.Bold : FontStyles.Normal;
        label.richText = true;
        if (!top) label.text = CardView.FormatEffectDescription(BattleHudMedieval.SpecialSkillDescription);
    }

    private bool CanHover => button != null && button.IsInteractable()
        && !GameControlsGuide.GameplayOverlayHidden && !MatchSetupSystem.StarfallCutsceneActive;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanHover) return;
        hovered = true;
        BattleAudio.Play(BattleSound.CardHover);
        transform.SetAsLastSibling();
    }

    public void OnPointerExit(PointerEventData eventData) { hovered = false; }

    void LateUpdate()
    {
        if (rect == null) return;
        if (!CanHover) hovered = false;
        Vector3 target = restingScale * (hovered ? hoverScale : 1f);
        rect.localScale = Vector3.Lerp(rect.localScale, target, 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));
    }

    void OnDisable()
    {
        hovered = false;
        if (rect != null) rect.localScale = restingScale;
    }
}
