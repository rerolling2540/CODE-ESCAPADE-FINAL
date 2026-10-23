using System.Collections;
using UnityEngine;

public sealed class BattleBloodEffect : MonoBehaviour
{
    private const string SheetPath = "BloodFX/VFX Blood Batch 1_SpriteSheetRows";
    private const int FrameWidth = 110;
    private const int FrameHeight = 93;
    private const int FramesInStrip = 14;
    private const float FramesPerSecond = 40f;
    private static Sprite[] impactFrames;

    public static void Play(CombatantView target)
    {
        if (target == null) return;

        if (impactFrames == null) impactFrames = LoadImpactFrames();
        if (impactFrames == null) return;

        Vector3 position = target.VisualCenter;
        position.x += Random.Range(-0.12f, 0.12f);
        position.y += Random.Range(-0.08f, 0.12f);
        position.z -= 0.05f;

        var effect = new GameObject("Battle Blood Impact");
        effect.transform.position = position;
        effect.transform.localScale = Vector3.one * Random.Range(0.9f, 1.12f);

        var renderer = effect.AddComponent<SpriteRenderer>();
        renderer.sprite = impactFrames[0];
        renderer.flipX = Random.value > 0.5f;
        var targetRenderer = target.GetComponentInChildren<SpriteRenderer>();
        if (targetRenderer != null)
        {
            renderer.sortingLayerID = targetRenderer.sortingLayerID;
            renderer.sortingOrder = targetRenderer.sortingOrder + 1;
        }
        else renderer.sortingOrder = 10;
        effect.AddComponent<BattleBloodEffect>().StartCoroutine(Animate(effect, renderer));
    }

    private static Sprite[] LoadImpactFrames()
    {
        Texture2D sheet = Resources.Load<Texture2D>(SheetPath);
        if (sheet == null || sheet.width < FrameWidth * FramesInStrip || sheet.height < FrameHeight)
        {
            Debug.LogWarning("Battle blood sprite sheet could not be loaded from Resources/BloodFX.");
            return null;
        }

        var frames = new Sprite[FramesInStrip];
        for (int i = 0; i < FramesInStrip; i++)
        {
            // The bottom row is the outward burst and fade sequence in the supplied sheet.
            frames[i] = Sprite.Create(sheet, new Rect(i * FrameWidth, 0, FrameWidth, FrameHeight),
                new Vector2(0.5f, 0.25f), FrameWidth, 0, SpriteMeshType.FullRect);
            frames[i].name = $"BattleBloodImpact_{i + 1}";
        }

        return frames;
    }

    private static IEnumerator Animate(GameObject effect, SpriteRenderer renderer)
    {
        float frameDuration = 1f / FramesPerSecond;
        for (int i = 0; i < impactFrames.Length; i++)
        {
            if (effect == null || renderer == null) yield break;
            renderer.sprite = impactFrames[i];
            yield return new WaitForSeconds(frameDuration);
        }

        if (effect != null) Destroy(effect);
    }
}
