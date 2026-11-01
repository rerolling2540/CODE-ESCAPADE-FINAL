using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
public sealed class MenuBackgroundWindGraphic : MaskableGraphic
{
    [Range(0,60)] public int leafCount = 22;
    [Range(.1f,2f)] public float windSpeed = .7f;
    private float clock;

    protected override void OnEnable()
    {
        base.OnEnable();
        raycastTarget = false;
    }

    void Update() => SetAnimationTime(clock + Time.unscaledDeltaTime);

    public void SetAnimationTime(float time)
    {
        clock = time;
        SetVerticesDirty();
    }

    static float Hash(float value) => Mathf.Repeat(Mathf.Sin(value * 127.1f) * 43758.5453f,1f);

    // A passing gust lasts four seconds, with a quiet interval between gusts.
    static void Gust(float time, out float strength, out float carried)
    {
        const float interval = 12f, start = 2f, duration = 4f;
        float cycle = Mathf.Floor(time / interval);
        float phase = Mathf.Repeat(time,interval);
        float progress = Mathf.Clamp01((phase-start)/duration);
        strength = Mathf.Pow(Mathf.Sin(progress*Mathf.PI),2f);
        // Integrate the gust so leaves keep moving forward instead of snapping back.
        carried = cycle*duration*.5f + progress*duration*.5f -
            duration*Mathf.Sin(progress*Mathf.PI*2f)/(4f*Mathf.PI);
    }

    static Vector2 LeafPosition(int index, float time, out float gust, out float carried)
    {
        float seed = Hash(index+1);
        float initialX = Hash(index+91.7f);
        float initialY = Hash(index+233.4f);
        float delay = initialX*1.3f;
        Gust(time-delay,out gust,out carried);
        float x = initialX + time*(.012f+Hash(index+14)*.008f) + carried*.11f;
        float y = initialY - time*(.018f+Hash(index+53)*.014f) + carried*.1f;
        float swirl = time*(1.5f+Hash(index+117)*1.3f) + seed*6.283185f;
        x += Mathf.Cos(swirl)*gust*(.025f+Hash(index+29)*.025f);
        y += Mathf.Sin(swirl)*gust*(.03f+Hash(index+39)*.035f);
        x += Mathf.Sin(time*.8f+seed*20f)*.008f;
        return new Vector2(Mathf.Repeat(x+.07f,1.14f)-.07f,
            Mathf.Repeat(y+.06f,1.12f)-.06f);
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = rectTransform.rect;
        float scale = rect.width / 1280f;
        float time = clock * windSpeed;
        for (int n=0;n<leafCount;n++)
        {
            float seed = Hash(n+1);
            Vector2 normalized = LeafPosition(n,time,out float gust,out float carried);
            Vector2 position = rect.min + Vector2.Scale(normalized,rect.size);
            float angle = seed*6.283185f + time*(.35f+seed*.4f) +
                carried*(4f+seed*4f) + Mathf.Sin(time*4f+n)*gust*.8f;
            float size = (4f + Hash(n+72) * 4f) * scale;
            float fold = .35f + .65f * Mathf.Abs(Mathf.Cos(time*1.4f + carried*3f + n));
            Vector2 right = new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)) * size;
            Vector2 up = new Vector2(-Mathf.Sin(angle),Mathf.Cos(angle)) * size * fold;
            float edge = Mathf.Min(Mathf.Min(normalized.x,1-normalized.x),
                Mathf.Min(normalized.y,1-normalized.y));
            float fade = Mathf.SmoothStep(0,1,Mathf.Clamp01(edge/.05f));
            Color tint = Color.Lerp(new Color(.48f,.58f,.22f),new Color(.91f,.62f,.24f),seed);
            tint.a = .72f * fade;
            tint *= color;
            Leaf(mesh,position,right,up,tint);
            Line(mesh,position-right*.95f,position+right*.55f,Mathf.Max(.65f,scale),new Color(.28f,.22f,.08f,tint.a*.65f));
        }
    }

    static void Leaf(VertexHelper mesh, Vector2 p, Vector2 right, Vector2 up, Color tint)
    {
        int start = mesh.currentVertCount;
        mesh.AddVert(p-right,tint,Vector2.zero);
        mesh.AddVert(p-right*.3f+up*.5f,tint,Vector2.zero);
        mesh.AddVert(p+right*.5f+up*.35f,tint,Vector2.zero);
        mesh.AddVert(p+right,tint,Vector2.zero);
        mesh.AddVert(p+right*.2f-up*.45f,tint,Vector2.zero);
        mesh.AddVert(p-right*.6f-up*.28f,tint,Vector2.zero);
        for(int n=1;n<5;n++) mesh.AddTriangle(start,start+n,start+n+1);
    }

    static void Line(VertexHelper mesh, Vector2 from, Vector2 to, float width, Color tint)
    {
        Vector2 delta = to-from;
        if(delta.sqrMagnitude<.001f) return;
        Vector2 normal = new Vector2(-delta.y,delta.x).normalized*width*.5f;
        int start = mesh.currentVertCount;
        mesh.AddVert(from-normal,tint,Vector2.zero);
        mesh.AddVert(from+normal,tint,Vector2.zero);
        mesh.AddVert(to+normal,tint,Vector2.zero);
        mesh.AddVert(to-normal,tint,Vector2.zero);
        mesh.AddTriangle(start,start+1,start+2);
        mesh.AddTriangle(start,start+2,start+3);
    }
}
