using UnityEngine;

[ExecuteAlways]
public class BattleCaveBackground : MonoBehaviour
{
    public Camera battleCamera;
    public SpriteRenderer[] layers;

    [Header("Cave Atmosphere")]
    [Range(0f, 1f)] public float motionStrength = 0.12f;
    [Min(5f)] public float driftPeriod = 40f;
    [Range(0, 50)] public int dustCount = 10;
    private Vector3[] restingPositions;
    private GameObject atmosphere;
    private SpriteRenderer[] dust;
    private SpriteRenderer[] mist;
    private Sprite dustSprite, mistSprite;
    private Texture2D dustTexture, mistTexture;
    private float clock;

    void OnEnable()
    {
        CapturePositions();
        FitCamera();
        if (Application.isPlaying) CreateAtmosphere();
    }

    void CapturePositions()
    {
        if (layers == null) return;
        restingPositions = new Vector3[layers.Length];
        for (int i = 0; i < layers.Length; i++)
            if (layers[i] != null) restingPositions[i] = layers[i].transform.localPosition;
    }

    void LateUpdate()
    {
        FitCamera();
        if (!Application.isPlaying || battleCamera == null || layers == null) return;
        if (restingPositions == null || restingPositions.Length != layers.Length) CapturePositions();
        if (atmosphere == null) CreateAtmosphere();
        clock += Time.deltaTime;
        AnimateLayers();
        AnimateAtmosphere();
    }

    void FitCamera()
    {
        if (battleCamera == null || !battleCamera.orthographic || layers == null || layers.Length == 0 ||
            layers[0] == null || layers[0].sprite == null) return;
        Vector2 size = layers[0].sprite.bounds.size;
        if (size.x <= 0f || size.y <= 0f) return;
        float height = battleCamera.orthographicSize * 2f;
        // Uniform scaling fills wide and tall windows without stretching the artwork.
        float scale = Mathf.Max(height / size.y, height * battleCamera.aspect / size.x) * 1.08f;
        Vector3 position = battleCamera.transform.position;
        position.z += 20f;
        Vector3 targetScale = new Vector3(scale, scale, 1f);
        if (transform.position != position) transform.position = position;
        if (transform.localScale != targetScale) transform.localScale = targetScale;
    }

    void AnimateLayers()
    {
        float phase = clock * Mathf.PI * 2f / Mathf.Max(5f, driftPeriod);
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == null) continue;
            // The solid backdrop and foreground floor remain anchored to the battle.
            float depth = i > 0 && i < layers.Length - 1 ? Mathf.Sin(i / (float)(layers.Length - 1) * Mathf.PI) : 0f;
            float x = Mathf.Sin(phase) * motionStrength * depth;
            float y = Mathf.Sin(phase * 0.7f) * motionStrength * depth * 0.18f;
            layers[i].transform.localPosition = restingPositions[i] +
                new Vector3(x / transform.localScale.x, y / transform.localScale.y, 0f);
        }
    }

    void CreateAtmosphere()
    {
        if (atmosphere != null || battleCamera == null) return;
        atmosphere = new GameObject("Cave Mist and Dust");
        atmosphere.hideFlags = HideFlags.DontSave;
        atmosphere.transform.SetParent(transform, false);
        dustTexture = new Texture2D(5, 5, TextureFormat.RGBA32, false);
        dustTexture.filterMode = FilterMode.Point;
        dustTexture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 5; x++)
            {
                int distance = Mathf.Abs(x - 2) + Mathf.Abs(y - 2);
                dustTexture.SetPixel(x, y, new Color(1f, 1f, 1f, distance == 0 ? 1f : distance == 1 ? 0.35f : 0f));
            }
        dustTexture.Apply();
        dustSprite = Sprite.Create(dustTexture, new Rect(0, 0, 5, 5), new Vector2(0.5f, 0.5f), 5f);

        mistTexture = new Texture2D(64, 16, TextureFormat.RGBA32, false);
        mistTexture.filterMode = FilterMode.Bilinear;
        mistTexture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 64; x++)
            {
                float horizontal = Mathf.Sin(x / 63f * Mathf.PI);
                float vertical = Mathf.Sin(y / 15f * Mathf.PI);
                mistTexture.SetPixel(x, y, new Color(1f, 1f, 1f, horizontal * horizontal * vertical * vertical));
            }
        mistTexture.Apply();
        mistSprite = Sprite.Create(mistTexture, new Rect(0, 0, 64, 16), new Vector2(0.5f, 0.5f), 16f);
        dust = new SpriteRenderer[dustCount];
        for (int i = 0; i < dust.Length; i++) dust[i] = MakeSprite("Cave Dust " + i, dustSprite, -192);
        mist = new SpriteRenderer[1];
        for (int i = 0; i < mist.Length; i++) mist[i] = MakeSprite("Cave Mist " + i, mistSprite, -193);
        AnimateAtmosphere();
    }

    SpriteRenderer MakeSprite(string name, Sprite sprite, int order)
    {
        var item = new GameObject(name);
        item.transform.SetParent(atmosphere.transform, false);
        var renderer = item.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerID = layers != null && layers.Length > 0 && layers[0] != null ? layers[0].sortingLayerID : 0;
        renderer.sortingOrder = order;
        return renderer;
    }

    void AnimateAtmosphere()
    {
        if (atmosphere == null || dust == null || mist == null) return;
        float height = battleCamera.orthographicSize * 2f;
        float width = height * battleCamera.aspect;
        Vector3 centre = transform.position;
        for (int i = 0; i < dust.Length; i++)
        {
            float seed = i * 2.399963f;
            float x = (Mathf.Repeat(i * 0.618034f + clock * (0.002f + i % 3 * 0.0004f), 1f) - 0.5f) * width;
            float progress = Mathf.Repeat(i * 0.381966f + clock * (0.006f + i % 4 * 0.001f), 1f);
            float y = (progress - 0.5f) * height * 0.75f + height * 0.1f;
            dust[i].transform.position = centre + new Vector3(x + Mathf.Sin(clock * 0.25f + seed) * 0.15f, y, 0f);
            float size = Mathf.Lerp(0.025f, 0.055f, Mathf.Repeat(i * 0.754877f, 1f));
            dust[i].transform.localScale = new Vector3(size / transform.localScale.x, size / transform.localScale.y, 1f);
            float fade = Mathf.Sin(progress * Mathf.PI);
            dust[i].color = new Color(0.65f, 0.73f, 0.85f, fade * (0.045f + 0.02f * Mathf.Sin(clock * 0.8f + seed)));
        }
        for (int i = 0; i < mist.Length; i++)
        {
            float phase = clock * 0.06f + i * 2.4f;
            mist[i].transform.position = centre + new Vector3(Mathf.Sin(phase) * width * 0.16f,
                -height * 0.28f + Mathf.Sin(phase * 0.7f) * 0.08f, 0f);
            mist[i].transform.localScale = new Vector3(width * 0.36f / transform.localScale.x,
                height * 0.10f / transform.localScale.y, 1f);
            mist[i].color = new Color(0.38f, 0.46f, 0.58f, 0.03f + 0.01f * Mathf.Sin(phase));
        }
    }

    void OnDisable()
    {
        if (restingPositions != null && layers != null)
            for (int i = 0; i < Mathf.Min(layers.Length, restingPositions.Length); i++)
                if (layers[i] != null) layers[i].transform.localPosition = restingPositions[i];
        Release(atmosphere);
        Release(dustSprite);
        Release(mistSprite);
        Release(dustTexture);
        Release(mistTexture);
        atmosphere = null;
        dustSprite = mistSprite = null;
        dustTexture = mistTexture = null;
        dust = mist = null;
    }

    void Release(Object item)
    {
        if (item == null) return;
        if (Application.isPlaying) Destroy(item);
        else DestroyImmediate(item);
    }
}
