using System.Collections;
using UnityEngine;

/// <summary>Replaces the procedural mesh on portal-effect instances with the supplied sprite animation.</summary>
public sealed class AnimatedPortalVisual : MonoBehaviour
{
    private const string SpriteSheetPath = "AnimatedPortal/Portal-Spinning";
    private const int FrameSize = 64;
    // The 3x3 sheet contains eight animation frames; the bottom-right cell is empty.
    private const int FrameCount = 8;

    [SerializeField, Min(1f)] private float framesPerSecond = 12f;
    [SerializeField, Min(0.1f)] private float worldSize = 1.4f;
    public Sprite[] customFrames;

    private SpriteRenderer portalRenderer;
    private Sprite[] frames;

    private void Awake()
    {
        var proceduralRenderer = GetComponent<MeshRenderer>();
        if (proceduralRenderer != null) proceduralRenderer.enabled = false;

        if (customFrames != null && customFrames.Length > 0)
        {
            frames = customFrames;
            portalRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (portalRenderer == null)
            {
                var customVisual = new GameObject("Custom Portal Sprite");
                customVisual.transform.SetParent(transform, false);
                portalRenderer = customVisual.AddComponent<SpriteRenderer>();
            }
            portalRenderer.sprite = frames[0];
            if (proceduralRenderer != null)
            {
                portalRenderer.sortingLayerID = proceduralRenderer.sortingLayerID;
                portalRenderer.sortingOrder = proceduralRenderer.sortingOrder;
            }
            return;
        }

        Texture2D sheet = Resources.Load<Texture2D>(SpriteSheetPath);
        if (sheet == null || sheet.width != FrameSize * 3 || sheet.height != FrameSize * 3)
        {
            Debug.LogWarning("Animated portal sprite sheet could not be loaded from Resources/AnimatedPortal.", this);
            return;
        }

        sheet.filterMode = FilterMode.Point;
        frames = new Sprite[FrameCount];
        for (int frameIndex = 0; frameIndex < FrameCount; frameIndex++)
        {
            int row = 2 - frameIndex / 3;
            int column = frameIndex % 3;
            frames[frameIndex] = Sprite.Create(sheet,
                new Rect(column * FrameSize, row * FrameSize, FrameSize, FrameSize),
                new Vector2(0.5f, 0.5f), FrameSize, 0, SpriteMeshType.FullRect);
            frames[frameIndex].name = $"PortalSpinning_Frame_{frameIndex + 1}";
        }

        var visual = new GameObject("Animated Portal Sprite");
        visual.transform.SetParent(transform, false);
        float parentScale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        float localScale = worldSize / Mathf.Max(parentScale, 0.001f);
        visual.transform.localScale = new Vector3(localScale, localScale, 1f);

        portalRenderer = visual.AddComponent<SpriteRenderer>();
        portalRenderer.sprite = frames[0];
        if (proceduralRenderer != null)
        {
            portalRenderer.sortingLayerID = proceduralRenderer.sortingLayerID;
            portalRenderer.sortingOrder = proceduralRenderer.sortingOrder;
        }
    }

    private void Start()
    {
        if (frames != null && frames.Length > 0) StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        var wait = new WaitForSeconds(1f / Mathf.Max(1f, framesPerSecond));
        int frame = 0;
        while (true)
        {
            if (portalRenderer == null) yield break;
            portalRenderer.sprite = frames[frame];
            frame = (frame + 1) % frames.Length;
            yield return wait;
        }
    }
}
