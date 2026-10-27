using UnityEngine;

[RequireComponent(typeof(NPC))]
public sealed class NPCInteractionPrompt : MonoBehaviour
{
    public float height = 2.35f;
    private NPC npc;
    private GUIStyle style;
    void Awake() { npc = GetComponent<NPC>(); }
    void OnGUI()
    {
        if (npc == null || npc.currentState != NPC.NPCState.Talk || GameControlsGuide.MapHidden || Camera.main == null) return;
        var screen=Camera.main.WorldToScreenPoint(transform.position+Vector3.up*height);
        if(screen.z<=0)return;
        if(style==null){style=new GUIStyle(GUI.skin.box){fontSize=18,alignment=TextAnchor.MiddleCenter};style.normal.textColor=new Color(1,.87f,.55f);}
        string name=npc.talk!=null&&npc.talk.actor!=null?npc.talk.actor.actorName:gameObject.name;
        GUI.Box(new Rect(screen.x-125,Screen.height-screen.y-20,250,40),"[E] Talk to "+name,style);
    }
}
