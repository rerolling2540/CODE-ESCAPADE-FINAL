using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatantView : MonoBehaviour
{
    [Header("UI & Visual References")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private StatusEffectsUI statusEffectsUI;

    [Header("Dynamic Bar Scaling (Elden Ring Style)")]
    [SerializeField] private RectTransform healthBarRect; // Drag your HealthSlider RectTransform here
    [SerializeField] private float baseWidth = 200f;       // Bar width at base health (100 HP)
    [SerializeField] private int baseHealth = 100;         // Starting baseline health
    [SerializeField] private float maxWidth = 600f;        // Cap width to keep UI within screen bounds

    [Header("Health System Properties")]
    public int MaxHealth { get; private set; } = 100;
    public int CurrentHealth { get; private set; } = 100;

    private Dictionary<StatusEffectType, int> statusEffects = new();

    private void Start()
    {
        UpdateHealthUI();
    }

    // Call this to initialize hero stats from HeroClassData without hardcoding HP
    public void InitializeClass(HeroClassData classData, int level)
    {
        if (classData == null) return;

        MaxHealth = classData.GetHealthForLevel(level);
        CurrentHealth = MaxHealth;

        UpdateHealthUI();
    }

    protected void SetUpBase(int health, Sprite image)
    {
        MaxHealth = CurrentHealth = health;
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = image;
        }
        UpdateHealthUI();
    }

    public void UpdateHealthUI()
    {
        // 1. Dynamic Health Bar Width Scaling
        if (healthBarRect != null && baseHealth > 0)
        {
            float healthRatio = (float)MaxHealth / baseHealth;
            float newWidth = Mathf.Min(baseWidth * healthRatio, maxWidth);

            // Dynamically updates RectTransform width
            healthBarRect.sizeDelta = new Vector2(newWidth, healthBarRect.sizeDelta.y);
        }

        // 2. Standard Slider & Text Refresh
        if (healthText != null)
        {
            healthText.text = "HP: " + CurrentHealth + " / " + MaxHealth;
        }

        if (healthSlider != null)
        {
            healthSlider.maxValue = MaxHealth;
            healthSlider.value = CurrentHealth;
        }
    }

    public void Damage(int damageAmount)
    {
        int remainingDamage = damageAmount;
        int currentArmor = GetStatusEffectStacks(StatusEffectType.ARMOR);

        if (currentArmor > 0)
        {
            if (currentArmor >= damageAmount)
            {
                RemoveStatusEffect(StatusEffectType.ARMOR, remainingDamage);
                remainingDamage = 0;
            }
            else
            {
                RemoveStatusEffect(StatusEffectType.ARMOR, currentArmor);
                remainingDamage -= currentArmor;
            }
        }

        if (remainingDamage > 0)
        {
            CurrentHealth -= remainingDamage;
            if (CurrentHealth < 0)
            {
                CurrentHealth = 0;
            }
        }

        transform.DOShakePosition(0.2f, 0.5f);
        UpdateHealthUI();
    }

    public void Heal(int amount)
    {
        CurrentHealth += amount;
        if (CurrentHealth > MaxHealth)
        {
            CurrentHealth = MaxHealth;
        }
        UpdateHealthUI();
    }

    #region Context Menu Testing
    [ContextMenu("Test - Take 20 Damage")]
    public void TestDamage()
    {
        Damage(20);
    }

    [ContextMenu("Test - Heal 20 HP")]
    public void TestHeal()
    {
        Heal(20);
    }
    #endregion

    #region Status Effect Logic
    public void AddStatusEffect(StatusEffectType type, int stackCount)
    {
        if (statusEffects.ContainsKey(type))
        {
            statusEffects[type] += stackCount;
        }
        else
        {
            statusEffects.Add(type, stackCount);
        }

        if (statusEffectsUI != null)
        {
            statusEffectsUI.UpdateStatusEffectUI(type, GetStatusEffectStacks(type));
        }
    }

    public void RemoveStatusEffect(StatusEffectType type, int stackCount)
    {
        if (statusEffects.ContainsKey(type))
        {
            statusEffects[type] -= stackCount;
            if (statusEffects[type] <= 0)
            {
                statusEffects.Remove(type);
            }
        }

        if (statusEffectsUI != null)
        {
            statusEffectsUI.UpdateStatusEffectUI(type, GetStatusEffectStacks(type));
        }
    }

    public int GetStatusEffectStacks(StatusEffectType type)
    {
        if (statusEffects.ContainsKey(type)) return statusEffects[type];
        else return 0;
    }
    #endregion
}