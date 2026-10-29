using UnityEngine;

/// <summary>Loops the horizontal frame strip supplied by the Foozle trap pack.</summary>
public sealed class TrapPackAnimation : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private Sprite idleSprite;
    [SerializeField, Min(1f)] private float framesPerSecond = 12f;
    [SerializeField, Min(0), Tooltip("First damaging animation frame (zero-based).")]
    private int firstDamageFrame;
    [SerializeField, Tooltip("Last damaging frame, inclusive. -1 means all remaining frames.")]
    private int lastDamageFrame = -1;
    [SerializeField, Tooltip("Fit the contact box to the visible sprite mesh on each frame (for extending worms).")]
    private bool fitDamageHitboxToSprite;
    private BoxCollider2D damageHitbox;
    private float frameDuration;
    private float elapsed;
    private int currentFrame;
    public int CurrentFrame => currentFrame;
    public Sprite IdleSprite => idleSprite != null ? idleSprite : frames != null && frames.Length > 0 ? frames[0] : null;
    public bool IsDamageActive => isActiveAndEnabled && frames != null &&
        currentFrame >= 0 && currentFrame < frames.Length &&
        spriteRenderer != null && spriteRenderer.enabled &&
        spriteRenderer.sprite != null && spriteRenderer.sprite != idleSprite &&
        currentFrame >= firstDamageFrame && (lastDamageFrame < 0 || currentFrame <= lastDamageFrame);

    private void Awake()
    {
        Initialize(frames, framesPerSecond);
    }

    private void OnEnable()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && frames != null && frames.Length > 0)
        {
            currentFrame = Mathf.Clamp(currentFrame, 0, frames.Length - 1);
            spriteRenderer.sprite = frames[currentFrame];
            UpdateDamageHitbox();
        }
    }

    public void Initialize(Sprite[] spriteFrames, float frameRate)
    {
        frames = spriteFrames;
        elapsed = 0f;
        currentFrame = 0;
        framesPerSecond = Mathf.Max(1f, frameRate);
        frameDuration = 1f / Mathf.Max(1f, framesPerSecond);
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (frames != null && frames.Length > 0 && spriteRenderer != null)
        {
            spriteRenderer.sprite = frames[0];
            UpdateDamageHitbox();
        }
    }

    private void Update()
    {
        if (frames == null || frames.Length < 2 || spriteRenderer == null) return;
        elapsed += Time.deltaTime;
        while (elapsed >= frameDuration)
        {
            elapsed -= frameDuration;
            currentFrame = (currentFrame + 1) % frames.Length;
            spriteRenderer.sprite = frames[currentFrame];
            UpdateDamageHitbox();
        }
    }

    private void UpdateDamageHitbox()
    {
        if (!fitDamageHitboxToSprite || spriteRenderer == null || spriteRenderer.sprite == null) return;
        if (damageHitbox == null) damageHitbox = GetComponent<BoxCollider2D>();
        if (damageHitbox == null || !damageHitbox.isTrigger) return;

        // Tight mesh vertices exclude the empty part of the sprite sheet's frame.
        // Local-space bounds also follow authored rotation and nonuniform scale.
        Vector2[] vertices = spriteRenderer.sprite.vertices;
        if (vertices.Length == 0) return;
        Vector2 min = vertices[0];
        Vector2 max = vertices[0];
        for (int i = 1; i < vertices.Length; i++)
        {
            min = Vector2.Min(min, vertices[i]);
            max = Vector2.Max(max, vertices[i]);
        }
        Vector2 center = (min + max) * 0.5f;
        if (spriteRenderer.flipX) center.x = -center.x;
        if (spriteRenderer.flipY) center.y = -center.y;
        damageHitbox.offset = center;
        damageHitbox.size = Vector2.Max(max - min, Vector2.one * 0.01f);
    }
}
