
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
      RectTransform root = transform as RectTransform;
      if (root != null)
          root.sizeDelta = new Vector2(36, 44);

      if (image != null)
      {
          RectTransform icon = image.rectTransform;
          icon.anchorMin = icon.anchorMax = new Vector2(0.5f, 0.5f);
          icon.pivot = new Vector2(0.5f, 0.5f);
          icon.sizeDelta = new Vector2(30, 30);
          icon.anchoredPosition = new Vector2(0, 6);
          image.preserveAspect = true;
          image.raycastTarget = false;
      }

      if (stackBadgeBackground != null)
      {
          RectTransform badge = stackBadgeBackground.rectTransform;
          badge.anchorMin = badge.anchorMax = new Vector2(0.5f, 0);
          badge.pivot = new Vector2(0.5f, 0);
          badge.sizeDelta = new Vector2(28, 15);
          badge.anchoredPosition = Vector2.zero;
          stackBadgeBackground.raycastTarget = false;
      }

      if (stackCountText != null)
      {
          RectTransform count = stackCountText.rectTransform;
          count.anchorMin = Vector2.zero;
          count.anchorMax = Vector2.one;
          count.pivot = new Vector2(0.5f, 0.5f);
          count.offsetMin = count.offsetMax = Vector2.zero;
          stackCountText.fontSize = 12;
          stackCountText.alignment = TextAlignmentOptions.Center;
          stackCountText.enableAutoSizing = false;
          stackCountText.raycastTarget = false;
          stackCountText.color = new Color(1f, 0.93f, 0.72f, 1f);
          stackCountText.fontStyle = FontStyles.Bold;
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
