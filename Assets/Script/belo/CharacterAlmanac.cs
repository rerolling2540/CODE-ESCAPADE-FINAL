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
    private GameObject canvasRoot, launcher, modal, listContent, library, reader;
    private ScrollRect libraryScroll;
    private Material readableMaterial;
    private TMP_FontAsset readableFont;
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
            if (Input.GetKeyDown(KeyCode.Escape)) { if (reader.activeSelf) BackToBooks(); else Close(); }
            else if (!search.isFocused && Input.GetKeyDown(KeyCode.L)) Close();
        }
        else if (!settings && !dialogue && Time.timeScale > 0f && Input.GetKeyDown(KeyCode.L)) Open();
    }
    void OnDisable() { Close(); }
    void OnDestroy()
    {
        if (canvasRoot != null) { if (Application.isPlaying) Destroy(canvasRoot); else DestroyImmediate(canvasRoot); }
        if (readableMaterial != null) { if (Application.isPlaying) Destroy(readableMaterial); else DestroyImmediate(readableMaterial); }
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
        BackToBooks();
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
            MakeBook(page, languagePages.IndexOf(page) + 1);
        }
        countLabel.text = filtered.Count == 0 ? "No books found. Clear your search or choose another difficulty." : filtered.Count + " topic books · " + LanguageName(language) + "   /   Select a book to read";
        libraryScroll.verticalNormalizedPosition = 1f;
        for(int i=0;i<tierButtons.Count;i++) tierButtons[i].GetComponent<Image>().color = i==tier ? new Color(1f,.86f,.59f) : Color.white;
        UpdateStatus();
    }
    void ShowPage(Page page)
    {
        library.SetActive(false); reader.SetActive(true);
        selected = page; revealed = false; RenderPage();
        detailScroll.verticalNormalizedPosition = 1f;
        UpdateStatus();
    }
    public void BackToBooks()
    {
        reader.SetActive(false); library.SetActive(true);
        UpdateStatus();
    }
    void MakeBook(Page page, int number)
    {
        var button = MakeButton("Book " + page.id,listContent.transform,"",()=>ShowPage(page));
        var blank = button.GetComponentInChildren<TMP_Text>();
        blank.gameObject.SetActive(false);
        // Layered leather cover, binding, and brass bands match the inventory's framed slots.
        Color leather = page.difficulty == "hard" ? new Color(.34f,.16f,.19f) : page.difficulty == "medium" ? new Color(.14f,.28f,.26f) : new Color(.33f,.23f,.12f);
        var cover = Panel("Leather Cover",button.transform,leather,false); Stretch(cover,10,8,10,8); cover.GetComponent<Image>().raycastTarget=false;
        var spine = Panel("Book Binding",cover.transform,new Color(.12f,.07f,.035f),false);Stretch(spine,0,0,147,0);spine.GetComponent<Image>().raycastTarget=false;
        foreach(float y in new[]{-20f,-170f})
        {
            var band=Panel("Brass Binding",cover.transform,new Color(.72f,.51f,.23f),false);Position(band,new Vector2(0,y),new Vector2(16,6));band.GetComponent<Image>().raycastTarget=false;
        }
        var serial=Label("Volume",cover.transform,"VOL. "+number.ToString("00"),14,Gold);Position(serial.gameObject,new Vector2(24,-12),new Vector2(129,22));serial.alignment=TextAlignmentOptions.Center;
        var bookTitle=Label("Topic",cover.transform,Topic(page),22,Gold);Position(bookTitle.gameObject,new Vector2(24,-43),new Vector2(129,102));bookTitle.alignment=TextAlignmentOptions.Center;bookTitle.enableAutoSizing=true;bookTitle.fontSizeMin=16;bookTitle.fontSizeMax=22;
        var level=Label("Difficulty",cover.transform,TierName(page),14,Gold);Position(level.gameObject,new Vector2(24,-153),new Vector2(129,24));level.alignment=TextAlignmentOptions.Center;
        var read=Label("Read",cover.transform,"OPEN BOOK",13,new Color(.94f,.86f,.66f));Position(read.gameObject,new Vector2(24,-180),new Vector2(129,22));read.alignment=TextAlignmentOptions.Center;
    }
    static string Safe(string value) => "<noparse>"+(value??"").Replace("</noparse>","&lt;/noparse&gt;")+"</noparse>";
    static string Section(string caption) => "\n\n<size=22><color=#69421E><b>" + caption + "</b></color></size>\n";
    static string Code(string value) => "<size=20><color=#222D37><mspace=0.6em>" + Safe(value) + "</mspace></color></size>";
    void RenderPage()
    {
        if(selected == null) { detail.text = "<b>No matching pages</b>\n\nTry another difficulty or clear your search."; revealCaption.text="No page selected"; return; }
        var p = selected;
        string answer = revealed ? Section("PRACTICE ANSWER")+Code(p.choices[p.correct_index])+Section("WHY IT WORKS")+Safe(p.explanation) : "\n\n<size=20><i>Trace the practice code, then select Check practice answer below.</i></size>";
        detail.text = "<size=32><b>"+Safe(Topic(p))+"</b></size>\n<size=19><color=#69421E>"+LanguageName(language)+" · "+TierName(p)+"</color></size>"+
            Section("01  THE IDEA")+Safe(p.meaning)+
            Section("02  HOW TO USE IT")+Safe(p.purpose)+
            Section("03  WORKED EXAMPLE")+Code(p.code_snippet)+
            Section("EXPECTED OUTPUT")+Code(p.example_output)+
            Section("FOLLOW THE STEPS")+Safe(p.walkthrough)+
            Section("04  TRY IT YOURSELF")+Safe(p.prompt)+"\n\n"+Code(p.practice_code)+answer;
        revealCaption.text = revealed ? "Hide practice answer" : "Check practice answer";
    }
    void UpdateStatus() { if(status!=null) status.text = edition + (reader != null && reader.activeSelf ? "  ·  Back to books [Esc]  ·  Close [L]" : "  ·  Scroll to browse your books  ·  Close [L / Esc]"); }

    void BuildUI()
    {
        readableFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF") ?? TMP_Settings.defaultFontAsset;
        if (readableFont != null)
        {
            readableMaterial = new Material(readableFont.material) { name = "Almanac Clear Text" };
            readableMaterial.DisableKeyword("UNDERLAY_ON"); readableMaterial.DisableKeyword("UNDERLAY_INNER"); readableMaterial.DisableKeyword("OUTLINE_ON");
            readableMaterial.SetFloat("_OutlineWidth",0f); readableMaterial.SetFloat("_OutlineSoftness",0f); readableMaterial.SetFloat("_FaceDilate",0f);
            readableMaterial.SetColor("_UnderlayColor",Color.clear);
        }
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
        library = new GameObject("Topic Book Collection",typeof(RectTransform));library.transform.SetParent(frame.transform,false);Stretch(library,0,0,0,0);
        reader = new GameObject("Open Book Reader",typeof(RectTransform));reader.transform.SetParent(frame.transform,false);Stretch(reader,0,0,0,0);
        var input=Panel("Search",library.transform,new Color(.91f,.83f,.66f),false);Position(input,new Vector2(24,-76),new Vector2(310,42));
        search=input.AddComponent<TMP_InputField>();var inputText=Label("Search Text",input.transform,"",19,Ink);Stretch(inputText.gameObject,10,5,10,5);inputText.richText=false;search.textViewport=(RectTransform)input.transform;search.textComponent=inputText;
        var placeholder=Label("Search Placeholder",input.transform,"Search topics or examples…",18,new Color(.4f,.3f,.2f));Stretch(placeholder.gameObject,10,5,10,5);search.placeholder=placeholder;
        search.onValueChanged.AddListener(value=>{query=value;RefreshList();});
        string[] tiers={"All books","Beginner","Intermediate","Advanced"};
        for(int i=0;i<tiers.Length;i++){int index=i;var button=MakeButton("Difficulty "+tiers[i],library.transform,tiers[i],()=>{tier=index;RefreshList();});Position(button.gameObject,new Vector2(355+i*197,-76),new Vector2(185,42));tierButtons.Add(button);}
        countLabel=Label("Book Count",library.transform,"",18,Gold);Position(countLabel.gameObject,new Vector2(28,-126),new Vector2(1120,30));
        libraryScroll=MakeScroll("Bookshelf",library.transform,false);Stretch(libraryScroll.gameObject,24,164,24,48);listContent=libraryScroll.content.gameObject;
        var grid=listContent.AddComponent<UnityEngine.UI.GridLayoutGroup>();grid.cellSize=new Vector2(175,226);grid.spacing=new Vector2(12,16);grid.padding=new RectOffset(12,12,14,14);grid.constraint=UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=6;grid.childAlignment=TextAnchor.UpperCenter;
        var back=MakeButton("Back To Books",reader.transform,"< Back to books [Esc]",BackToBooks);Position(back.gameObject,new Vector2(24,-76),new Vector2(260,42));
        var readerHint=Label("Reader Hint",reader.transform,"Read, explore the example, then try the practice below.",18,Gold);Position(readerHint.gameObject,new Vector2(305,-80),new Vector2(820,35));
        detailScroll=MakeScroll("Study Page",reader.transform,false);Stretch(detailScroll.gameObject,24,134,24,104);
        detail=detailScroll.content.gameObject.AddComponent<TextMeshProUGUI>();ApplyReadableFont(detail);detail.fontSize=23;detail.color=Ink;detail.richText=true;detail.textWrappingMode=TextWrappingModes.Normal;detail.lineSpacing=6;detail.paragraphSpacing=8;detail.margin=new Vector4(88,26,88,32);detailScroll.content.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var reveal=MakeButton("Reveal Explanation",reader.transform,"Reveal answer & explanation",()=>{if(selected==null)return;revealed=!revealed;RenderPage();});
        var revealRect=(RectTransform)reveal.transform;revealRect.anchorMin=Vector2.zero;revealRect.anchorMax=new Vector2(1,0);revealRect.offsetMin=new Vector2(24,52);revealRect.offsetMax=new Vector2(-24,96);revealCaption=reveal.GetComponentInChildren<TMP_Text>();
        status=Label("Almanac Status",frame.transform,"Offline edition",16,Gold);var sr=(RectTransform)status.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=new Vector2(1,0);sr.pivot=new Vector2(.5f,0);sr.offsetMin=new Vector2(26,12);sr.offsetMax=new Vector2(-26,40);
        reader.SetActive(false); modal.SetActive(false);
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
    void ApplyReadableFont(TMP_Text text)
    {
        if (readableFont != null) text.font=readableFont;
        if (readableMaterial != null) text.fontSharedMaterial=readableMaterial;
    }
    TMP_Text Label(string name,Transform parent,string value,int size,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var text=go.GetComponent<TextMeshProUGUI>();ApplyReadableFont(text);text.text=value;text.fontSize=size;text.color=color;text.textWrappingMode=TextWrappingModes.Normal;text.raycastTarget=false;return text;
    }
    Button MakeButton(string name,Transform parent,string caption,UnityEngine.Events.UnityAction action)
    {
        var go=Panel(name,parent,Color.white,false);var image=go.GetComponent<Image>();image.sprite=GameControlsGuide.Frame(false);image.type=Image.Type.Sliced;
        var button=go.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(action);var colors=button.colors;colors.highlightedColor=new Color(1,.86f,.6f);colors.selectedColor=colors.highlightedColor;button.colors=colors;
        var label=Label("Label",go.transform,caption,20,Gold);label.alignment=TextAlignmentOptions.Center;label.enableAutoSizing=true;label.fontSizeMin=14;label.fontSizeMax=20;Stretch(label.gameObject,10,6,10,6);return button;
    }
    static ScrollRect MakeScroll(string name,Transform parent,bool list)
    {
        var root=new GameObject(name,typeof(RectTransform),typeof(ScrollRect));root.transform.SetParent(parent,false);
        var viewport=Panel("Viewport",root.transform,Color.white,true);Stretch(viewport,0,0,18,0);viewport.AddComponent<Mask>().showMaskGraphic=true;
        var content=new GameObject("Content",typeof(RectTransform),typeof(ContentSizeFitter));content.transform.SetParent(viewport.transform,false);
        var rect=(RectTransform)content.transform;rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,1);rect.offsetMin=rect.offsetMax=Vector2.zero;
        content.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        if(list){var layout=content.AddComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(12,12,12,12);layout.spacing=8;layout.childControlWidth=layout.childControlHeight=true;layout.childForceExpandHeight=false;}
        var scroll=root.GetComponent<ScrollRect>();scroll.viewport=(RectTransform)viewport.transform;scroll.content=rect;scroll.horizontal=false;scroll.scrollSensitivity=32;scroll.movementType=ScrollRect.MovementType.Clamped;
        var track=Panel("Scroll Track",root.transform,new Color(.17f,.10f,.05f),false);var trackRect=(RectTransform)track.transform;trackRect.anchorMin=new Vector2(1,0);trackRect.anchorMax=Vector2.one;trackRect.offsetMin=new Vector2(-14,0);trackRect.offsetMax=Vector2.zero;
        var handle=Panel("Scroll Handle",track.transform,new Color(.72f,.51f,.23f),false);Stretch(handle,2,2,2,2);
        var bar=track.AddComponent<UnityEngine.UI.Scrollbar>();bar.direction=UnityEngine.UI.Scrollbar.Direction.BottomToTop;bar.handleRect=(RectTransform)handle.transform;bar.targetGraphic=handle.GetComponent<Image>();scroll.verticalScrollbar=bar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
        return scroll;
    }
    static void Position(GameObject go,Vector2 point,Vector2 size){var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=point;rect.sizeDelta=size;}
    static void Stretch(GameObject go,float left,float top,float right,float bottom){var rect=(RectTransform)go.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(left,bottom);rect.offsetMax=new Vector2(-right,-top);}
}
