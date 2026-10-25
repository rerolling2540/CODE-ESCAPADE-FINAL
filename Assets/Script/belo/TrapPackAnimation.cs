using UnityEngine;

/// <summary>Loops the horizontal frame strip supplied by the Foozle trap pack.</summary>
public sealed class TrapPackAnimation : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private float frameDuration;
    private float elapsed;
    private int currentFrame;

    public void Initialize(Sprite[] spriteFrames, float framesPerSecond)
    {
        frames = spriteFrames;
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
