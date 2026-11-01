using TMPro;
using UnityEngine;

/// <summary>Scene-authored poison for floor spikes. Repeated hits refresh, never stack.</summary>
public sealed class PoisonSpikeHazard : MonoBehaviour
{
    [SerializeField, Min(1)] private int damagePerTick = 5;
    [SerializeField, Min(1f)] private float duration = 5f;
    [SerializeField] private CanvasGroup warning;
    [SerializeField] private TMP_Text warningText;
    [SerializeField, Min(0.1f)] private float blinkInterval = 0.4f;
    [SerializeField] private UnityEngine.UI.Image poisonScreenTint;
    [SerializeField, Range(0f, 0.3f)] private float screenFlashOpacity = 0.14f;
    private playerMovement poisonedPlayer;
    private float remaining;
    private float tickElapsed;
    private float warningElapsed;
    public bool IsPoisoned => poisonedPlayer != null && remaining > 0f;

    private void Awake() => Clear();

    public void Infect(playerMovement player)
    {
        if (player == null || !player.isActiveAndEnabled || PlayerHealth.CurrentHealth <= 0) return;
        // Several painted hazard layers can poison the same player; refresh one status.
        foreach (var hazard in FindObjectsByType<PoisonSpikeHazard>(FindObjectsSortMode.None))
        {
            if (hazard == this || !hazard.IsPoisoned || hazard.poisonedPlayer != player) continue;
            if (IsPoisoned) Clear();
            hazard.remaining = hazard.duration;
            hazard.RefreshWarning();
            return;
        }
        if (!IsPoisoned || poisonedPlayer != player) tickElapsed = warningElapsed = 0f;
        poisonedPlayer = player;
        remaining = duration;
        RefreshWarning();
    }

    private void Update()
    {
        if (remaining <= 0f) return;
        if (poisonedPlayer == null || !poisonedPlayer.isActiveAndEnabled ||
            PlayerHealth.CurrentHealth <= 0 || PlayerDeathFlow.IsActive)
        {
            Clear();
            return;
        }
        if (Time.timeScale <= 0f || SceneTransitionFader.IsTransitioning ||
            (DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive)) return;
        Advance(Time.deltaTime);
    }

    private void Advance(float deltaTime)
    {
        float step = Mathf.Min(Mathf.Max(0f, deltaTime), remaining);
        remaining = Mathf.Max(0f, remaining - step);
        tickElapsed += step;
        warningElapsed += step;
        // Poison has its own cadence, independent of the contact-hit immunity window.
        while (tickElapsed + 0.0001f >= 1f && PlayerHealth.CurrentHealth > 0)
        {
            tickElapsed = Mathf.Max(0f, tickElapsed - 1f);
            PlayerHealth.Damage(damagePerTick, PlayerDamageCause.Poison);
        }
        if (remaining <= 0f || PlayerHealth.CurrentHealth <= 0) Clear();
        else RefreshWarning();
    }

    private void RefreshWarning()
    {
        if (warning != null) warning.alpha = 1f;
        float interval = Mathf.Max(0.1f, blinkInterval);
        bool visible = Mathf.Repeat(warningElapsed, interval * 2f) < interval;
        if (warningText != null)
        {
            warningText.text = "POISONED";
            warningText.alpha = visible ? 1f : 0f;
        }
        SetScreenTint(visible);
    }

    private void SetScreenTint(bool visible)
    {
        if (poisonScreenTint == null) return;
        Color color = warningText != null ? warningText.color : new Color(0.8f, 1f, 0.35f);
        color.a = visible ? screenFlashOpacity : 0f;
        poisonScreenTint.color = color;
    }

    private void Clear()
    {
        poisonedPlayer = null;
        remaining = tickElapsed = warningElapsed = 0f;
        if (warning != null) warning.alpha = 0f;
        if (warningText != null) warningText.alpha = 1f;
        SetScreenTint(false);
    }

    private void OnDisable() => Clear();
}
