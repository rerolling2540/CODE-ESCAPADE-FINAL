using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways, RequireComponent(typeof(SpriteRenderer))]
public class BattlePlatform : MonoBehaviour
{
    public Camera battleCamera;
    public float groundOffset = -2.2f;
    [Min(0f)] public float sidePadding = 1.5f;
    private SpriteRenderer platform;
    private readonly List<CombatantView> combatants = new List<CombatantView>();
    private readonly List<SpriteRenderer> shadows = new List<SpriteRenderer>();
    private Texture2D shadowTexture;
    private Sprite shadowSprite;
    private float requiredWidth;

    void OnEnable() { platform = GetComponent<SpriteRenderer>(); FitPlatform(); }

    public void GroundCombatants(HeroView hero, List<EnemyView> enemies)
    {
        combatants.Clear();
        if (hero != null) combatants.Add(hero);
        if (enemies != null)
            foreach (var enemy in enemies) if (enemy != null) combatants.Add(enemy);
        if (battleCamera == null || combatants.Count == 0) return;
        float ground = float.PositiveInfinity;
        foreach (var fighter in combatants)
            ground = Mathf.Min(ground, fighter.VisualBounds.min.y - HoverClearance(fighter));
        groundOffset = ground - battleCamera.transform.position.y;
        foreach (var fighter in combatants)
        {
            float shift = ground + HoverClearance(fighter) - fighter.VisualBounds.min.y;
            fighter.transform.position += Vector3.up * shift;
        }
        requiredWidth = 0f;
        foreach (var fighter in combatants)
        {
            Bounds bounds = fighter.VisualBounds;
            float edge = Mathf.Max(Mathf.Abs(bounds.min.x - battleCamera.transform.position.x),
                Mathf.Abs(bounds.max.x - battleCamera.transform.position.x));
            requiredWidth = Mathf.Max(requiredWidth, (edge + sidePadding) * 2f);
        }
        FitPlatform();
        CreateShadows();
    }

    static float HoverClearance(CombatantView fighter) => fighter.IsHovering ? 0.55f : 0f;

    void LateUpdate()
    {
        FitPlatform();
        if (!Application.isPlaying) return;
        float ground = battleCamera != null ? battleCamera.transform.position.y + groundOffset : transform.position.y;
        for (int i = 0; i < shadows.Count; i++)
        {
            var fighter = combatants[i];
            var shadow = shadows[i];
            if (shadow == null) continue;
            shadow.enabled = fighter != null && fighter.CurrentHealth > 0;
            if (!shadow.enabled) continue;
            Bounds bounds = fighter.VisualBounds;
            shadow.transform.position = new Vector3(bounds.center.x, ground + 0.015f, transform.position.z - 0.1f);
            float width = Mathf.Clamp(bounds.size.x * 0.72f, 0.5f, 2.2f);
            shadow.transform.localScale = new Vector3(width / shadowSprite.bounds.size.x / transform.localScale.x,
                0.14f / shadowSprite.bounds.size.y / transform.localScale.y, 1f);
            shadow.color = new Color(0.025f, 0.02f, 0.045f, fighter.IsHovering ? 0.24f : 0.55f);
        }
    }

    void FitPlatform()
    {
        if (battleCamera == null || platform == null || platform.sprite == null) return;
        // Keep the stage steady while combatants lunge during attacks.
        float width = Mathf.Max(requiredWidth, battleCamera.orthographicSize * 2f * battleCamera.aspect * 1.04f);
        float scale = width / Mathf.Max(0.01f, platform.sprite.bounds.size.x);
        transform.position = new Vector3(battleCamera.transform.position.x,
            battleCamera.transform.position.y + groundOffset, 1f);
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    void CreateShadows()
    {
        foreach (var shadow in shadows) if (shadow != null) Destroy(shadow.gameObject);
        shadows.Clear();
        if (shadowSprite == null)
        {
            shadowTexture = new Texture2D(64, 16, TextureFormat.RGBA32, false);
            shadowTexture.filterMode = FilterMode.Bilinear;
            shadowTexture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 64; x++)
                {
                    float dx = (x - 31.5f) / 31.5f, dy = (y - 7.5f) / 7.5f;
                    float alpha = Mathf.Clamp01(1f - dx * dx - dy * dy);
                    shadowTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                }
            shadowTexture.Apply();
            shadowSprite = Sprite.Create(shadowTexture, new Rect(0, 0, 64, 16), new Vector2(0.5f, 0.5f), 100f);
        }
        foreach (var fighter in combatants)
        {
            var item = new GameObject("Contact Shadow - " + fighter.name);
            item.hideFlags = HideFlags.DontSave;
            item.transform.SetParent(transform, false);
            var shadow = item.AddComponent<SpriteRenderer>();
            shadow.sprite = shadowSprite;
            shadow.sortingLayerID = platform.sortingLayerID;
            shadow.sortingOrder = platform.sortingOrder + 1;
            shadow.color = Color.clear;
            shadows.Add(shadow);
        }
    }

    void OnDestroy()
    {
        Release(shadowSprite);
        Release(shadowTexture);
    }

    void Release(Object item)
    {
        if (item == null) return;
        if (Application.isPlaying) Destroy(item);
        else DestroyImmediate(item);
    }
}
