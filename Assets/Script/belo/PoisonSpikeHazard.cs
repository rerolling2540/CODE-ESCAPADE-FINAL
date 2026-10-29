using TMPro;
using UnityEngine;

/// <summary>Scene-authored poison for floor spikes. Repeated hits refresh, never stack.</summary>
public sealed class PoisonSpikeHazard : MonoBehaviour
{
    [SerializeField, Min(1)] private int damagePerTick = 5;
    [SerializeField, Min(1f)] private float duration = 5f;
    [SerializeField] private CanvasGroup warning;
    [SerializeField] private TMP_Text warningText;
    private playerMovement poisonedPlayer;
    private float remaining;
    private float tickElapsed;
    public bool IsPoisoned => poisonedPlayer != null && remaining > 0f;

    private void Awake() => Clear();

    public void Infect(playerMovement player)
    {
        if (player == null || !player.isActiveAndEnabled || PlayerHealth.CurrentHealth <= 0) return;
        if (!IsPoisoned || poisonedPlayer != player) tickElapsed = 0f;
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
        // Poison has its own cadence, independent of the contact-hit immunity window.
        while (tickElapsed + 0.0001f >= 1f && PlayerHealth.CurrentHealth > 0)
        {
            tickElapsed = Mathf.Max(0f, tickElapsed - 1f);
            PlayerHealth.Damage(damagePerTick);
        }
        if (remaining <= 0f || PlayerHealth.CurrentHealth <= 0) Clear();
        else RefreshWarning();
    }

    private void RefreshWarning()
    {
        if (warning != null) warning.alpha = 1f;
        if (warningText != null)
            warningText.text = "POISONED";
    }

    private void Clear()
    {
        poisonedPlayer = null;
        remaining = tickElapsed = 0f;
        if (warning != null) warning.alpha = 0f;
    }

    private void OnDisable() => Clear();
}
