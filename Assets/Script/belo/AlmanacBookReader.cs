using System;
using System.Linq;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>The almanac's selected volume, using the same hinged sheets as NPC workshops.</summary>
public sealed class AlmanacBookReader : MonoBehaviour
{
    public bool Visible=>outer!=null&&outer.activeSelf;
    private GameObject outer,book,left,right;
    private CanvasGroup controls;
    private WorkshopBookTurn turn;
    private TMP_Text leftTitle,rightTitle,leftText,rightText,number,feedback;
    private ScrollRect leftScroll,rightScroll;
    private Button previous,next,start;
    private readonly Button[] answers=new Button[4];
    private CharacterAlmanac.Page lesson;
    private Action returnToShelf;
    private int spread;
    private string attempt;
    private bool answering,resolved,correct,coverMoving;
    private Coroutine coverRoutine;
    private TMP_FontAsset font;
    private Material material;
    private static Sprite paper,banner;
    private bool CanUse=>Visible&&!coverMoving&&!turn.Busy&&!GameControlsGuide.SettingsOpen;
    public void Show(CharacterAlmanac.Page page,Action back)
    {
        HideImmediate();lesson=page;returnToShelf=back;spread=0;resolved=false;answering=false;
        if(outer==null)Build();outer.SetActive(true);Render();
        if(Application.isPlaying)coverRoutine=StartCoroutine(Cover(true,null));
    }
    public void Close(Action done)
    {
        if(!Visible){done?.Invoke();return;}
        if(coverMoving)return;
        EndAttempt();turn.Cancel();
        if(!Application.isPlaying){HideImmediate();done?.Invoke();return;}
        coverRoutine=StartCoroutine(Cover(false,done));
    }
    public void HideImmediate()
    {
        if(coverRoutine!=null)StopCoroutine(coverRoutine);coverRoutine=null;coverMoving=false;
        EndAttempt();if(turn!=null)turn.Cancel();if(outer!=null){outer.SetActive(false);outer.transform.localScale=Vector3.one;}
    }
    void EndAttempt(){if(!string.IsNullOrEmpty(attempt))QuestionPerformanceTracker.Abandon(attempt);attempt=null;answering=false;}
    void OnDisable()=>HideImmediate();
    void OnDestroy(){HideImmediate();if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}}
    void Update(){if(!CanUse)return;if(Input.GetKeyDown(KeyCode.RightArrow))Change(1);else if(Input.GetKeyDown(KeyCode.LeftArrow))Change(-1);}
    IEnumerator Cover(bool opening,Action done)
    {
        coverMoving=true;controls.interactable=false;float elapsed=0;
        while(elapsed<.45f){if(!GameControlsGuide.SettingsOpen)elapsed+=Time.unscaledDeltaTime;float p=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.45f));outer.transform.localScale=new Vector3(Mathf.Lerp(.009f,1,opening?p:1-p),1,1);yield return null;}
        coverRoutine=null;coverMoving=false;controls.interactable=true;
        if(opening)outer.transform.localScale=Vector3.one;else{HideImmediate();done?.Invoke();}
    }
    public void Change(int delta)
    {
        if(!CanUse)return;int destination=Mathf.Clamp(spread+delta,0,3);if(destination==spread||destination==3&&!resolved)return;
        if(spread==2)EndAttempt();
        turn.Begin(left.transform,right.transform,controls,delta>0,()=>{spread=destination;Render();});
    }
    public void StartPractice()
    {
        if(!CanUse||spread!=2||answering)return;
        attempt=QuestionPerformanceTracker.Begin(lesson.id+"-almanac",QuestionCurriculum.Topic(lesson.topic,lesson.id),lesson.language,lesson.difficulty,QuestionPerformanceTracker.GetCurrentPlayerLevel(),"almanac");
        answering=true;resolved=false;Render();
    }
    public void Answer(int index)
    {
        if(!CanUse||spread!=2||!answering||index<0||index>=lesson.choices.Length)return;
        correct=index==lesson.correct_index;QuestionPerformanceTracker.Complete(attempt,correct);attempt=null;answering=false;resolved=true;
        turn.Begin(left.transform,right.transform,controls,true,()=>{spread=3;Render();});
    }
    static string Safe(string value)=>"<noparse>"+(value??"").Replace("</noparse>","")+"</noparse>";
    static string Section(string title,string body)=>"<b>"+title+"</b>\n\n"+Safe(body);
    void Render()
    {
        string[] phases={"MEANING & USE","WORKED EXAMPLE","PRACTICE","REVIEW"};
        number.text=(spread+1)+" / 4  ·  "+phases[spread];
        if(spread==0){leftTitle.text=lesson.title;rightTitle.text="HOW TO USE IT";leftText.text=Section(CharacterAlmanac.LanguageName(lesson.language)+" / "+(lesson.difficulty=="hard"?"Advanced":lesson.difficulty=="medium"?"Intermediate":"Beginner"),lesson.meaning);rightText.text=Safe(lesson.purpose)+"\n\n<i>Turn the page to see a worked example.</i>";}
        if(spread==1){leftTitle.text="READ THE EXAMPLE";rightTitle.text="FOLLOW THE STEPS";leftText.text=Section("CODE",lesson.code_snippet)+"\n\n"+Section("MESSAGES / RESULT",lesson.example_output);rightText.text=Safe(lesson.walkthrough);}
        if(spread==2){leftTitle.text="TRY IT YOURSELF";rightTitle.text="CHOOSE AN ANSWER";leftText.text=Safe(lesson.prompt)+"\n\n"+Section("YOUR NEW EXAMPLE",lesson.practice_code);rightText.text="Read the code from top to bottom.\n\nPress Start practice when ready. Then choose one answer. There is no penalty for turning back to read. An unfinished attempt is recorded as abandoned.";}
        if(spread==3){leftTitle.text=correct?"CORRECT":"LET'S REVIEW";rightTitle.text="WHY IT WORKS";leftText.text=Section("CORRECT ANSWER",lesson.choices[lesson.correct_index]);rightText.text=Safe(lesson.explanation);}
        leftTitle.color=spread==3&&correct?new Color(.08f,.42f,.16f):Ink;
        previous.interactable=spread>0;next.interactable=spread<2||spread==2&&resolved;
        start.gameObject.SetActive(spread==2&&!answering);start.GetComponentInChildren<TMP_Text>().text=resolved?"Try practice again":"Start practice";
        for(int i=0;i<4;i++){bool show=spread==2&&answering&&i<lesson.choices.Length;answers[i].gameObject.SetActive(show);if(show)answers[i].GetComponentInChildren<TMP_Text>().text=((char)('A'+i))+". "+lesson.choices[i];}
        rightScroll.gameObject.SetActive(spread!=2||!answering);
        if(spread==2&&!answering){rightScroll.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,240);}else rightScroll.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,390);
        feedback.text=spread==2?(answering?"Choose once. Turn back to reread if you need.":"Practice starts only when you press Start practice."):"Scroll inside a page for longer passages.  ·  Left / Right: turn pages";
        leftScroll.verticalNormalizedPosition=rightScroll.verticalNormalizedPosition=1;
        Canvas.ForceUpdateCanvases();
    }
    private static readonly Color Ink=new Color(.19f,.12f,.07f);
    static Sprite Art(string path,ref Sprite cached,bool sliced)
    {
        if(cached!=null)return cached;var texture=Resources.Load<Texture2D>(path);if(texture==null)return GameControlsGuide.Frame(true);
        texture.filterMode=FilterMode.Point;cached=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,sliced?new Vector4(4,4,4,4):Vector4.zero);return cached;
    }
    void Build()
    {
        // Hide the previous scroll reader's controls, preserving its bookshelf and progress panels.
        foreach(Transform child in transform)child.gameObject.SetActive(false);
        font=Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF")??TMP_Settings.defaultFontAsset;
        if(font!=null){material=new Material(font.material);material.DisableKeyword("UNDERLAY_ON");material.DisableKeyword("UNDERLAY_INNER");material.SetFloat("_OutlineWidth",0);material.SetFloat("_FaceDilate",0);}
        outer=new GameObject("Almanac volume",typeof(RectTransform));outer.transform.SetParent(transform,false);Centre(outer,1160,600,0,-25);
        var cover=Panel("Leather binding",outer.transform,new Color(.24f,.13f,.065f),false);Centre(cover,1160,548,0,0);
        book=new GameObject("Almanac open book",typeof(RectTransform),typeof(CanvasGroup),typeof(WorkshopBookTurn));book.transform.SetParent(outer.transform,false);Centre(book,1120,528,0,0);controls=book.GetComponent<CanvasGroup>();turn=book.GetComponent<WorkshopBookTurn>();
        left=Panel("Left sheet",book.transform,Color.white,true);Place(left,10,10,550,508);right=Panel("Right sheet",book.transform,new Color(.98f,.96f,.91f),true);Place(right,560,10,550,508);
        var spine=Panel("Spine shadow",book.transform,new Color(.22f,.13f,.07f,.35f),false);Place(spine,554,10,12,508);spine.GetComponent<Image>().raycastTarget=false;
        leftTitle=Heading(left.transform);rightTitle=Heading(right.transform);
        leftScroll=Scroll(left.transform,out leftText);rightScroll=Scroll(right.transform,out rightText);
        start=Button("Start practice",right.transform,StartPractice,42,394,466,48);
        for(int i=0;i<4;i++){int index=i;answers[i]=Button("Answer",right.transform,()=>Answer(index),42,110+i*90,466,80);}
        previous=Button("< Previous page",outer.transform,()=>Change(-1),0,562,210,40);
        next=Button("Next page >",outer.transform,()=>Change(1),950,562,210,40);
        Button("Back to bookshelf",outer.transform,()=>{if(CanUse)Close(returnToShelf);},0,0,220,38);
        number=Text("Page number",outer.transform,"",17);Place(number.gameObject,420,564,320,32);number.color=new Color(.96f,.86f,.66f);number.alignment=TextAlignmentOptions.Center;
        feedback=Text("Reading hint",outer.transform,"",15);Place(feedback.gameObject,240,0,900,32);feedback.color=new Color(.96f,.86f,.66f);
    }
    TMP_Text Heading(Transform parent)
    {
        var ribbon=Panel("Ribbon",parent,Color.white,false);ribbon.GetComponent<Image>().sprite=Art("UI/WorkshopArt/Banner",ref banner,false);ribbon.GetComponent<Image>().raycastTarget=false;Place(ribbon,30,18,490,72);
        var title=Text("Page heading",parent,"",25);title.fontStyle=FontStyles.Bold;title.enableAutoSizing=true;title.fontSizeMin=17;title.fontSizeMax=25;title.alignment=TextAlignmentOptions.Center;Place(title.gameObject,58,34,434,42);return title;
    }
    ScrollRect Scroll(Transform parent,out TMP_Text text)
    {
        var root=new GameObject("Page reading area",typeof(RectTransform),typeof(ScrollRect));root.transform.SetParent(parent,false);Place(root,42,100,466,390);
        var viewport=Panel("Viewport",root.transform,new Color(1,1,1,.001f),false);Fill(viewport,0);viewport.AddComponent<RectMask2D>();
        text=Text("Page text",viewport.transform,"",22);text.richText=true;text.lineSpacing=4;text.paragraphSpacing=6;
        var rect=text.rectTransform;rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,1);rect.offsetMin=rect.offsetMax=Vector2.zero;
        text.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var scroll=root.GetComponent<ScrollRect>();scroll.viewport=(RectTransform)viewport.transform;scroll.content=rect;scroll.horizontal=false;scroll.scrollSensitivity=30;scroll.movementType=ScrollRect.MovementType.Clamped;return scroll;
    }
    GameObject Panel(string name,Transform parent,Color color,bool sheet){var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var image=go.GetComponent<Image>();image.color=color;if(sheet){image.sprite=Art("UI/WorkshopPaper",ref paper,true);image.type=Image.Type.Sliced;}return go;}
    TMP_Text Text(string name,Transform parent,string value,int size){var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var text=go.GetComponent<TMP_Text>();if(font!=null)text.font=font;if(material!=null)text.fontSharedMaterial=material;text.text=value;text.fontSize=size;text.color=Ink;text.textWrappingMode=TextWrappingModes.Normal;text.raycastTarget=false;return text;}
    Button Button(string name,Transform parent,UnityEngine.Events.UnityAction action,float x,float y,float w,float h){var go=Panel(name,parent,new Color(.91f,.83f,.67f),true);Place(go,x,y,w,h);var b=go.AddComponent<Button>();b.targetGraphic=go.GetComponent<Image>();b.onClick.AddListener(action);var label=Text("Label",go.transform,name,20);label.richText=false;label.enableAutoSizing=true;label.fontSizeMin=14;label.fontSizeMax=20;label.alignment=TextAlignmentOptions.Center;Fill(label.gameObject,8);return b;}
    static void Centre(GameObject go,float w,float h,float x,float y){var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(w,h);r.anchoredPosition=new Vector2(x,y);}
    static void Place(GameObject go,float x,float y,float w,float h){var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
    static void Fill(GameObject go,float inset){var r=(RectTransform)go.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.one*inset;r.offsetMax=-Vector2.one*inset;}
}
