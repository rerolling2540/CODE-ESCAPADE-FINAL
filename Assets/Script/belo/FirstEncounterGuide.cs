using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Non-blocking first-encounter guidance; completion belongs to the active save slot.</summary>
public sealed class FirstEncounterGuide : MonoBehaviour
{
    public const string ReferenceText=
        "<b>NPCs — A FELLOW TRAVELLER</b>\nApproach an NPC and press E to speak. Dialogue may offer quests, guidance, or services.\n\n"+
        "<b>THE SYNTAX MERCHANT</b>\nApproach and press E to talk. Trade your currencies for cards and supplies.\n\n"+
        "<b>THE BLACKSMITH</b>\nPress E nearby to talk. Refine your cards using duplicates and coins.\n\n"+
        "<b>TRAPS — WATCH YOUR STEP</b>\nTraps damage your health; electricity can be deadly. Keep clear, time your jumps, and look for a nearby puzzle lever.\n\n"+
        "<b>DISABLE THE ELECTRICITY</b>\nPress E beside the lever and solve its coding puzzle. A correct answer permanently disables the linked trap in this save.\n\n"+
        "<b>AN ENEMY AHEAD</b>\nTouching a monster begins a card battle. Defeat the encounter to earn XP and progress toward your next level.\n\n"+
        "<b>THE DEMON DOOR</b>\nPress E at the door and answer its coding question. A wrong answer gives another question; a correct answer opens the way ahead.\n\n"+
        "<b>BOSS ENCOUNTER</b>\nThe Undead Executioner awaits. Check your health and refined cards before approaching. Touching the boss begins battle.\n\n"+
        "<b>A TRAVELLER'S TREASURE</b>\nPress E near an unopened chest to collect its card reward. Find your new cards in the inventory with I.\n\n"+
        "<b>A PASSAGE AHEAD</b>\nStand near a portal and press E, or click its interaction button, to travel onward.";
    private static FirstEncounterGuide instance;
    private sealed class Tip
    {
        public Behaviour target;
        public string id,title,body;
        public float radius;
    }
    private readonly List<Tip> targets=new List<Tip>();
    private readonly HashSet<string> previewSeen=new HashSet<string>();
    private Tip current;
    private float remaining,scanAt;
    private int slot=-1;
    private playerMovement player;
    private GUIStyle panel,titleStyle,bodyStyle,button;
    public static void EnsureInstance()
    {
        if(instance!=null)return;
        instance=new GameObject("First Encounter Tutorials").AddComponent<FirstEncounterGuide>();
        DontDestroyOnLoad(instance.gameObject);
    }
    void OnEnable(){SceneManager.sceneLoaded+=SceneLoaded;}
    void OnDisable(){SceneManager.sceneLoaded-=SceneLoaded;}
    void SceneLoaded(Scene scene,LoadSceneMode mode){targets.Clear();player=null;current=null;scanAt=Time.unscaledTime+.75f;}
    void Add(Behaviour target,string id,string title,string body,float radius)
    {targets.Add(new Tip{target=target,id=id,title=title,body=body,radius=radius});}
    void Scan()
    {
        targets.Clear();
        foreach(var npc in FindObjectsByType<NPC>(FindObjectsSortMode.None))
        {
            string name=npc.name.ToLowerInvariant();
            if(name.Contains("merchant"))Add(npc,"merchant","THE SYNTAX MERCHANT","Approach and press E to talk. Trade your currencies for cards and supplies.",4.5f);
            else if(name.Contains("blacksmith"))Add(npc,"blacksmith","THE BLACKSMITH","Press E nearby to talk. Refine your cards using duplicates and coins.",4.5f);
            else Add(npc,"npc","A FELLOW TRAVELLER","Approach an NPC and press E to speak. Dialogue may offer quests, guidance, or services.",4.5f);
        }
        foreach(var lever in FindObjectsByType<ElectricalTrapSwitch2D>(FindObjectsSortMode.None))
            if(lever.IsPowered)Add(lever,"lever","DISABLE THE ELECTRICITY","Press E beside the lever and solve its coding puzzle. A correct answer permanently disables the linked trap in this save.",6);
        foreach(var trap in FindObjectsByType<Map4TrapDamageTrigger>(FindObjectsSortMode.None))
            Add(trap,"trap","WATCH YOUR STEP","Traps damage your health; electricity can be deadly. Keep clear, time your jumps, and look for a nearby puzzle lever.",6);
        foreach(var door in FindObjectsByType<DemonDoor>(FindObjectsSortMode.None))
            Add(door,"demon-door","THE DEMON DOOR","Press E at the door and answer its coding question. A wrong answer gives another question; a correct answer opens the way ahead.",7);
        foreach(var enemy in FindObjectsByType<CaveEnemyEncounter>(FindObjectsSortMode.None))
        {
            bool boss=enemy.name.ToLowerInvariant().Contains("boss")||(enemy.enemy!=null&&enemy.enemy.name.ToLowerInvariant().Contains("executioner"));
            Add(enemy,boss?"boss":"enemy",boss?"BOSS ENCOUNTER":"AN ENEMY AHEAD",boss?"The Undead Executioner awaits. Check your health and refined cards before approaching. Touching the boss begins battle.":"Touching a monster begins a card battle. Defeat the encounter to earn XP and progress toward your next level.",boss?10:7);
        }
        foreach(var chest in FindObjectsByType<CardRewardChest>(FindObjectsSortMode.None))
            Add(chest,"chest","A TRAVELLER'S TREASURE","Press E near an unopened chest to collect its card reward. Find your new cards in the inventory with I.",4);
        foreach(var portal in FindObjectsByType<PortalInteraction>(FindObjectsSortMode.None))
            if(portal.GetComponent<DemonDoor>()==null)Add(portal,"portal","A PASSAGE AHEAD","Stand near a portal and press E, or click its interaction button, to travel onward.",4);
    }
    bool Seen(string id)
    {
        var saved=SaveSlots.Read(SaveSlots.Active);
        return saved!=null?saved.seenTutorialTips!=null&&saved.seenTutorialTips.Contains(id):previewSeen.Contains(SaveSlots.Active+":"+id);
    }
    void Show(Tip tip)
    {
        current=tip;remaining=8;
        var saved=SaveSlots.Read(SaveSlots.Active);
        if(saved==null){previewSeen.Add(SaveSlots.Active+":"+tip.id);return;}
        if(saved.seenTutorialTips==null)saved.seenTutorialTips=new List<string>();
        if(!saved.seenTutorialTips.Contains(tip.id)){saved.seenTutorialTips.Add(tip.id);SaveSlots.Write(saved);}
    }
    bool Hidden=>!CharacterAlmanac.IsExplorationScene(SceneManager.GetActiveScene().name)||GameControlsGuide.MapHidden||SceneTransitionFader.IsTransitioning;
    bool Relevant(Tip tip)
    {
        if(player==null||tip==null||tip.target==null||!tip.target.gameObject.activeInHierarchy||tip.target.gameObject.scene!=player.gameObject.scene)return false;
        var cycle=tip.target.GetComponent<TimedTrapCycle2D>();
        if(cycle!=null&&tip.id=="trap") {if(cycle.CurrentPhase==TimedTrapCycle2D.Phase.Disabled)return false;}
        else if(!tip.target.isActiveAndEnabled)return false;
        if(tip.target is ElectricalTrapSwitch2D lever&&!lever.IsPowered)return false;
        if(tip.target is CardRewardChest chest&&chest.IsOpened)return false;
        if(tip.target is DemonDoor door&&door.IsOpened)return false;
        var visual=tip.target.GetComponentInChildren<SpriteRenderer>();
        var collider=tip.target.GetComponent<Collider2D>();
        Vector3 anchor=visual!=null&&visual.enabled?visual.bounds.center:collider!=null?collider.bounds.center:tip.target.transform.position;
        var body=player.GetComponent<Collider2D>();Vector3 origin=body!=null?body.bounds.center:player.transform.position;
        float radius=Mathf.Min(tip.radius,tip.id=="boss"?8f:tip.id=="trap"?4f:5f);
        if(Vector2.Distance(origin,anchor)>radius)return false;
        var camera=Camera.main;if(camera==null)return false;
        Vector3 viewport=camera.WorldToViewportPoint(anchor);
        if(viewport.z<=0||viewport.x<0||viewport.x>1||viewport.y<0||viewport.y>1)return false;
        // A nearby trap in a cave below the village is not an encounter.
        foreach(var hit in Physics2D.LinecastAll(origin,anchor))
        {
            if(hit.collider==null||hit.collider.isTrigger||hit.transform.IsChildOf(player.transform)||hit.transform.IsChildOf(tip.target.transform))continue;
            return false;
        }
        return true;
    }
    void Update()
    {
        if(slot!=SaveSlots.Active){slot=SaveSlots.Active;current=null;scanAt=0;}
        if(Hidden)return;
        if(player==null)player=FindFirstObjectByType<playerMovement>();
        if(player==null)return;
        if(current!=null){remaining-=Time.unscaledDeltaTime;if(remaining<=0||!Relevant(current))current=null;else return;}
        if(Time.unscaledTime>=scanAt){Scan();scanAt=Time.unscaledTime+2;}
        Tip nearest=null;float best=float.PositiveInfinity;
        foreach(var tip in targets)
        {
            if(!Relevant(tip)||Seen(tip.id))continue;
            float distance=Vector2.Distance(player.transform.position,tip.target.transform.position);
            if(distance<=tip.radius&&distance<best){nearest=tip;best=distance;}
        }
        if(nearest!=null)Show(nearest);
    }
    void OnGUI()
    {
        if(current==null||Hidden||!Relevant(current))return;
        if(panel==null)
        {
            panel=new GUIStyle(GUI.skin.box);MedievalNotice.Apply(panel);
            titleStyle=new GUIStyle(GUI.skin.label){font=panel.font,fontSize=23,fontStyle=FontStyle.Bold};titleStyle.normal.textColor=new Color(.26f,.11f,.025f);UiTextColors.Stable(titleStyle);
            bodyStyle=new GUIStyle(titleStyle){fontSize=19,fontStyle=FontStyle.Normal,wordWrap=true};
            button=new GUIStyle(GUI.skin.button){font=panel.font,fontSize=17,fontStyle=FontStyle.Bold};MedievalNotice.Apply(button);button.padding=new RectOffset(6,6,4,4);
        }
        var matrix=GUI.matrix;var color=GUI.color;
        float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
        try{
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2,0),Quaternion.identity,Vector3.one*scale);GUI.color=Color.white;
            GUI.Box(new Rect(285,540,710,156),GUIContent.none,panel);
            GUI.Label(new Rect(309,554,536,34),current.title,titleStyle);
            GUI.Label(new Rect(309,592,660,86),current.body,bodyStyle);
            if(GUI.Button(new Rect(865,552,106,36),"Got it",button))current=null;
        }finally{GUI.matrix=matrix;GUI.color=color;}
    }
    void OnDestroy(){if(instance==this)instance=null;}
}
