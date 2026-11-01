using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
public sealed class MenuFlyingBirdsGraphic : MaskableGraphic
{
    [Range(3,12)] public int flockSize = 9;
    [Range(1,3)] public int flockCount = 2;
    [Range(8f,28f)] public float birdSize = 24f;
    [Min(10f)] public float crossingDuration = 28f;
    [Tooltip("Sky visibility mask: foreground trees hide birds through the texture alpha.")]
    public Texture2D skyVisibilityMask;
    private float clock;
    public override Texture mainTexture => skyVisibilityMask != null ? skyVisibilityMask : Texture2D.whiteTexture;

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

    struct Flight
    {
        public bool active;
        public int pass, count;
        public float phase, facing, seed;
    }

    static float Hash(float value) => Mathf.Repeat(Mathf.Sin(value*127.1f)*43758.5453f,1f);

    Flight GetFlight(int flock, float time)
    {
        float baseDuration = Mathf.Max(10f,crossingDuration);
        float interval = baseDuration+8f;
        float elapsed = time+baseDuration*.3f+flock*interval/Mathf.Max(1,flockCount);
        int pass = Mathf.FloorToInt(elapsed/interval);
        float seed = (pass+1)*53.17f+(flock+1)*29.31f;
        float delay = Hash(seed+2)*1.5f;
        float duration = baseDuration*Mathf.Lerp(.75f,1f,Hash(seed+4));
        float phase = (Mathf.Repeat(elapsed,interval)-delay)/duration;
        return new Flight
        {
            active = phase>=0 && phase<=1,
            pass = pass,
            count = Mathf.Clamp(flockSize-Mathf.FloorToInt(Hash(seed+9)*3),3,flockSize),
            phase = phase,
            facing = Hash(seed+7)<.32f ? -1f : 1f,
            seed = seed
        };
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = rectTransform.rect;
        for (int flock=0;flock<flockCount;flock++)
        {
            Flight flight = GetFlight(flock,clock);
            if (!flight.active) continue;
            float x = Mathf.Lerp(-.14f,1.14f,flight.phase);
            if (flight.facing<0) x=1-x;
            float y = .715f+Hash(flight.seed+3)*.025f+Mathf.Sin(flight.phase*Mathf.PI)*.025f;
            y += Mathf.Sin(flight.phase*Mathf.PI*2+flight.seed)*.005f;
            float fade = Mathf.SmoothStep(0,1,Mathf.Min(flight.phase,1-flight.phase)*12f);
            bool loose = Hash(flight.seed+13)>.5f;
            for (int n=0;n<flight.count;n++)
            {
                int row = (n+1)/2;
                float side = n%2==0 ? 1f : -1f;
                float seed = Hash(flight.seed+n*17.31f);
                float trailing = row*.023f+(n==0 ? 0 : seed*.007f);
                float spread = loose ? (Hash(flight.seed+n+35)-.5f)*.04f : row*.008f*side;
                float flutter = Mathf.Sin(clock*(.8f+seed*.3f)+n*1.9f)*.0025f;
                Vector2 center = rect.min+Vector2.Scale(new Vector2(x-flight.facing*trailing,y+spread+flutter),rect.size);
                bool glider = (n+flock+flight.pass)%2==1;
                float size = birdSize*Mathf.Lerp(.9f,1.1f,seed)*(glider?1.12f:1f)*rect.width/1280f;
                float phase = seed*6.283185f+n*1.7f;
                float lift, farLift;
                if (glider)
                {
                    // Broad wings alternate a short, slow flapping burst with a glide.
                    float burst = Mathf.Clamp01(Mathf.Repeat(clock+seed*7f,8f)/3f);
                    float strength = Mathf.Pow(Mathf.Sin(burst*Mathf.PI),2);
                    lift = Mathf.Lerp(.18f,Mathf.Sin(clock*(7f+seed)+phase)*.46f,strength);
                    farLift = Mathf.Lerp(.12f,Mathf.Sin(clock*(7f+seed)+phase-.45f)*.33f,strength);
                }
                else
                {
                    lift = Mathf.Sin(clock*(17f+seed*3f)+phase)*.4f;
                    farLift = Mathf.Sin(clock*(17f+seed*3f)+phase-.45f)*.29f;
                }
                Color tint = color;
                tint.a *= fade;
                if (glider) { tint.r+=.018f; tint.g+=.02f; tint.b+=.025f; }
                SideBird(mesh,center,size,flight.facing,lift,farLift,glider,tint);
            }
        }
        // The mask aligns with the full background rather than with each individual bird.
        UIVertex vertex = default;
        for (int n=0;n<mesh.currentVertCount;n++)
        {
            mesh.PopulateUIVertex(ref vertex,n);
            vertex.uv0 = new Vector4((vertex.position.x-rect.xMin)/Mathf.Max(1f,rect.width),
                (vertex.position.y-rect.yMin)/Mathf.Max(1f,rect.height),0,0);
            mesh.SetUIVertex(vertex,n);
        }
    }

    static void SideBird(VertexHelper mesh, Vector2 center, float size, float facing,
        float lift, float farLift, bool glider, Color tint)
    {
        float tip = glider ? -.43f : -.28f;
        AddShape(mesh,center,size,facing,tint,
            new Vector2(-.16f,.035f),new Vector2(-.43f,.09f),
            new Vector2(-.34f,-.09f),new Vector2(-.12f,-.04f));
        AddShape(mesh,center,size,facing,tint,
            new Vector2(-.02f,.03f),new Vector2(-.1f,farLift*.6f+.03f),
            new Vector2(tip*.85f,farLift),new Vector2(-.18f,farLift*.3f-.025f));
        AddShape(mesh,center,size,facing,tint,
            new Vector2(-.26f,.015f),new Vector2(-.1f,.1f),
            new Vector2(.2f,.045f),new Vector2(-.03f,-.085f));
        AddShape(mesh,center,size,facing,tint,
            new Vector2(.015f,.025f),new Vector2(-.1f,lift*.65f+.04f),
            new Vector2(tip,lift),new Vector2(-.16f,lift*.3f-.045f));
        AddShape(mesh,center,size,facing,tint,
            new Vector2(.1f,.04f),new Vector2(.22f,.14f),
            new Vector2(.3f,.095f),new Vector2(.265f,-.015f));
        int start = mesh.currentVertCount;
        mesh.AddVert(ProfilePoint(center,size,facing,new Vector2(.275f,.085f)),tint,Vector2.zero);
        mesh.AddVert(ProfilePoint(center,size,facing,new Vector2(.4f,.045f)),tint,Vector2.zero);
        mesh.AddVert(ProfilePoint(center,size,facing,new Vector2(.275f,.015f)),tint,Vector2.zero);
        mesh.AddTriangle(start,start+1,start+2);
    }

    static Vector2 ProfilePoint(Vector2 center, float size, float facing, Vector2 point)
        => center+new Vector2(point.x*facing,point.y)*size;

    static void AddShape(VertexHelper mesh, Vector2 center, float size, float facing, Color tint,
        Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        int start = mesh.currentVertCount;
        mesh.AddVert(ProfilePoint(center,size,facing,a),tint,Vector2.zero);
        mesh.AddVert(ProfilePoint(center,size,facing,b),tint,Vector2.zero);
        mesh.AddVert(ProfilePoint(center,size,facing,c),tint,Vector2.zero);
        mesh.AddVert(ProfilePoint(center,size,facing,d),tint,Vector2.zero);
        mesh.AddTriangle(start,start+1,start+2);
        mesh.AddTriangle(start,start+2,start+3);
    }
}
