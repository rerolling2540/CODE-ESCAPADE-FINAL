using UnityEngine;

/// <summary>A scene-authored launcher with a visible wind-up and one reusable arrow.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public sealed class ArrowTrap2D : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private Sprite arrowSprite;
    [SerializeField] private bool fireRight;
    [SerializeField, Min(1f)] private float framesPerSecond = 8f;
    [SerializeField, Min(0)] private int releaseFrame = 9;
    [SerializeField, Min(0f)] private float phaseOffset;
    [SerializeField, Min(1f)] private float arrowSpeed = 14f;
    [SerializeField, Min(1f)] private float arrowRange = 12f;
    [SerializeField, Min(1)] private int damage = 35;
    [SerializeField] private AudioClip releaseSound;

    private SpriteRenderer launcher;
    private SpriteRenderer arrow;
    private AudioSource audioSource;
    private float elapsed;
    private float remainingRange;
    private int previousFrame = -1;
    private Vector2 direction;
    public int ShotsFired { get; private set; }

    private void Awake()
    {
        launcher = GetComponent<SpriteRenderer>();
        launcher.flipX = fireRight;
        direction = fireRight ? Vector2.right : Vector2.left;
        var projectile = new GameObject("Arrow Projectile");
        projectile.transform.SetParent(transform, false);
        arrow = projectile.AddComponent<SpriteRenderer>();
        arrow.sprite = arrowSprite;
        arrow.flipX = fireRight;
        arrow.sortingLayerID = launcher.sortingLayerID;
        arrow.sortingOrder = launcher.sortingOrder + 1;
        // The supplied arrow uses a different pixels-per-unit value than the launcher.
        float scale = arrowSprite != null ? arrowSprite.pixelsPerUnit / 32f : 1f;
        projectile.transform.localScale = Vector3.one * scale;
        arrow.enabled = false;
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null) GameAudio.Route(audioSource, false);
        elapsed = phaseOffset;
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f) return;
        MoveArrow(Time.deltaTime);
        if (frames == null || frames.Length == 0) return;
        elapsed += Time.deltaTime;
        int frame = Mathf.FloorToInt(elapsed * framesPerSecond) % frames.Length;
        launcher.sprite = frames[frame];
        // Detect a crossed release frame even if a slow frame skips its exact index.
        bool released = previousFrame >= 0 && (frame >= previousFrame
            ? previousFrame < releaseFrame && frame >= releaseFrame
            : previousFrame < releaseFrame || frame >= releaseFrame);
        if (released) Fire();
        previousFrame = frame;
    }

    private void Fire()
    {
        if (arrowSprite == null) return;
        arrow.transform.position = transform.TransformPoint(new Vector3(fireRight ? 0.36f : -0.36f, 0.32f, 0f));
        remainingRange = arrowRange;
        arrow.enabled = true;
        ShotsFired++;
        if (audioSource != null && releaseSound != null) audioSource.PlayOneShot(releaseSound, 0.45f);
    }

    private void MoveArrow(float deltaTime)
    {
        if (arrow == null || !arrow.enabled) return;
        float distance = Mathf.Min(arrowSpeed * deltaTime, remainingRange);
        var hits = Physics2D.CircleCastAll(arrow.transform.position, 0.07f, direction, distance);
        foreach (var hit in hits)
        {
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform)) continue;
            var player = hit.collider.GetComponentInParent<playerMovement>();
            if (player != null)
            {
                player.TryTakeDamage(damage, PlayerDamageCause.Trap);
                arrow.enabled = false;
                return;
            }
            if (!hit.collider.isTrigger)
            {
                arrow.enabled = false;
                return;
            }
        }
        arrow.transform.position += (Vector3)(direction * distance);
        remainingRange -= distance;
        if (remainingRange <= 0f) arrow.enabled = false;
    }

    private void OnDisable()
    {
        if (arrow != null) arrow.enabled = false;
    }
}
