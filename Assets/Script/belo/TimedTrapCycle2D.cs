using UnityEngine;

/// <summary>Warns before an attack, then leaves a predictable opening to cross.</summary>
[DefaultExecutionOrder(50)]
[RequireComponent(typeof(TrapPackAnimation), typeof(Map4TrapDamageTrigger))]
public sealed class TimedTrapCycle2D : MonoBehaviour
{
    public enum Phase { Safe, Warning, Active, Disabled }

    [SerializeField, Min(0.5f)] private float safeDuration = 3f;
    [SerializeField, Min(0.1f)] private float warningDuration = 0.6f;
    [SerializeField, Min(0.1f)] private float activeDuration = 2f;
    [SerializeField, Min(0f)] private float phaseOffset;
    [SerializeField] private ElectricalTrapSwitch2D powerSwitch;

    private TrapPackAnimation animation;
    private Map4TrapDamageTrigger damage;
    private SpriteRenderer visual;
    private Collider2D[] hitboxes;
    private Color originalColor;
    private float startedAt;
    private bool initialized;

    public Phase CurrentPhase { get; private set; }
    public float CycleDuration => safeDuration + warningDuration + activeDuration;

    private void Awake()
    {
        animation = GetComponent<TrapPackAnimation>();
        damage = GetComponent<Map4TrapDamageTrigger>();
        visual = GetComponent<SpriteRenderer>();
        hitboxes = GetComponents<Collider2D>();
        if (visual != null) originalColor = visual.color;
        startedAt = Time.time;
    }

    private void Start() => ApplyCycle(0f);

    private void Update()
    {
        if (Time.timeScale > 0f) ApplyCycle(Time.time - startedAt);
    }

    private void ApplyCycle(float elapsed)
    {
        if (powerSwitch != null && !powerSwitch.IsPowered)
        {
            ApplyPhase(Phase.Disabled, elapsed);
            return;
        }
        float cycle = Mathf.Repeat(elapsed + phaseOffset, CycleDuration);
        Phase phase = cycle < safeDuration ? Phase.Safe :
            cycle < safeDuration + warningDuration ? Phase.Warning : Phase.Active;
        ApplyPhase(phase, elapsed);
    }

    private void ApplyPhase(Phase phase, float elapsed)
    {
        bool active = phase == Phase.Active;
        bool restart = active && (!initialized || CurrentPhase != Phase.Active);
        animation.enabled = active;
        damage.enabled = active;
        foreach (var hitbox in hitboxes)
            if (hitbox != null) hitbox.enabled = active;
        if (restart) animation.RestartPlayback();
        if (visual != null)
        {
            if (!active && animation.IdleSprite != null) visual.sprite = animation.IdleSprite;
            float flash = phase == Phase.Warning ? 0.4f + 0.5f * Mathf.PingPong(elapsed * 7f, 1f) : 0f;
            visual.color = Color.Lerp(originalColor, new Color(1f, 0.45f, 0.1f, originalColor.a), flash);
        }
        CurrentPhase = phase;
        initialized = true;
    }

    private void OnDisable()
    {
        if (animation != null) ApplyPhase(Phase.Disabled, 0f);
    }
}
