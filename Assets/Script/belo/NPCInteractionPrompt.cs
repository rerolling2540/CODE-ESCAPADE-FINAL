using UnityEngine;

[RequireComponent(typeof(NPC))]
public sealed class NPCInteractionPrompt : MonoBehaviour
{
    public float height = 2.35f;
    private NPC npc;
    private GUIStyle wood, paper, title, hint, key;
    private Font font;
    void Awake() { npc = GetComponent<NPC>(); }

    void EnsureStyles()
    {
        if(title!=null)return;
        font=Font.CreateDynamicFontFromOSFont("Georgia",24);
        wood=new GUIStyle(GUI.skin.box){border=new RectOffset(10,10,10,10)};
        wood.normal.background=GameControlsGuide.Frame(false).texture;
        paper=new GUIStyle(wood);paper.normal.background=GameControlsGuide.Frame(true).texture;
        title=new GUIStyle(GUI.skin.label){font=font,fontSize=22,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(),clipping=TextClipping.Clip};
        title.normal.textColor=new Color(.14f,.065f,.025f);UiTextColors.Stable(title);
        hint=new GUIStyle(title){fontSize=15,fontStyle=FontStyle.Normal};
        hint.normal.textColor=new Color(.30f,.16f,.065f);UiTextColors.Stable(hint);
        key=new GUIStyle(title){fontSize=27,alignment=TextAnchor.MiddleCenter};
        key.normal.textColor=new Color(1f,.87f,.5f);UiTextColors.Stable(key);
    }

    void OnGUI()
    {
        if(npc==null||npc.currentState!=NPC.NPCState.Talk||GameControlsGuide.MapHidden||Camera.main==null)return;
        Vector3 screen=Camera.main.WorldToScreenPoint(transform.position+Vector3.up*height);
        if(screen.z<=0)return;
        EnsureStyles();
        string actorName=npc.talk!=null&&npc.talk.actor!=null?npc.talk.actor.actorName:gameObject.name;
        string role=actorName.ToLowerInvariant();
        string action=role.Contains("merchant")?"Talk / Trade cards":role.Contains("blacksmith")?"Talk / Refine cards":"Talk to traveller";
        float scale=Mathf.Clamp(Screen.height/720f,.75f,2f);
        const float width=350f, panelHeight=76f;
        float x=Mathf.Clamp(screen.x/scale-width/2,8,Mathf.Max(8,Screen.width/scale-width-8));
        float y=Mathf.Clamp((Screen.height-screen.y)/scale-panelHeight-8,8,Mathf.Max(8,Screen.height/scale-panelHeight-8));
        Matrix4x4 previousMatrix=GUI.matrix;Color previousColor=GUI.color;int previousDepth=GUI.depth;
        try
        {
            GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);GUI.depth=-20;
            GUI.color=new Color(0,0,0,.45f);GUI.DrawTexture(new Rect(x+4,y+5,width,panelHeight),Texture2D.whiteTexture);
            GUI.color=Color.white;
            GUI.Box(new Rect(x,y,width,panelHeight),GUIContent.none,wood);
            GUI.Box(new Rect(x+7,y+7,width-14,panelHeight-14),GUIContent.none,paper);
            GUI.Box(new Rect(x+15,y+15,48,46),GUIContent.none,wood);
            GUI.Label(new Rect(x+15,y+15,48,46),"E",key);
            GUI.Label(new Rect(x+77,y+13,width-96,29),actorName,title);
            GUI.Label(new Rect(x+78,y+43,width-96,22),action,hint);
        }
        finally{GUI.matrix=previousMatrix;GUI.color=previousColor;GUI.depth=previousDepth;}
    }

    void OnDestroy(){if(font!=null)Destroy(font);}
}
