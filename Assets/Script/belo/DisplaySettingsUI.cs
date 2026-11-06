using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>Shared display preferences and settings-panel UI. Inherits settings visibility groups.</summary>
public sealed class DisplaySettingsUI : MonoBehaviour
{
    const string WidthKey="CodeEscapade.Display.Width",HeightKey="CodeEscapade.Display.Height",ModeKey="CodeEscapade.Display.Mode";
    static readonly FullScreenMode[] Modes={FullScreenMode.Windowed,FullScreenMode.FullScreenWindow,FullScreenMode.ExclusiveFullScreen};
    static readonly string[] ModeNames={"Windowed","Windowed Fullscreen","Fullscreen"};
    readonly List<Vector2Int> resolutions=new List<Vector2Int>();
    GameObject page;
    TMP_Text resolutionLabel,modeLabel,note;
    TMP_Text template;
    UnityEngine.UI.Image artwork;
    int resolutionIndex,modeIndex;
    float originalPanelHeight;
    readonly Dictionary<RectTransform,Vector2> actionPositions=new Dictionary<RectTransform,Vector2>();
    readonly Dictionary<RectTransform,Vector2> audioPositions=new Dictionary<RectTransform,Vector2>();

    void LateUpdate()
    {
        if(!gameObject.activeInHierarchy)return;
        SyncStyle();
        LayoutSettings();
    }
    public void LayoutSettings()
    {
        var rect=(RectTransform)transform;
        if(rect.rect.height<=0)return;
        if(gameObject.scene.name=="MainMenu")
        {
            var button=transform.Find("Display Settings Button") as RectTransform;
            var back=StyleSource() as RectTransform;
            if(button!=null&&back!=null)
            {
                float center=back.anchoredPosition.y+(back.anchorMin.y-.5f)*rect.rect.height;
                button.anchoredPosition=new Vector2(0,center+back.rect.height*.5f+60f+button.rect.height*.5f);
            }
            return;
        }
        if(originalPanelHeight<=0)originalPanelHeight=rect.rect.height;
        var parent=rect.parent as RectTransform;
        float available=parent!=null&&parent.rect.height>0?parent.rect.height/Mathf.Max(.01f,Mathf.Abs(rect.localScale.y))-32f:originalPanelHeight+110f;
        float height=Mathf.Max(originalPanelHeight,Mathf.Min(originalPanelHeight+110f,available));
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
        foreach(string name in new[]{"music","SFX"})
        {
            var audio=transform.Find(name) as RectTransform;
            if(audio==null)continue;
            if(!audioPositions.ContainsKey(audio))audioPositions[audio]=audio.anchoredPosition;
            audio.anchoredPosition=audioPositions[audio]+Vector2.up*45f;
        }
        var display=transform.Find("Display Settings Button") as RectTransform;
        if(display==null)return;
        // Raise the action grid into the unused space, leaving Display above the frame.
        foreach(string name in new[]{"Lobby","Save Game Button","Controls Guide Button","Tutorial Tips Button"})
        {
            var action=transform.Find(name) as RectTransform;
            if(action==null||!action.gameObject.activeSelf)continue;
            if(!actionPositions.ContainsKey(action))actionPositions[action]=action.anchoredPosition;
            action.anchoredPosition=actionPositions[action]+Vector2.up*60f;
        }
        float bottom=0f;
        foreach(string name in new[]{"Lobby","Save Game Button","Controls Guide Button","Tutorial Tips Button","Resume"})
        {
            var action=transform.Find(name) as RectTransform;
            if(action==null||!action.gameObject.activeSelf||action.anchorMin!=action.anchorMax)continue;
            float center=action.anchoredPosition.y+(action.anchorMin.y-.5f)*height;
            bottom=Mathf.Min(bottom,center-action.rect.height*.5f);
        }
        float y=bottom-16f-display.rect.height*.5f;
        // Fit the new row within the paper even when the canvas cannot grow further.
        float minimum=-height*.5f+80f+display.rect.height*.5f;
        if(y<minimum)
        {
            float lift=minimum-y;
            foreach(string name in new[]{"Lobby","Save Game Button","Controls Guide Button","Tutorial Tips Button","Resume"})
            {
                var action=transform.Find(name) as RectTransform;
                if(action!=null&&action.gameObject.activeSelf&&action.anchorMin.y<=.5f)action.anchoredPosition+=Vector2.up*lift;
            }
            y=minimum;
        }
        display.anchoredPosition=new Vector2(0,y);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Restore()
    {
        if(PlayerPrefs.HasKey(WidthKey))ApplySaved();
    }
    static void ApplySaved()
    {
        int width=PlayerPrefs.GetInt(WidthKey,1280),height=PlayerPrefs.GetInt(HeightKey,720);
        int mode=Mathf.Clamp(PlayerPrefs.GetInt(ModeKey,0),0,Modes.Length-1);
        if(width<640||height<360)return;
        if(!Application.isEditor)Screen.SetResolution(width,height,Modes[mode]);
    }
    public static void Ensure(GameObject panel)
    {
        if(panel==null||panel.GetComponent<DisplaySettingsUI>()!=null)return;
        panel.AddComponent<DisplaySettingsUI>().Build();
    }
    void Build()
    {
        var source=StyleSource();
        template=source!=null?source.GetComponentInChildren<TMP_Text>(true):GetComponentsInChildren<TMP_Text>(true).FirstOrDefault();
        artwork=source!=null?source.GetComponent<UnityEngine.UI.Image>():GetComponent<UnityEngine.UI.Image>();
        var rect=(RectTransform)transform;
        // Inactive canvases can report negative dimensions before their first layout pass.
        float panelWidth=rect.rect.width>0?rect.rect.width:700f;
        float panelHeight=rect.rect.height>0?rect.rect.height:600f;
        float width=Mathf.Clamp(panelWidth*.6f,260f,380f);
        var launch=Button("Display Settings Button",transform,"DISPLAY",new Vector2(0,-panelHeight*.4f),new Vector2(width,48),Open);
        launch.transform.SetAsLastSibling();
        page=new GameObject("Settings Display",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(CanvasGroup));
        page.transform.SetParent(transform,false);
        var pageRect=(RectTransform)page.transform;pageRect.anchorMin=Vector2.zero;pageRect.anchorMax=Vector2.one;pageRect.offsetMin=pageRect.offsetMax=Vector2.zero;
        var image=page.GetComponent<UnityEngine.UI.Image>();var panelImage=GetComponent<UnityEngine.UI.Image>();
        image.sprite=panelImage!=null?panelImage.sprite:null;image.type=UnityEngine.UI.Image.Type.Sliced;
        image.color=panelImage!=null?panelImage.color:new Color(.88f,.79f,.60f);image.raycastTarget=true;
        Label(page.transform,"DISPLAY SETTINGS",new Vector2(0,190),new Vector2(width+80,44),28);
        resolutionLabel=Button("Resolution",page.transform,"",new Vector2(0,95),new Vector2(width,58),()=>{resolutionIndex=(resolutionIndex+1)%resolutions.Count;Refresh();}).GetComponentInChildren<TMP_Text>();
        modeLabel=Button("Display Mode",page.transform,"",new Vector2(0,15),new Vector2(width,58),()=>{modeIndex=(modeIndex+1)%Modes.Length;Refresh();}).GetComponentInChildren<TMP_Text>();
        note=Label(page.transform,"Click an option to cycle through choices.",new Vector2(0,-65),new Vector2(width+60,64),18);
        Button("Apply Display",page.transform,"APPLY",new Vector2(-width*.26f,-155),new Vector2(width*.48f,48),Apply);
        Button("Back to Settings",page.transform,"BACK",new Vector2(width*.26f,-155),new Vector2(width*.48f,48),()=>page.SetActive(false));
        page.SetActive(false);
        SyncStyle();
    }
    void SyncStyle()
    {
        var source=StyleSource();
        if(source==null)return;
        var sourceImage=source.GetComponent<UnityEngine.UI.Image>();
        var sourceButton=source.GetComponent<UnityEngine.UI.Button>();
        var sourceText=source.GetComponentInChildren<TMP_Text>(true);
        foreach(var name in new[]{"Display Settings Button","Settings Display/Resolution","Settings Display/Display Mode","Settings Display/Apply Display","Settings Display/Back to Settings"})
        {
            var target=transform.Find(name);if(target==null)continue;
            var image=target.GetComponent<UnityEngine.UI.Image>();
            if(image!=null&&sourceImage!=null){image.sprite=sourceImage.sprite;image.type=sourceImage.type;image.color=sourceImage.color;image.material=sourceImage.material;image.pixelsPerUnitMultiplier=sourceImage.pixelsPerUnitMultiplier;}
            var button=target.GetComponent<UnityEngine.UI.Button>();
            if(button!=null&&sourceButton!=null){button.colors=sourceButton.colors;button.transition=sourceButton.transition;button.spriteState=sourceButton.spriteState;}
            var label=target.GetComponentInChildren<TMP_Text>(true);
            if(label!=null&&sourceText!=null){label.font=sourceText.font;label.fontSharedMaterial=sourceText.fontSharedMaterial;label.color=sourceText.color;label.fontStyle=sourceText.fontStyle;}
        }
        var launch=transform.Find("Display Settings Button") as RectTransform;
        var reference=source as RectTransform;
        if(launch!=null&&reference!=null)launch.sizeDelta=new Vector2(Mathf.Clamp(reference.rect.width,200,280),Mathf.Clamp(reference.rect.height,48,64));
        if(gameObject.scene.name=="MainMenu"&&page!=null&&sourceText!=null)
        {
            foreach(var toggle in GetComponentsInChildren<UnityEngine.UI.Toggle>(true))toggle.gameObject.SetActive(false);
            foreach(var label in page.GetComponentsInChildren<TMP_Text>(true))label.color=sourceText.color;
        }
    }
    Transform StyleSource()
    {
        if(gameObject.scene.name=="MainMenu")
        {
            foreach(var button in GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                if(page!=null&&button.transform.IsChildOf(page.transform))continue;
                var label=button.GetComponentInChildren<TMP_Text>(true);
                if(label!=null&&label.text.Trim()=="BACK")return button.transform;
            }
        }
        return transform.Find("Lobby")??transform.Find("Resume");
    }
    public void Open()
    {
        if(page==null)return;
        resolutions.Clear();
        foreach(var r in Screen.resolutions)AddResolution(r.width,r.height);
        int desktopWidth=Screen.currentResolution.width,desktopHeight=Screen.currentResolution.height;
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1600,900),new Vector2Int(1920,1080),new Vector2Int(2560,1440),new Vector2Int(3840,2160)})
            if(size.x<=desktopWidth&&size.y<=desktopHeight)AddResolution(size.x,size.y);
        AddResolution(Screen.width,Screen.height);
        if(resolutions.Count==0)AddResolution(1280,720);
        var saved=new Vector2Int(PlayerPrefs.GetInt(WidthKey,Screen.width),PlayerPrefs.GetInt(HeightKey,Screen.height));
        if(saved.x<=desktopWidth&&saved.y<=desktopHeight)AddResolution(saved.x,saved.y);
        resolutions.Sort((a,b)=>a.x!=b.x?a.x.CompareTo(b.x):a.y.CompareTo(b.y));
        resolutionIndex=Mathf.Max(0,resolutions.IndexOf(saved));
        int current=System.Array.IndexOf(Modes,Screen.fullScreenMode);
        modeIndex=Mathf.Clamp(PlayerPrefs.GetInt(ModeKey,Mathf.Max(0,current)),0,2);
        note.text="Click an option to cycle through choices.";
        Refresh();page.SetActive(true);page.transform.SetAsLastSibling();
    }
    void AddResolution(int width,int height)
    {var size=new Vector2Int(width,height);if(width>=640&&height>=360&&!resolutions.Contains(size))resolutions.Add(size);}
    void Refresh()
    {var size=resolutions[resolutionIndex];resolutionLabel.text="RESOLUTION: "+size.x+" × "+size.y;modeLabel.text=ModeNames[modeIndex];}
    void Apply()
    {
        var size=resolutions[resolutionIndex];
        PlayerPrefs.SetInt(WidthKey,size.x);PlayerPrefs.SetInt(HeightKey,size.y);PlayerPrefs.SetInt(ModeKey,modeIndex);PlayerPrefs.Save();
        ApplySaved();note.text=Application.isEditor?"Saved. Display changes apply in the built game.":"Display settings saved.";
    }
    TMP_Text Label(Transform parent,string text,Vector2 position,Vector2 size,float fontSize)
    {
        var go=new GameObject("Caption",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);Place((RectTransform)go.transform,position,size);
        var label=go.GetComponent<TMP_Text>();label.font=template!=null?template.font:TMP_Settings.defaultFontAsset;
        label.text=text;label.fontSize=fontSize;label.enableAutoSizing=true;label.fontSizeMin=12;label.fontSizeMax=fontSize;label.fontStyle=FontStyles.Bold;label.alignment=TextAlignmentOptions.Center;
        label.color=new Color(.16f,.085f,.035f);label.raycastTarget=false;return label;
    }
    UnityEngine.UI.Button Button(string name,Transform parent,string text,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction action)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));go.transform.SetParent(parent,false);Place((RectTransform)go.transform,position,size);
        var image=go.GetComponent<UnityEngine.UI.Image>();image.sprite=artwork!=null?artwork.sprite:null;image.type=UnityEngine.UI.Image.Type.Sliced;image.color=artwork!=null?artwork.color:new Color(.32f,.18f,.08f);
        var button=go.GetComponent<UnityEngine.UI.Button>();button.targetGraphic=image;button.onClick.AddListener(action);
        var label=Label(go.transform,text,Vector2.zero,size-new Vector2(16,8),22);label.color=new Color(1,.88f,.61f);
        label.enableAutoSizing=true;label.fontSizeMin=14;label.fontSizeMax=22;return button;
    }
    static void Place(RectTransform rect,Vector2 position,Vector2 size)
    {rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;rect.anchoredPosition=position;rect.sizeDelta=size;}
}
