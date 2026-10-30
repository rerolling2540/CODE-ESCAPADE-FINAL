using System.Collections.Generic;
using UnityEngine;

public class StatusEffectsUI : MonoBehaviour
{
    [SerializeField] private StatusEffectUI statusEffectsUIPrefab;
    [SerializeField] private Sprite armorSprite, burnSprite;
    private Dictionary<StatusEffectType, StatusEffectUI> statusEffectUIs = new();
    private bool compactHudIcons;

    public void MoveToHud(RectTransform hudRect, Vector2 position, Vector2 size)
    {
        if (hudRect == null) return;

        RectTransform rect = transform as RectTransform;
        if (rect == null) return;
        Canvas previousCanvas = GetComponentInParent<Canvas>();
        rect.SetParent(hudRect, false);
        if (previousCanvas != null && previousCanvas.renderMode == RenderMode.WorldSpace)
            previousCanvas.enabled = false;

        if (hudRect.name == "Player Status Effects")
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        else
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
        rect.localScale = Vector3.one;
        compactHudIcons = true;

        var layout = GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        if (layout != null)
        {
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 6;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        foreach (Transform child in transform)
            ConfigureHudIcon(child);
    }

    public void UpdateStatusEffectUI(StatusEffectType statusEffectType, int stackCount)
    {
        if (stackCount == 0)
        {
            if (statusEffectUIs.ContainsKey(statusEffectType))
            {
                StatusEffectUI statusEffectUI =statusEffectUIs[statusEffectType];
                statusEffectUIs.Remove(statusEffectType);
                Destroy(statusEffectUI.gameObject);
            }
        }
        else
        {
            if (!statusEffectUIs.ContainsKey(statusEffectType))
            {
                StatusEffectUI statusEffectUI = Instantiate(statusEffectsUIPrefab, transform);
                statusEffectUIs.Add(statusEffectType, statusEffectUI);
                if (compactHudIcons) ConfigureHudIcon(statusEffectUI.transform);
            }
            Sprite sprite = GetSpriteByType(statusEffectType);
            statusEffectUIs[statusEffectType].Set(sprite, stackCount);
        }
    }
    private Sprite GetSpriteByType(StatusEffectType statusEffectType)
    {
        return statusEffectType switch
        {
            StatusEffectType.ARMOR => armorSprite,
            StatusEffectType.BURN => burnSprite,
            _ => null,
        };
    }

    private static void ConfigureHudIcon(Transform iconTransform)
    {
        var iconRect = iconTransform as RectTransform;
        if (iconRect != null) iconRect.sizeDelta = new Vector2(30, 30);
        var image = iconTransform.GetComponentInChildren<UnityEngine.UI.Image>(true);
        if (image != null)
        {
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        var stackText = iconTransform.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (stackText != null)
        {
            var textRect = stackText.rectTransform;
            textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = new Vector2(10, -4);
            textRect.sizeDelta = new Vector2(30, 30);
            stackText.fontSize = 15;
            stackText.alignment = TMPro.TextAlignmentOptions.Center;
            stackText.raycastTarget = false;
        }
    }
}
