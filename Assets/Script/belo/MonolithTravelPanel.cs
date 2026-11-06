using TMPro;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Free return travel, plus the elf's unlocked first journey to the final grove.</summary>
public sealed class MonolithTravelPanel : MonoBehaviour
{
    private static MonolithTravelPanel current;
    public static bool IsOpen=>current!=null;
    private float previousTime=1;
    private CanvasGroup group;
    private TMP_Text notice;
    private bool restore;
    static bool IsMapDestination(WorldTravelCatalog.Destination d)=>d!=null&&!d.lobby&&!string.IsNullOrWhiteSpace(d.sceneName)&&d.sceneName!="MainMenu";
    public static bool IsDiscovered(WorldTravelCatalog.Destination d)=>IsMapDestination(d)&&WorldTravelDiscovery.HasVisited(SaveSlots.Read(SaveSlots.Active),d.sceneName);
    public static bool CanTravel(WorldTravelCatalog.Destination d)=>IsMapDestination(d)&&Application.CanStreamedLevelBeLoaded(d.sceneName)&&WorldTravelDiscovery.CanTravelTo(SaveSlots.Read(SaveSlots.Active),d.sceneName);
    static string LockedMessage(WorldTravelCatalog.Destination d)=>d?.sceneName=="Forest of Dawn"?"Locked — claim the elf's Frostbound Passage chapter reward":"Undiscovered — visit this map first";
    public static void Open(MonolithTravel owner)
    {
        if(IsOpen||owner==null||Time.timeScale<=0||GameControlsGuide.GameplayOverlayHidden)return;
        GameSave.EnsureInstance().SaveCurrentPlayer();
        WorldTravelDiscovery.RecordCurrentScene();
        var go=new GameObject("Monolith Travel Panel",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster),typeof(CanvasGroup));
        var panel=go.AddComponent<MonolithTravelPanel>();current=panel;panel.previousTime=Time.timeScale;panel.restore=true;Time.timeScale=0;panel.Build(WorldTravelCatalog.Load(),SceneManager.GetActiveScene().name);
        GameControlsGuide.RegisterMapBlocker(go);SettingsUiVisibility.EnsureInstance();SettingsUiVisibility.RefreshNow();
    }
    void Update()
    {
        if(group!=null){bool visible=!GameControlsGuide.SettingsOpen;group.alpha=visible?1:0;group.interactable=group.blocksRaycasts=visible;}
        if(!GameControlsGuide.SettingsOpen){Time.timeScale=0;if(Input.GetKeyDown(KeyCode.Escape))Close();}
    }
    public void Travel(WorldTravelCatalog.Destination d)
    {
        if(GameControlsGuide.SettingsOpen)return;
        if(!CanTravel(d)){notice.text=WorldTravelDiscovery.CanTravelTo(SaveSlots.Read(SaveSlots.Active),d?.sceneName)?"This destination is unavailable. Choose another location.":LockedMessage(d);return;}
        if(d.sceneName==SceneManager.GetActiveScene().name)return;
        GameSave.EnsureInstance().SaveCurrentPlayer();
        PortalArrivalPoint.Prepare(d.sceneName,WorldTravelCatalog.ArrivalId);
        Time.timeScale=previousTime;restore=false;current=null;Destroy(gameObject);
        SceneTransitionFader.Load(d.sceneName);
    }
    public void Close(){if(current==this)current=null;if(restore&&!GameControlsGuide.SettingsOpen)Time.timeScale=previousTime;restore=false;Destroy(gameObject);}
    void OnDestroy(){if(current==this)current=null;if(restore&&!GameControlsGuide.SettingsOpen)Time.timeScale=previousTime;}
    public void Build(WorldTravelCatalog catalog,string currentScene)
    {
        var destinations=catalog?.destinations?.Where(IsMapDestination).Take(5).ToArray()??System.Array.Empty<WorldTravelCatalog.Destination>();
        float closeY=160+destinations.Length*84;
        var canvas=GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1150;group=GetComponent<CanvasGroup>();
        var scaler=GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
        var shade=Image("Backdrop",transform,new Color(0,0,0,.7f));Stretch(shade.rectTransform);
        var frame=Image("Monolith travel",shade.transform,new Color(.24f,.14f,.065f));var rt=frame.rectTransform;rt.anchorMin=rt.anchorMax=rt.pivot=Vector2.one*.5f;rt.sizeDelta=new Vector2(820,closeY+80);
        var paper=Image("Parchment",frame.transform,new Color(.91f,.83f,.65f));Stretch(paper.rectTransform,8);
        Text("MONOLITH TRAVEL",paper.transform,28,20,748,44,32,true);
        notice=Text("Return to visited maps for free. The elf unlocks Forest of Dawn\nafter you claim the Frostbound Passage chapter reward.",paper.transform,28,75,748,64,23,false);
        if(destinations.Length>0)
        {
            for(int i=0;i<destinations.Length;i++)
            {
                var destination=destinations[i];bool here=destination.sceneName==currentScene;bool available=CanTravel(destination);
                string label=destination.title+"\n"+(here?"You are here":available?destination.description:WorldTravelDiscovery.CanTravelTo(SaveSlots.Read(SaveSlots.Active),destination.sceneName)?"Destination unavailable":LockedMessage(destination));
                var b=Button(label,paper.transform,28,150+i*84,748,76,()=>Travel(destination));b.interactable=!here&&available;
            }
        }
        else notice.text="No destinations are configured. Close this panel and try another monolith.";
        Button("Close  [Esc]",paper.transform,242,closeY,320,52,Close);
    }
    static UnityEngine.UI.Image Image(string name,Transform parent,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(parent,false);var i=go.GetComponent<UnityEngine.UI.Image>();i.color=color;return i;}
    static void Stretch(RectTransform r,float inset=0){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.one*inset;r.offsetMax=-Vector2.one*inset;}
    static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
    static TMP_Text Text(string value,Transform parent,float x,float y,float w,float h,float size,bool bold){var go=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var t=go.GetComponent<TextMeshProUGUI>();Place(t.rectTransform,x,y,w,h);t.font=Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");t.fontSize=size;t.fontStyle=bold?FontStyles.Bold:FontStyles.Normal;t.text=value;t.color=new Color(.18f,.10f,.045f);t.raycastTarget=false;return t;}
    static UnityEngine.UI.Button Button(string value,Transform parent,float x,float y,float w,float h,UnityEngine.Events.UnityAction action){var image=Image("Destination",parent,Color.white);Place(image.rectTransform,x,y,w,h);image.sprite=Resources.Load<Sprite>("UI/NPC/DialogueChoice");image.type=UnityEngine.UI.Image.Type.Sliced;image.pixelsPerUnitMultiplier=6;var b=image.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=image;b.onClick.AddListener(action);var label=Text(value,image.transform,30,6,w-60,h-12,23,true);label.alignment=TextAlignmentOptions.Center;label.color=new Color(1,.94f,.76f);return b;}
}
