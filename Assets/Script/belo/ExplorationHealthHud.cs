using TMPro;
using UnityEngine;

/// <summary>Displays the same saved HP as the battle hero.</summary>
public sealed class ExplorationHealthHud : MonoBehaviour
{
    [SerializeField] private HeroClassData heroClass;
    [SerializeField, Min(1)] private int startingLevel = 1;
    [SerializeField] private UnityEngine.UI.Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthLabel;

    private int lastHealth = -1;
    private int lastMaximum = -1;
    private float nextRefresh;

    private void Start()
    {
        PlayerHealth.EnsureInitialized(heroClass != null ? heroClass.GetHealthForLevel(startingLevel) : 100);
        Refresh();
    }

    private void Update()
    {
        // Also handles slot/cloud changes and direct scene play without a save slot.
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.1f;
        PlayerHealth.EnsureInitialized(heroClass != null ? heroClass.GetHealthForLevel(startingLevel) : 100);
        Refresh();
    }

    private void Refresh()
    {
        var progressionClass=PlayerProgression.Class??heroClass;
        if(progressionClass!=null)PlayerHealth.ConfigureMaximum(progressionClass.GetHealthForLevel(PlayerProgression.Level));
        int maximum = PlayerHealth.MaxHealth;
        int health = PlayerHealth.CurrentHealth;
        if (health == lastHealth && maximum == lastMaximum) return;
        lastHealth = health;
        lastMaximum = maximum;
        if (healthSlider != null)
        {
            healthSlider.maxValue = maximum;
            healthSlider.SetValueWithoutNotify(health);
        }
        if (healthLabel != null) healthLabel.text = $"HP: {health} / {maximum}";
    }
}
