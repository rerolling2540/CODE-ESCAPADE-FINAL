using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatusEffectsUI : MonoBehaviour
{
    // Keep the most useful player buffs first in the HUD row.
    private static readonly StatusEffectType[] DisplayOrder =
    {
        StatusEffectType.ARMOR,
        StatusEffectType.NATURE,
        StatusEffectType.BURN,
        StatusEffectType.POISON,
        StatusEffectType.BLEED,
        StatusEffectType.FROST,
        StatusEffectType.DAMAGE_BUFF
    };

    [SerializeField] private StatusEffectUI statusEffectsUIPrefab;
    [SerializeField] private Sprite armorSprite, burnSprite;
    [SerializeField] private Sprite poisonSprite, bleedSprite, frostSprite, natureSprite, damageBuffSprite;
    private Dictionary<StatusEffectType, StatusEffectUI> statusEffectUIs = new();
    private bool enemyHudLayout;

    public void MoveToEnemyHud(RectTransform hudRect)
    {
        if (hudRect == null || !(transform is RectTransform rect)) return;
        Canvas previousCanvas = GetComponentInParent<Canvas>();
        rect.SetParent(hudRect, false);
        if (previousCanvas != null && previousCanvas.transform != hudRect && previousCanvas.renderMode == RenderMode.WorldSpace)
            previousCanvas.enabled = false;

        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        // Keep enlarged per-enemy status icons above the HP bar, with enough width
        // for multiple effects without overlapping the neighboring enemy's HUD.
        rect.anchoredPosition = new Vector2(30f, 48f);
        rect.sizeDelta = new Vector2(440f, 70f);
        rect.localScale = Vector3.one;
        enemyHudLayout = true;
        var layout = GetComponent<HorizontalLayoutGroup>();
        if (layout == null) layout = gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 5f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        RefreshDisplayOrder();
    }

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
            // The visible, editable placement belongs to this scene object;
            // make the runtime status root fill it so it follows the host.
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
        // Keep the container scale authored in the scene as well.
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
            }
            Sprite sprite = GetSpriteByType(statusEffectType);
            statusEffectUIs[statusEffectType].Set(statusEffectType, sprite, stackCount);
        }

        RefreshDisplayOrder();
    }

    private void RefreshDisplayOrder()
    {
        int siblingIndex = 0;
        foreach (StatusEffectType type in DisplayOrder)
        {
            if (statusEffectUIs.TryGetValue(type, out StatusEffectUI statusEffectUI) && statusEffectUI != null)
            {
                if (enemyHudLayout)
                    statusEffectUI.SetHudSize(new Vector2(66f, 66f));
                else
                    statusEffectUI.SetPlayerHudSize();
                statusEffectUI.transform.SetSiblingIndex(siblingIndex++);
            }
        }
    }

    private Sprite GetSpriteByType(StatusEffectType statusEffectType)
    {
        return statusEffectType switch
        {
            StatusEffectType.ARMOR => armorSprite,
            StatusEffectType.BURN => burnSprite,
            StatusEffectType.POISON => poisonSprite,
            StatusEffectType.BLEED => bleedSprite,
            StatusEffectType.FROST => frostSprite,
            StatusEffectType.NATURE => natureSprite,
            StatusEffectType.DAMAGE_BUFF => damageBuffSprite,
            _ => null,
        };
    }

}
