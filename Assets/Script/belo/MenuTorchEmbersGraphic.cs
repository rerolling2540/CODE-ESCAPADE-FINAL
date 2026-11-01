using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
public sealed class MenuTorchEmbersGraphic : MaskableGraphic
{
    public Vector2[] sources = { new Vector2(.1756f,.438f), new Vector2(.5737f,.404f) };
    private float clock;

    public void SetAnimationTime(float time)
    {
        clock = time;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (sources == null) return;
        Rect rect = rectTransform.rect;
        float size = Mathf.Max(1f, rect.width / 1100f);
        for (int source = 0; source < sources.Length; source++)
        {
            for (int n = 0; n < 6; n++)
            {
                float seed = n * 1.37f + source * 2.19f;
                float age = Mathf.Repeat(clock * (.19f + n * .009f) + n / 6f + source * .31f, 1f);
                float alpha = Mathf.Sin(age * Mathf.PI) * .65f;
                float drift = Mathf.Sin(age * 4f + seed) * .006f + age * .006f;
                Vector2 normalized = sources[source] + new Vector2(drift, age * .085f);
                Vector2 position = rect.min + Vector2.Scale(normalized, rect.size);
                position = new Vector2(Mathf.Round(position.x), Mathf.Round(position.y));
                Color tint = new Color(1f, .42f + n * .055f, .1f, alpha) * color;
                int start = mesh.currentVertCount;
                mesh.AddVert(position + new Vector2(-size,-size),tint,Vector2.zero);
                mesh.AddVert(position + new Vector2(-size,size),tint,Vector2.zero);
                mesh.AddVert(position + new Vector2(size,size),tint,Vector2.zero);
                mesh.AddVert(position + new Vector2(size,-size),tint,Vector2.zero);
                mesh.AddTriangle(start,start+1,start+2);
                mesh.AddTriangle(start,start+2,start+3);
            }
        }
    }
}
