using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class QuestReturnPanel : MonoBehaviour
{
    public sealed class Destination {public int giver;public string scene,title,key,reward;}
    private readonly HashSet<string> dismissed=new HashSet<string>();
    private GameObject root;
    private CanvasGroup group;
    private TMP_Text message;
    private float nextCheck,previousTime=1;
    private bool opened;
    private string context;
    public static bool IsOpen {get;private set;}
    public static List<Destination> Ready(SaveSlots.Record s)
    {
        var result=new List<Destination>();
        if(WardenQuest.Ready(s))result.Add(new Destination{giver=-1,scene=WardenQuest.Maps[WardenQuest.Chapter(s)],title="Elf — "+WardenQuest.Titles[WardenQuest.Chapter(s)],key="main-"+WardenQuest.Chapter(s),reward=WardenQuest.RewardText(WardenQuest.Chapter(s))});
        for(int i=0;i<3;i++)if(LearningQuests.Accepted(s,i)&&!LearningQuests.Claimed(s,i)&&LearningQuests.Completed(s,i)==2)result.Add(new Destination{giver=i,scene=new[]{"Whispering Hollow","Shadowroot Depths","Frostbound Passage"}[i],title=new[]{"Alden","Rowan","Mira"}[i]+" — "+LearningQuests.Title(i),key="teacher-"+i,reward=LearningQuests.Reward(i)+" XP"});
        if(s!=null&&s.elfQuestStage>0&&s.elfQuestStage<4&&ElfQuestProgress.Ready(s))result.Add(new Destination{giver=-1,scene="Whispering Hollow",title="Elf — Earlier cave contract",key="legacy-"+s.elfQuestStage,reward="Cave contract reward"});
        for(int i=0;i<2;i++)if(HuntQuestProgress.Ready(s,i)){int stage=HuntQuestProgress.Stage(s,i);result.Add(new Destination{giver=HuntQuestProgress.ReturnGiverOffset+i,scene=HuntQuestProgress.Scene(i),title=HuntQuestProgress.Name(i)+" — "+HuntQuestProgress.Get(i,stage).title,key=HuntQuestProgress.Key(i,stage),reward=HuntQuestProgress.Reward(HuntQuestProgress.Get(i,stage))});}
        return result;
    }
    public static Npc_Talk FindNpc(int giver)
    {
        var player=Object.FindFirstObjectByType<playerMovement>();
        return Object.FindObjectsByType<Npc_Talk>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Where(n=>n.gameObject.scene==SceneManager.GetActiveScene()&&n.gameObject.activeInHierarchy&&(giver<0?n.offersElfQuest:giver>=HuntQuestProgress.ReturnGiverOffset?n.GetComponent<HuntQuestNPC>()?.giver==giver-HuntQuestProgress.ReturnGiverOffset:n.GetComponent<CodeMentorNPC>()?.workshop==giver))
            .OrderBy(n=>player!=null?Vector2.Distance(player.transform.position,n.transform.position):n.name=="Elf - Warden Guide"?0:1).FirstOrDefault();
    }
    public static bool Far(Destination d,playerMovement player)
    {
        if(player==null)return false;var npc=FindNpc(d.giver);
        return npc==null||Vector2.Distance(player.transform.position,npc.transform.position)>18;
    }
    string Context=>MainMenuSaveSlots.CurrentUserId+"/"+SaveSlots.Active+"/"+LearningQuests.Language;
    void Update()
    {
        if(context!=Context){Close();dismissed.Clear();context=Context;}
        if(opened)
        {
            if(group!=null){group.alpha=GameControlsGuide.SettingsOpen?0:1;group.interactable=group.blocksRaycasts=!GameControlsGuide.SettingsOpen;}
            if(!GameControlsGuide.SettingsOpen){Time.timeScale=0;if(Input.GetKeyDown(KeyCode.Escape))Dismiss();}
            return;
        }
        if(Time.unscaledTime<nextCheck)return;nextCheck=Time.unscaledTime+.6f;
        if(!CanShow())return;
        var player=FindFirstObjectByType<playerMovement>();var ready=Ready(SaveSlots.Read(SaveSlots.Active));
        if(ready.Any(d=>!dismissed.Contains(d.key)&&Far(d,player)))Show(ready);
    }
    bool CanShow()=>CharacterAlmanac.IsExplorationScene(SceneManager.GetActiveScene().name)&&Time.timeScale>0&&!GameControlsGuide.GameplayOverlayHidden&&!(DialogueManager.Instance!=null&&DialogueManager.Instance.isDialogueActive)&&!SceneTransitionFader.IsTransitioning;
    public void OpenManually(){if(CanShow())Show(Ready(SaveSlots.Read(SaveSlots.Active)));}
    void Show(List<Destination> destinations,bool preview=false)
    {
        if(opened)return;if(!preview){previousTime=Time.timeScale;opened=IsOpen=true;Time.timeScale=0;}
        root=new GameObject("Quest Reward Return",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster),typeof(CanvasGroup));
        root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;root.GetComponent<Canvas>().sortingOrder=1100;group=root.GetComponent<CanvasGroup>();
        var scaler=root.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
        var shade=Box("Backdrop",root.transform,new Color(0,0,0,.65f));Stretch(shade.rectTransform);
        var frame=Box("Quest return panel",shade.transform,new Color(.25f,.15f,.07f));Center(frame.rectTransform,780,660);
        var paper=Box("Parchment",frame.transform,new Color(.91f,.83f,.65f));Stretch(paper.rectTransform,8);
        Label(destinations.Count==0?"QUEST JOURNAL":"QUEST REWARD READY",paper.transform,28,20,708,44,30,true);
        message=Label(destinations.Count==0?"No rewards are ready yet. Finish a quest objective first.":"Your reward is ready. Choose an NPC below.\nYou can return later from the quest journal.",paper.transform,28,72,708,66,23,false);
        var list=Box("Scrollable rewards",paper.transform,new Color(1,1,1,0));Place(list.rectTransform,28,150,708,420);
        var scroll=list.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();scroll.horizontal=false;scroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;
        var view=Box("Viewport",list.transform,Color.white);Stretch(view.rectTransform);view.rectTransform.offsetMax=new Vector2(-24,0);view.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic=false;
        var content=new GameObject("Content",typeof(RectTransform),typeof(UnityEngine.UI.VerticalLayoutGroup),typeof(UnityEngine.UI.ContentSizeFitter));content.transform.SetParent(view.transform,false);
        var contentRect=content.GetComponent<RectTransform>();contentRect.anchorMin=new Vector2(0,1);contentRect.anchorMax=Vector2.one;contentRect.pivot=new Vector2(.5f,1);contentRect.sizeDelta=Vector2.zero;
        var layout=content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();layout.spacing=8;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandHeight=false;
        content.GetComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport=view.rectTransform;scroll.content=contentRect;
        var track=Box("Scrollbar",list.transform,new Color(.27f,.16f,.07f));Place(track.rectTransform,690,0,18,420);
        var handle=Box("Handle",track.transform,new Color(.84f,.63f,.30f));Stretch(handle.rectTransform);
        var bar=track.gameObject.AddComponent<UnityEngine.UI.Scrollbar>();bar.direction=UnityEngine.UI.Scrollbar.Direction.BottomToTop;bar.handleRect=handle.rectTransform;bar.targetGraphic=handle;scroll.verticalScrollbar=bar;scroll.scrollSensitivity=35;
        if(destinations.Count>5)message.text="Your rewards are ready. Scroll the list to choose an NPC.\nYou can return later from the quest journal.";
        var player=FindFirstObjectByType<playerMovement>();
        for(int i=0;i<destinations.Count;i++)
        {
            var d=destinations[i];bool far=Far(d,player);
            var localNpc=FindNpc(d.giver);string map=localNpc!=null?localNpc.gameObject.scene.name:d.scene;
            var b=Button(d.title+"\n"+(far?"Travel to "+map:"Nearby — close this panel and press E"),content.transform,0,0,684,76,()=>Travel(d));b.interactable=far;
            b.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight=76;
        }
        Button("Return later",paper.transform,222,580,320,52,Dismiss);
        if(!preview){GameControlsGuide.RegisterMapBlocker(root);SettingsUiVisibility.EnsureInstance();SettingsUiVisibility.RefreshNow();}
    }
    public GameObject BuildPreview(List<Destination> destinations){if(Application.isPlaying)throw new System.InvalidOperationException("Preview is only available outside Play Mode.");Show(destinations,true);return root;}
    void Travel(Destination d)
    {
        if(!Ready(SaveSlots.Read(SaveSlots.Active)).Any(x=>x.key==d.key)){message.text="This reward has already been collected.";return;}
        dismissed.Add(d.key);Close();QuestReturnTravel.Begin(d.giver,d.scene);
    }
    void Dismiss(){foreach(var d in Ready(SaveSlots.Read(SaveSlots.Active)))dismissed.Add(d.key);Close();}
    void Close(){if(!opened)return;opened=IsOpen=false;if(root!=null)Destroy(root);if(!GameControlsGuide.SettingsOpen)Time.timeScale=previousTime;}
    void OnDisable(){Close();}
    static UnityEngine.UI.Image Box(string name,Transform parent,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(parent,false);var image=go.GetComponent<UnityEngine.UI.Image>();image.color=color;return image;}
    static void Stretch(RectTransform r,float inset=0){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.one*inset;r.offsetMax=-Vector2.one*inset;}
    static void Center(RectTransform r,float w,float h){r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(w,h);}
    static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
    static TMP_Text Label(string value,Transform parent,float x,float y,float w,float h,float size,bool bold){var go=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var t=go.GetComponent<TextMeshProUGUI>();Place(t.rectTransform,x,y,w,h);t.font=Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");t.text=value;t.fontSize=size;t.fontStyle=bold?FontStyles.Bold:FontStyles.Normal;t.color=new Color(.19f,.11f,.05f);t.raycastTarget=false;return t;}
    static UnityEngine.UI.Button Button(string text,Transform parent,float x,float y,float w,float h,UnityEngine.Events.UnityAction action){var image=Box("Action",parent,Color.white);Place(image.rectTransform,x,y,w,h);image.sprite=Resources.Load<Sprite>("UI/NPC/DialogueChoice");image.type=UnityEngine.UI.Image.Type.Sliced;image.pixelsPerUnitMultiplier=6;var b=image.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=image;b.onClick.AddListener(action);var t=Label(text,image.transform,30,6,w-60,h-12,22,true);t.alignment=TextAlignmentOptions.Center;t.color=new Color(1,.94f,.76f);return b;}
}

public sealed class QuestReturnTravel : MonoBehaviour
{
    private static QuestReturnTravel current;
    public static bool IsTravelling=>current!=null;
    private float previousTime=1;
    public static void Begin(int giver,string scene)
    {
        if(current!=null)return;
        var go=new GameObject("Quest NPC travel");current=go.AddComponent<QuestReturnTravel>();DontDestroyOnLoad(go);current.StartCoroutine(current.Run(giver,scene));
    }
    IEnumerator Run(int giver,string scene)
    {
        previousTime=Time.timeScale;Time.timeScale=0;
        GameSave.EnsureInstance().SaveCurrentPlayer();
        if(QuestReturnPanel.FindNpc(giver)==null)
        {
            if(!Application.CanStreamedLevelBeLoaded(scene)){Debug.LogWarning("Quest NPC map is not available: "+scene);Destroy(gameObject);yield break;}
            yield return SceneManager.LoadSceneAsync(scene);
        }
        yield return null;yield return null;
        var player=FindFirstObjectByType<playerMovement>();var npc=QuestReturnPanel.FindNpc(giver);
        if(player!=null&&npc!=null)
        {
            Vector3 safe;var desired=npc.transform.position+new Vector3(3,1,0);
            if(SafeCheckpoint.TryFind(player,desired,out safe))
            {
                Vector3 delta=safe-player.transform.position;player.transform.position=safe;var body=player.GetComponent<Rigidbody2D>();if(body!=null){body.position=safe;body.linearVelocity=Vector2.zero;}
                Physics2D.SyncTransforms();foreach(var camera in FindObjectsByType<Unity.Cinemachine.CinemachineCamera>(FindObjectsSortMode.None))if(camera.Follow==player.transform)camera.OnTargetObjectWarped(player.transform,delta);
                PlayerDeathFlow.RecordCheckpoint(safe);GameSave.EnsureInstance().SaveCurrentPlayer();
            }
            else Debug.LogWarning("No safe landing near the quest NPC. Player kept at the map's arrival position.");
        }
        Destroy(gameObject);
    }
    void OnDestroy(){if(current==this){current=null;if(!GameControlsGuide.SettingsOpen)Time.timeScale=previousTime;}}
}

