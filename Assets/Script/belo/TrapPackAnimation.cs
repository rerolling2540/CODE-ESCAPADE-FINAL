using UnityEngine;

/// <summary>Loops the horizontal frame strip supplied by the Foozle trap pack.</summary>
public sealed class TrapPackAnimation : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private Sprite idleSprite;
    [SerializeField, Min(1f)] private float framesPerSecond = 12f;
    private float frameDuration;
    private float elapsed;
    private int currentFrame;
    public Sprite IdleSprite => idleSprite != null ? idleSprite : frames != null && frames.Length > 0 ? frames[0] : null;

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
            spriteRenderer.sprite = frames[0];
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
        }
    }
}
