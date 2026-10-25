using System.Collections;
using UnityEngine;

public sealed class CharacterSmokeEffects : MonoBehaviour
{
    private const string RunSheetPath = "SmokeFX/SmokeFX Lite SpriteSheet 1A-1";
    private const string DashSheetPath = "SmokeFX/SmokeFX Lite SpriteSheet 2A-1";
    private const string JumpSheetPath = "SmokeFX/Smoke VFX 1";
    private const string LandingSheetPath = "SmokeFX/Smoke VFX 2";
    private const int FrameSize = 64;

    [SerializeField, Min(0.08f)] private float runPuffInterval = 0.22f;
    [SerializeField, Min(0.01f)] private float runFrameDuration = 0.035f;
    [SerializeField, Min(0.01f)] private float dashFrameDuration = 0.04f;
    [SerializeField, Min(0.01f)] private float jumpFrameDuration = 0.04f;
    [SerializeField, Min(0.01f)] private float landingFrameDuration = 0.045f;
    [SerializeField, Min(0.1f)] private float runPuffScale = 0.62f;
    [SerializeField, Min(0.1f)] private float dashPuffScale = 1.05f;
    [SerializeField, Min(0.1f)] private float jumpPuffScale = 0.82f;
    [SerializeField, Min(0.1f)] private float landingPuffScale = 0.9f;

    private SpriteRenderer characterRenderer;
    private CapsuleCollider2D characterCollider;
    private Sprite[] runFrames;
    private Sprite[] dashFrames;
    private Sprite[] jumpFrames;
    private Sprite[] landingFrames;
    private float runPuffTimer;

    public void Initialize(SpriteRenderer visual, CapsuleCollider2D capsule)
    {
        characterRenderer = visual;
        characterCollider = capsule;
        runFrames = LoadStrip(RunSheetPath);
        dashFrames = LoadStrip(DashSheetPath);
        jumpFrames = LoadStrip(JumpSheetPath, 48, 32, 48);
        landingFrames = LoadStrip(LandingSheetPath, 48, 32, 48);

        if (runFrames == null || dashFrames == null || jumpFrames == null || landingFrames == null)
            Debug.LogWarning("One or more smoke FX sprite sheets could not be loaded from Resources/SmokeFX.", this);
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

    public void PlayJump(bool facingRight)
    {
        if (jumpFrames == null || characterRenderer == null) return;

        Vector3 position = GetFeetPosition();
        SpawnPuff(jumpFrames, position, !facingRight, jumpFrameDuration, jumpPuffScale);
    }

    public void PlayLanding(bool facingRight)
    {
        if (landingFrames == null || characterRenderer == null) return;

        Vector3 position = GetFeetPosition();
        SpawnPuff(landingFrames, position, !facingRight, landingFrameDuration, landingPuffScale);
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

    private static Sprite[] LoadStrip(string resourcePath, int frameWidth = FrameSize,
        int frameHeight = FrameSize, float pixelsPerUnit = 100f)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null || texture.width < frameWidth || texture.width % frameWidth != 0 ||
            texture.height < frameHeight)
            return null;

        int frameCount = texture.width / frameWidth;
        var frames = new Sprite[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            frames[i] = Sprite.Create(texture, new Rect(i * frameWidth, 0, frameWidth, frameHeight),
                new Vector2(0.5f, 0.12f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
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
