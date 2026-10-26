using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Original programming lessons informed by the quiz-bank curriculum.</summary>
public sealed class CharacterAlmanac : MonoBehaviour
{
    [Serializable] public sealed class Page
    {
        public string id, language, difficulty, title, meaning, purpose, prompt, code_snippet, example_output, walkthrough, practice_code, explanation;
        public string[] choices;
        public int correct_index, min_level, max_level;
        public bool Valid => !string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(meaning) && !string.IsNullOrEmpty(purpose) && !string.IsNullOrEmpty(prompt) && choices != null && correct_index >= 0 && correct_index < choices.Length;
    }
    [Serializable] private sealed class Bank { public Page[] items; }
    
    private List<Page> pages = new List<Page>();
    private GameObject canvasRoot, launcher, modal, listContent;
    private TMP_Text title, status, detail, countLabel, revealCaption;
    private TMP_InputField search;
    private ScrollRect detailScroll;
    private readonly List<Button> tierButtons = new List<Button>();
    private Page selected;
    private string language, query = "", edition = "Original learning edition";
    private int tier;
    private bool opened, revealed;
    private float previousTimeScale;
    private static CharacterAlmanac openJournal;
    public static bool IsOpen => openJournal != null && openJournal.opened;
    public int PageCount => pages.Count(p => p.Valid && p.language == language);
    public string Language => language;
    private static readonly Color Ink = new Color(.19f,.095f,.035f);
    private static readonly Color Gold = new Color(1f,.86f,.54f);

    public static string LanguageForCharacter(int index) => index == SelectedHero.ArcherIndex ? "csharp" : index == SelectedHero.MageIndex ? "python" : "java";
    public static string LanguageName(string value) => value == "csharp" ? "C#" : value == "python" ? "Python" : "Java";
    private static string HeroName(int index) => index == 1 ? "Archer" : index == 2 ? "Mage" : "Warrior";
    void Awake()
    {
        ReloadLessons();
        language = LanguageForCharacter(PlayerPrefs.GetInt("SelectedCharacter",0));
        BuildUI();
        canvasRoot.SetActive(InExploration);
    }
    public void ReloadLessons()
    {
        var snapshot = Resources.Load<TextAsset>("LearningAlmanac/OriginalLessons");
        if (snapshot != null) ReadBank(snapshot.text);
        edition = "Original learning edition";
        selected = null;
        if (opened && detail != null) RefreshList();
    }
    void OnEnable() { if (canvasRoot != null) ReloadLessons(); }
    public static bool IsExplorationScene(string name) => name == "Scene 2" || name == "Scene3" || name == "Scene4";
    private bool InExploration => IsExplorationScene(SceneManager.GetActiveScene().name) && gameObject.scene == SceneManager.GetActiveScene();
    void Update()
    {
        if (!InExploration) { Close(); if (canvasRoot != null) canvasRoot.SetActive(false); return; }
        if (canvasRoot != null && !canvasRoot.activeSelf) canvasRoot.SetActive(true);
        bool settings = GameControlsGuide.SettingsOpen;
        bool dialogue = DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive;
        if (launcher != null) launcher.SetActive(!opened && !settings && !dialogue && Time.timeScale > 0f);
        if (opened)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || (!search.isFocused && Input.GetKeyDown(KeyCode.L))) Close();
        }
        else if (!settings && !dialogue && Time.timeScale > 0f && Input.GetKeyDown(KeyCode.L)) Open();
    }
    void OnDisable() { Close(); }
    void OnDestroy()
    {
        if (canvasRoot == null) return;
        if (Application.isPlaying) Destroy(canvasRoot); else DestroyImmediate(canvasRoot);
    }

    public void Open()
    {
        if (opened || IsOpen || GameControlsGuide.SettingsOpen || !InExploration || Time.timeScale <= 0f) return;
        SelectLanguage(PlayerPrefs.GetInt("SelectedCharacter",0));
        previousTimeScale = Time.timeScale;
        opened = true; openJournal = this;
        Time.timeScale = 0f;
        modal.SetActive(true); launcher.SetActive(false);
        RefreshList();
    }
    public void Close()
    {
        if (!opened) return;
        opened = false;
        if (openJournal == this) openJournal = null;
        Time.timeScale = previousTimeScale;
        if (modal != null) modal.SetActive(false);
    }
    public void SelectLanguage(int index)
    {
        string next = LanguageForCharacter(index);
        if (language != next) { language = next; selected = null; query = ""; tier = 0; if (search != null) search.SetTextWithoutNotify(""); }
        title.text = "THE SCHOLAR'S ALMANAC  /  " + HeroName(index) + " · " + LanguageName(language);
        if (opened) RefreshList();
    }

    void ReadBank(string json)
    {
        var bank = JsonUtility.FromJson<Bank>(json);
        if (bank?.items != null) pages = bank.items.Where(p => p != null && p.Valid && (p.language == "java" || p.language == "csharp" || p.language == "python")).ToList();
    }
    static string TierName(Page p) => p.difficulty == "hard" ? "Advanced" : p.difficulty == "medium" ? "Intermediate" : "Beginner";
    public static string Topic(Page p) => p.title ?? "Programming lesson";
    void RefreshList()
    {
        for (int i=listContent.transform.childCount-1;i>=0;i--)
        {
            var child=listContent.transform.GetChild(i).gameObject;
            child.SetActive(false);
            if(Application.isPlaying) Destroy(child); else DestroyImmediate(child);
        }
        var languagePages = pages.Where(p => p.Valid && p.language == language).OrderBy(p=>p.min_level).ThenBy(p=>p.id,StringComparer.Ordinal).ToList();
        var filtered = languagePages.Where(p => (tier == 0 || p.difficulty == new[]{"","easy","medium","hard"}[tier]) &&
            (string.IsNullOrEmpty(query) || (Topic(p)+" "+p.meaning+" "+p.purpose+" "+p.prompt+" "+p.code_snippet+" "+p.explanation).IndexOf(query,StringComparison.OrdinalIgnoreCase) >= 0))
            .OrderBy(p => p.min_level).ThenBy(p => p.id,StringComparer.Ordinal).ToList();
        for (int i = 0; i < filtered.Count; i++)
        {
            var page = filtered[i];
            var button = MakeButton("Study Page",listContent.transform,Topic(page)+"\n"+TierName(page)+" · Page "+(languagePages.IndexOf(page)+1).ToString("00"),()=>ShowPage(page));
            button.gameObject.AddComponent<LayoutElement>().preferredHeight = 70;
        }
        countLabel.text = filtered.Count + " study pages · " + LanguageName(language);
        if (selected == null || !filtered.Any(p=>p.id==selected.id)) selected = filtered.FirstOrDefault();
        else selected = filtered.First(p=>p.id==selected.id);
        ShowPage(selected);
        for(int i=0;i<tierButtons.Count;i++) tierButtons[i].GetComponent<Image>().color = i==tier ? new Color(1f,.86f,.59f) : Color.white;
        UpdateStatus();
    }
    void ShowPage(Page page)
    {
        selected = page; revealed = false; RenderPage();
        detailScroll.verticalNormalizedPosition = 1f;
    }
    static string Safe(string value) => "<noparse>"+(value??"").Replace("</noparse>","&lt;/noparse&gt;")+"</noparse>";
    void RenderPage()
    {
        if(selected == null) { detail.text = "<b>No matching pages</b>\n\nTry another difficulty or clear your search."; revealCaption.text="No page selected"; return; }
        var p = selected;
        string answer = revealed ? "\n\n<b>PRACTICE ANSWER</b>\n"+Safe(p.choices[p.correct_index])+"\n\n<b>WHY THIS ANSWER IS CORRECT</b>\n"+Safe(p.explanation) : "\n\nWork through the practice example, then use Check practice answer below.";
        detail.text = "<size=28><b>"+Topic(p)+"</b></size>\n"+LanguageName(language)+" · "+TierName(p)+
            "\n\n<b>WHAT IT MEANS</b>\n"+Safe(p.meaning)+
            "\n\n<b>WHEN AND HOW TO USE IT</b>\n"+Safe(p.purpose)+
            "\n\n<b>WORKED EXAMPLE</b>\n"+Safe(p.code_snippet)+
            "\n\n<b>OUTPUT</b>\n"+Safe(p.example_output)+
            "\n\n<b>STEP-BY-STEP WALKTHROUGH</b>\n"+Safe(p.walkthrough)+
            "\n\n<b>TRY IT YOURSELF</b>\n"+Safe(p.prompt)+"\n\n"+Safe(p.practice_code)+answer;
        revealCaption.text = revealed ? "Hide practice answer" : "Check practice answer";
    }
    void UpdateStatus() { if(status!=null) status.text = edition + "  ·  Scroll lists and pages to read  ·  Close [L / Esc]"; }

    void BuildUI()
    {
        canvasRoot = new GameObject("Almanac Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvasRoot.transform.SetParent(transform,false);
        var canvas = canvasRoot.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=250;
        var scaler = canvasRoot.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1280,720); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        launcher = MakeButton("Almanac Button",canvasRoot.transform,"Almanac [L]",Open).gameObject;
        ConfigureLauncher();
        modal = Panel("Almanac Overlay",canvasRoot.transform,new Color(.025f,.015f,.01f,.88f),false); Stretch(modal,0,0,0,0);
        var frame=Panel("Scholar's Almanac",modal.transform,Color.white,false);frame.GetComponent<Image>().sprite=GameControlsGuide.Frame(false);frame.GetComponent<Image>().type=Image.Type.Sliced;Stretch(frame,35,25,35,25);
        title = Label("Almanac Title",frame.transform,"",25,Gold); Position(title.gameObject,new Vector2(28,-18),new Vector2(960,42));
        var close=MakeButton("Close Almanac",frame.transform,"Close [L / Esc]",Close);var cr=(RectTransform)close.transform;cr.anchorMin=cr.anchorMax=Vector2.one;cr.pivot=new Vector2(1,1);cr.anchoredPosition=new Vector2(-24,-18);cr.sizeDelta=new Vector2(175,42);
        var input=Panel("Search",frame.transform,new Color(.91f,.83f,.66f),false);Position(input,new Vector2(24,-76),new Vector2(310,42));
        search=input.AddComponent<TMP_InputField>();var inputText=Label("Search Text",input.transform,"",19,Ink);Stretch(inputText.gameObject,10,5,10,5);inputText.richText=false;search.textViewport=(RectTransform)input.transform;search.textComponent=inputText;
        var placeholder=Label("Search Placeholder",input.transform,"Search topics or examples…",18,new Color(.4f,.3f,.2f));Stretch(placeholder.gameObject,10,5,10,5);search.placeholder=placeholder;
        search.onValueChanged.AddListener(value=>{query=value;RefreshList();});
        string[] tiers={"All pages","Beginner","Intermediate","Advanced"};
        for(int i=0;i<tiers.Length;i++){int index=i;var button=MakeButton("Difficulty "+tiers[i],frame.transform,tiers[i],()=>{tier=index;RefreshList();});Position(button.gameObject,new Vector2(355+i*197,-76),new Vector2(185,42));tierButtons.Add(button);}
        countLabel=Label("Page Count",frame.transform,"",18,Gold);Position(countLabel.gameObject,new Vector2(28,-126),new Vector2(305,30));
        var list=MakeScroll("Study Index",frame.transform,true);
        var listRect=(RectTransform)list.transform;listRect.anchorMin=Vector2.zero;listRect.anchorMax=new Vector2(0,1);listRect.offsetMin=new Vector2(24,48);listRect.offsetMax=new Vector2(334,-164);listContent=list.content.gameObject;
        detailScroll=MakeScroll("Study Page",frame.transform,false);Stretch(detailScroll.gameObject,355,134,24,104);
        detail=detailScroll.content.gameObject.AddComponent<TextMeshProUGUI>();detail.fontSize=23;detail.color=Ink;detail.richText=true;detail.textWrappingMode=TextWrappingModes.Normal;detail.margin=new Vector4(22,18,24,22);detailScroll.content.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var reveal=MakeButton("Reveal Explanation",frame.transform,"Reveal answer & explanation",()=>{if(selected==null)return;revealed=!revealed;RenderPage();});
        var revealRect=(RectTransform)reveal.transform;revealRect.anchorMin=Vector2.zero;revealRect.anchorMax=new Vector2(1,0);revealRect.offsetMin=new Vector2(355,52);revealRect.offsetMax=new Vector2(-24,96);revealCaption=reveal.GetComponentInChildren<TMP_Text>();
        status=Label("Almanac Status",frame.transform,"Offline edition",16,Gold);var sr=(RectTransform)status.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=new Vector2(1,0);sr.pivot=new Vector2(.5f,0);sr.offsetMin=new Vector2(26,12);sr.offsetMax=new Vector2(-26,40);
        modal.SetActive(false);
    }
    void ConfigureLauncher()
    {
        if(launcher==null)return;var rect=(RectTransform)launcher.transform;
        rect.anchorMin=rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,.5f);
        rect.anchoredPosition=new Vector2(-240,-40);rect.sizeDelta=new Vector2(180,48);
    }
    static GameObject Panel(string name,Transform parent,Color color,bool paper)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var image=go.GetComponent<Image>();image.color=color;
        if(paper){image.sprite=GameControlsGuide.Frame(true);image.type=Image.Type.Sliced;}
        return go;
    }
    static TMP_Text Label(string name,Transform parent,string value,int size,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var text=go.GetComponent<TextMeshProUGUI>();text.text=value;text.fontSize=size;text.color=color;text.textWrappingMode=TextWrappingModes.Normal;text.raycastTarget=false;return text;
    }
    static Button MakeButton(string name,Transform parent,string caption,UnityEngine.Events.UnityAction action)
    {
        var go=Panel(name,parent,Color.white,false);var image=go.GetComponent<Image>();image.sprite=GameControlsGuide.Frame(false);image.type=Image.Type.Sliced;
        var button=go.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(action);var colors=button.colors;colors.highlightedColor=new Color(1,.86f,.6f);colors.selectedColor=colors.highlightedColor;button.colors=colors;
        var label=Label("Label",go.transform,caption,20,Gold);label.alignment=TextAlignmentOptions.Center;label.enableAutoSizing=true;label.fontSizeMin=14;label.fontSizeMax=20;Stretch(label.gameObject,10,6,10,6);return button;
    }
    static ScrollRect MakeScroll(string name,Transform parent,bool list)
    {
        var root=new GameObject(name,typeof(RectTransform),typeof(ScrollRect));root.transform.SetParent(parent,false);
        var viewport=Panel("Viewport",root.transform,Color.white,true);Stretch(viewport,0,0,0,0);viewport.AddComponent<Mask>().showMaskGraphic=true;
        var content=new GameObject("Content",typeof(RectTransform),typeof(ContentSizeFitter));content.transform.SetParent(viewport.transform,false);
        var rect=(RectTransform)content.transform;rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,1);rect.offsetMin=rect.offsetMax=Vector2.zero;
        content.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        if(list){var layout=content.AddComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(12,12,12,12);layout.spacing=8;layout.childControlWidth=layout.childControlHeight=true;layout.childForceExpandHeight=false;}
        var scroll=root.GetComponent<ScrollRect>();scroll.viewport=(RectTransform)viewport.transform;scroll.content=rect;scroll.horizontal=false;scroll.scrollSensitivity=32;scroll.movementType=ScrollRect.MovementType.Clamped;return scroll;
    }
    static void Position(GameObject go,Vector2 point,Vector2 size){var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=point;rect.sizeDelta=size;}
    static void Stretch(GameObject go,float left,float top,float right,float bottom){var rect=(RectTransform)go.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(left,bottom);rect.offsetMax=new Vector2(-right,-top);}
}
