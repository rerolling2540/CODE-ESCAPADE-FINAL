
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatusEffectUI : MonoBehaviour
{
  [SerializeField] private Image image;
  [SerializeField] private TMP_Text stackCountText;
  [SerializeField] private Image stackBadgeBackground;

  private void Awake()
  {
      ConfigureForStatusHud();
  }

  public void ConfigureForStatusHud()
  {
      if (image != null)
      {
          image.preserveAspect = true;
          image.raycastTarget = false;
      }

      if (stackBadgeBackground != null)
      {
          stackBadgeBackground.raycastTarget = false;
      }

      if (stackCountText != null)
      {
          stackCountText.alignment = TextAlignmentOptions.Center;
          stackCountText.raycastTarget = false;
          stackCountText.color = new Color(1f, 0.93f, 0.72f, 1f);
          stackCountText.fontStyle = FontStyles.Bold;
      }
  }

  public void SetHudSize(Vector2 size)
  {
      if (transform is RectTransform rect) rect.sizeDelta = size;
      if (image != null)
      {
          RectTransform icon = image.rectTransform;
          icon.anchorMin = icon.anchorMax = new Vector2(0.5f, 0.5f);
          icon.pivot = new Vector2(0.5f, 0.5f);
          float badgeHeight = Mathf.Clamp(size.y * 0.24f, 10f, 14f);
          float gap = Mathf.Clamp(size.y * 0.07f, 2.5f, 4f);
          float iconTop = size.y * 0.5f - Mathf.Clamp(size.y * 0.06f, 2f, 4f);
          float iconBottom = -size.y * 0.5f + badgeHeight + gap;
          float iconHeight = Mathf.Max(1f, iconTop - iconBottom);
          float iconWidth = Mathf.Min(size.x - 4f, iconHeight);
          icon.sizeDelta = new Vector2(iconWidth, iconHeight);
          icon.anchoredPosition = new Vector2(0f, (iconTop + iconBottom) * 0.5f);
          image.preserveAspect = true;
          image.raycastTarget = false;
      }
      if (stackBadgeBackground != null)
      {
          RectTransform badge = stackBadgeBackground.rectTransform;
          badge.anchorMin = badge.anchorMax = new Vector2(0.5f, 0f);
          badge.pivot = new Vector2(0.5f, 0f);
          badge.sizeDelta = new Vector2(size.x - 4f, Mathf.Clamp(size.y * 0.24f, 10f, 14f));
          badge.anchoredPosition = Vector2.zero;
          stackBadgeBackground.raycastTarget = false;
      }
      if (stackCountText != null)
      {
          RectTransform count = stackCountText.rectTransform;
          count.anchorMin = Vector2.zero;
          count.anchorMax = Vector2.one;
          count.offsetMin = count.offsetMax = Vector2.zero;
          stackCountText.fontSize = 11f;
          stackCountText.alignment = TextAlignmentOptions.Center;
          stackCountText.raycastTarget = false;
      }
  }
  
  public void Set(StatusEffectType type, Sprite sprite, int stackCount)
    {
        ConfigureForStatusHud();

        if (image != null)
        {
            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        if (stackBadgeBackground != null)
            stackBadgeBackground.enabled = sprite != null;

        if (stackCountText != null)
        {
            stackCountText.text = sprite != null ? Mathf.Max(0, stackCount).ToString() : ShortName(type) + "\n" + Mathf.Max(0, stackCount);
            if (sprite == null) stackCountText.fontSize = 10;
        }
    }

    private static string ShortName(StatusEffectType type) => type switch
    {
        StatusEffectType.ARMOR => "ARM",
        StatusEffectType.BURN => "BRN",
        StatusEffectType.POISON => "PSN",
        StatusEffectType.BLEED => "BLD",
        StatusEffectType.FROST => "FST",
        StatusEffectType.NATURE => "NAT",
        _ => type.ToString()
    };
}
