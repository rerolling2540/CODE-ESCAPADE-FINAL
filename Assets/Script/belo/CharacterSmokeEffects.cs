using System.Collections;
using UnityEngine;

public sealed class CharacterSmokeEffects : MonoBehaviour
{
    private const string RunSheetPath = "SmokeFX/SmokeFX Lite SpriteSheet 1A-1";
    private const string DashSheetPath = "SmokeFX/SmokeFX Lite SpriteSheet 2A-1";
    private const int FrameSize = 64;

    [SerializeField, Min(0.08f)] private float runPuffInterval = 0.22f;
    [SerializeField, Min(0.01f)] private float runFrameDuration = 0.035f;
    [SerializeField, Min(0.01f)] private float dashFrameDuration = 0.04f;
    [SerializeField, Min(0.1f)] private float runPuffScale = 0.62f;
    [SerializeField, Min(0.1f)] private float dashPuffScale = 1.05f;

    private SpriteRenderer characterRenderer;
    private CapsuleCollider2D characterCollider;
    private Sprite[] runFrames;
    private Sprite[] dashFrames;
    private float runPuffTimer;

    public void Initialize(SpriteRenderer visual, CapsuleCollider2D capsule)
    {
        characterRenderer = visual;
        characterCollider = capsule;
        runFrames = LoadStrip(RunSheetPath);
        dashFrames = LoadStrip(DashSheetPath);

        if (runFrames == null || dashFrames == null)
            Debug.LogWarning("Smoke FX sprite sheets could not be loaded from Resources/SmokeFX.", this);
    }

    public void UpdateRunning(bool running, bool grounded, bool facingRight)
    {
        if (!running || !grounded || runFrames == null || characterRenderer == null)
        {
            runPuffTimer = 0f;
            return;
        }

        runPuffTimer -= Time.deltaTime;
        if (runPuffTimer > 0f) return;

        runPuffTimer = runPuffInterval;
        Vector3 position = GetFeetPosition();
        position.x += facingRight ? -0.2f : 0.2f;
        SpawnPuff(runFrames, position, !facingRight, runFrameDuration, runPuffScale);
    }

    public void PlayDash(bool facingRight)
    {
        if (dashFrames == null || characterRenderer == null) return;

        Vector3 position = GetFeetPosition();
        position.x += facingRight ? -0.12f : 0.12f;
        SpawnPuff(dashFrames, position, !facingRight, dashFrameDuration, dashPuffScale);
    }

    private Vector3 GetFeetPosition()
    {
        if (characterCollider != null)
            return new Vector3(characterCollider.bounds.center.x, characterCollider.bounds.min.y + 0.03f,
                characterRenderer.transform.position.z);

        return characterRenderer.transform.position;
    }

    private void SpawnPuff(Sprite[] frames, Vector3 position, bool flipX, float frameDuration, float scale)
    {
        var puff = new GameObject("Smoke FX");
        puff.transform.position = position;
        puff.transform.localScale = Vector3.one * scale;

        var renderer = puff.AddComponent<SpriteRenderer>();
        renderer.sprite = frames[0];
        renderer.flipX = flipX;
        renderer.sortingLayerID = characterRenderer.sortingLayerID;
        renderer.sortingOrder = characterRenderer.sortingOrder - 1;

        StartCoroutine(AnimatePuff(puff, renderer, frames, frameDuration));
    }

    private static Sprite[] LoadStrip(string resourcePath)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null || texture.width < FrameSize || texture.width % FrameSize != 0 || texture.height < FrameSize)
            return null;

        int frameCount = texture.width / FrameSize;
        var frames = new Sprite[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            frames[i] = Sprite.Create(texture, new Rect(i * FrameSize, 0, FrameSize, FrameSize),
                new Vector2(0.5f, 0.12f), FrameSize, 0, SpriteMeshType.FullRect);
            frames[i].name = $"{resourcePath}_Frame_{i + 1}";
        }

        return frames;
    }

    private static IEnumerator AnimatePuff(GameObject puff, SpriteRenderer renderer, Sprite[] frames, float frameDuration)
    {
        for (int i = 0; i < frames.Length; i++)
        {
            if (puff == null || renderer == null) yield break;
            renderer.sprite = frames[i];
            yield return new WaitForSeconds(frameDuration);
        }

        if (puff != null) Destroy(puff);
    }
}
