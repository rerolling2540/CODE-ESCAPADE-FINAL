using UnityEngine;
using TMPro;

/// <summary>Small scene-authored demonstrations beside the elf. Saved answers animate their world state.</summary>
public sealed class WardenVisualAid : MonoBehaviour
{
    public string proof;
    public SpriteRenderer[] symbols;
    public TMP_Text caption;
    public string beforeText,afterText;
    public Sprite[] fireFrames;
    public Npc_Talk teacher;
    public bool contextualDisplay;
    private float reveal;
    void Awake(){if(teacher==null)teacher=GetComponentInParent<Npc_Talk>();HideVisuals();}
    void OnEnable()=>HideVisuals();
    void OnDisable()=>HideVisuals();
    void HideVisuals()
    {
        if(symbols!=null)foreach(var symbol in symbols)if(symbol!=null)symbol.enabled=false;
        if(caption!=null)caption.enabled=false;
    }
    public static bool IsLessonActive(string proof,string activeProof,bool sameTeacher,bool dialogueOpen,bool overlayHidden)
        =>!string.IsNullOrEmpty(proof)&&proof==activeProof&&sameTeacher&&dialogueOpen&&!overlayHidden;
    void LateUpdate()
    {
        var dialogue=DialogueManager.Instance;
        bool visible=IsLessonActive(proof,dialogue!=null?dialogue.ActiveLearningVisualProof:null,
            teacher!=null&&dialogue!=null&&dialogue.Owner==teacher,dialogue!=null&&dialogue.isDialogueActive,GameControlsGuide.GameplayOverlayHidden);
        if(!visible){HideVisuals();return;}
        bool solved=WardenQuest.Has(SaveSlots.Read(SaveSlots.Active),proof);
        reveal=Mathf.MoveTowards(reveal,solved?1:0,Time.deltaTime*2);
        if(contextualDisplay)
        {
            var player=FindFirstObjectByType<playerMovement>();var save=SaveSlots.Read(SaveSlots.Active);
            int chapter=proof=="supplies"?0:proof=="gate"?2:proof=="fires"?3:4;
            visible&=save!=null&&save.wardenAccepted&&WardenQuest.Chapter(save)==chapter&&player!=null&&teacher!=null&&Vector2.Distance(player.transform.position,teacher.transform.position)<12;
            if(proof=="shelf")visible&=!WardenQuest.Has(save,"shelf");
            if(proof=="heal")visible&=WardenQuest.Has(save,"shelf");
        }
        for(int i=0;i<symbols.Length;i++)if(symbols[i]!=null)
        {
            float amount=proof=="fires"?Mathf.Clamp01(reveal*4-i):reveal;
            symbols[i].enabled=visible;symbols[i].color=Color.Lerp(new Color(.35f,.35f,.35f),Color.white,amount);
            if(proof=="fires"&&fireFrames!=null&&fireFrames.Length>0&&amount>0)symbols[i].sprite=fireFrames[Mathf.FloorToInt(Time.time*8+i)%fireFrames.Length];
            if(proof=="gate")symbols[i].transform.localPosition=Vector3.up*reveal;
        }
        if(caption!=null){caption.enabled=visible;caption.text=solved?afterText:beforeText;}
    }
}
