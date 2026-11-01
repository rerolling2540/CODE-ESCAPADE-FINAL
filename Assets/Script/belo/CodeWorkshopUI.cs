using System;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>A chapter-based learning book sharing the existing quest and question history.</summary>
public sealed class CodeWorkshopUI : MonoBehaviour
{
    private static CodeWorkshopUI active;
    public static bool IsOpen=>active!=null&&active.opened;
    private GameObject root,book,leftPage,rightPage,cellsRoot,correctMark;
    private readonly GameObject[] leftSections=new GameObject[5],rightSections=new GameObject[5];
    private readonly UnityEngine.UI.Button[] topics=new UnityEngine.UI.Button[2],levels=new UnityEngine.UI.Button[3],answers=new UnityEngine.UI.Button[3];
    private readonly UnityEngine.UI.Image[] levelIcons=new UnityEngine.UI.Image[3];
    private readonly UnityEngine.UI.Image[] topicIcons=new UnityEngine.UI.Image[2];
    private UnityEngine.UI.Image chapterIcon;
    private TMP_Text stateLegend;
    private static string TopicArt(int index)=>new[]{"Scroll","Coins","Gate","Step","Shelf","Heal"}[index];
    private static readonly System.Collections.Generic.Dictionary<string,Sprite> artwork=new System.Collections.Generic.Dictionary<string,Sprite>();
    private UnityEngine.UI.Button previousPage,nextPage,previousStep,nextStep,replay,practice;
    private TMP_Text header,pageNumber,overview,chapter,definition,context,example,code,trace,console,challenge,instructions,resultTitle,resultAnswer,resultExplanation,stepLabel;
    private WorkshopBookTurn turn;
    private CanvasGroup bookInput;
    private CodeMentorNPC mentor;
    private VisualCodeLessons.Lesson lesson;
    private TMP_FontAsset font;
    private Material textMaterial;
    private string language,attempt,user;
    private int topic,exerciseTier,step,page,slot,hero,selectedAnswer=-1;
    private bool opened,answered,correct;
    private bool coverMoving;
    private Coroutine coverRoutine;
    private GameObject bookCover;
    private CanvasGroup[] bookChrome;
    private float previousTimeScale;
    private static Sprite paper;
    private static readonly Color Ink=new Color(.19f,.12f,.07f),Blue=new Color(.72f,.84f,.88f);
    private bool CanUse=>opened&&!coverMoving&&!turn.Busy&&!GameControlsGuide.SettingsOpen;
    private Sprite Paper
    {
        get
        {
            if(paper!=null)return paper;
            var texture=Resources.Load<Texture2D>("UI/WorkshopPaper");
            if(texture==null)return GameControlsGuide.Frame(true);
            texture.filterMode=FilterMode.Point;
            paper=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(4,4,4,4));return paper;
        }
    }
    public void Open(CodeMentorNPC teacher)
    {
        if(opened||IsOpen||GameControlsGuide.SettingsOpen||Time.timeScale<=0||!CharacterAlmanac.IsExplorationScene(gameObject.scene.name))return;
        mentor=teacher;hero=PlayerPrefs.GetInt("SelectedCharacter",0);slot=SaveSlots.Active;user=MainMenuSaveSlots.CurrentUserId;
        language=CharacterAlmanac.LanguageForCharacter(hero);topic=teacher.SuggestedTopic(language);exerciseTier=0;step=0;page=0;answered=false;
        lesson=WorkshopVariations.Get(language,topic,exerciseTier);
        if(root==null)Build();
        opened=true;active=this;previousTimeScale=Time.timeScale;Time.timeScale=0;root.SetActive(true);Refresh();
        SettingsUiVisibility.EnsureInstance();SettingsUiVisibility.RefreshNow();
        coverRoutine=StartCoroutine(AnimateCover(true));
    }
    public void Close()
    {
        if(!opened)return;
        if(!Application.isPlaying){CloseNow();return;}
        if(coverMoving)return;
        EndAttempt();if(turn!=null)turn.Cancel();
        coverRoutine=StartCoroutine(AnimateCover(false));
    }
    System.Collections.IEnumerator AnimateCover(bool opening)
    {
        coverMoving=true;bookInput.interactable=false;
        SampleOpenClose(opening?0:1);
        float elapsed=0;
        while(elapsed<.55f)
        {
            if(!GameControlsGuide.SettingsOpen)elapsed+=Time.unscaledDeltaTime;
            float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.55f));
            SampleOpenClose(opening?t:1-t);
            yield return null;
        }
        SampleOpenClose(opening?1:0);coverMoving=false;coverRoutine=null;bookInput.interactable=true;
        if(!opening)CloseNow();
    }
    // The entire spread closes symmetrically into a narrow centre spine, as in the reference.
    public void SampleOpenClose(float openness)
    {
        openness=Mathf.Clamp01(openness);
        var scale=new Vector3(Mathf.Lerp(.009f,1,openness),1,1);
        if(book!=null)book.transform.localScale=scale;
        if(bookCover!=null)bookCover.transform.localScale=scale;
        if(bookChrome!=null)foreach(var group in bookChrome)if(group!=null)group.alpha=openness;
    }
    void CloseNow()
    {
        if(coverRoutine!=null)StopCoroutine(coverRoutine);coverRoutine=null;coverMoving=false;
        EndAttempt();if(turn!=null)turn.Cancel();if(!opened)return;
        opened=false;if(active==this)active=null;if(root!=null)root.SetActive(false);SampleOpenClose(1);
        if(!GameControlsGuide.SettingsOpen)Time.timeScale=previousTimeScale;
    }
    void Update()
    {
        if(!opened)return;
        if(slot!=SaveSlots.Active||hero!=PlayerPrefs.GetInt("SelectedCharacter",0)||user!=MainMenuSaveSlots.CurrentUserId||!CharacterAlmanac.IsExplorationScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)){CloseNow();return;}
        if(GameControlsGuide.SettingsOpen)return;Time.timeScale=0;
        if(Input.GetKeyDown(KeyCode.Escape))Close();
        else if(CanUse&&Input.GetKeyDown(KeyCode.RightArrow))NextPage();
        else if(CanUse&&Input.GetKeyDown(KeyCode.LeftArrow))PreviousPage();
    }
    void OnDisable()=>CloseNow();
    void OnDestroy(){CloseNow();if(textMaterial!=null){if(Application.isPlaying)Destroy(textMaterial);else DestroyImmediate(textMaterial);}}
    void EndAttempt(){if(!string.IsNullOrEmpty(attempt))QuestionPerformanceTracker.Abandon(attempt);attempt=null;}
    void Transition(int destination,bool forward,Action beforeRefresh=null)
    {
        if(!CanUse)return;
        // Leaving practice finishes its pending attempt once, even if the book closes mid-turn.
        if(page==3)EndAttempt();
        turn.Begin(leftPage.transform,rightPage.transform,bookInput,forward,()=>{beforeRefresh?.Invoke();page=destination;Refresh();},()=>{
            if(destination==3&&opened)attempt=QuestionPerformanceTracker.Begin(WorkshopProgression.Id(language,topic,exerciseTier),lesson.topic,language,QuestionCurriculum.Difficulty(exerciseTier+1),QuestionPerformanceTracker.GetCurrentPlayerLevel(),"almanac");
        });
    }
    public void SelectTopic(int index)
    {
        if(!CanUse||page!=0)return;
        index=Mathf.Clamp(index,mentor.workshop*2,mentor.workshop*2+1);
        if(topic!=index){topic=index;exerciseTier=0;step=0;answered=false;lesson=WorkshopVariations.Get(language,topic,0);}
        Refresh();
    }
    public void SelectExerciseTier(int value)
    {
        if(!CanUse||page!=0||value<0||value>2||!WorkshopProgression.Available(language,topic,value))return;
        if(exerciseTier!=value){exerciseTier=value;step=0;answered=false;lesson=WorkshopVariations.Get(language,topic,value);}
        Refresh();
    }
    public void Contents(){if(CanUse&&page!=0)Transition(0,false);}
    public void PreviousPage(){if(CanUse&&page>0)Transition(page==4?2:page-1,false);}
    public void NextPage()
    {
        if(!CanUse)return;
        if(page==0)Transition(1,true);
        else if(page==1)Transition(2,true);
        else if(page==2)BeginPrediction();
        else if(page==4)Contents();
    }
    public void Move(int delta){if(!CanUse||page!=2)return;step=Mathf.Clamp(step+delta,0,lesson.steps.Length-1);Refresh();}
    public void Replay(){if(!CanUse)return;if(page!=2)Transition(2,false,()=>step=0);else{step=0;Refresh();}}
    public void BeginPrediction()
    {
        if(!CanUse||page!=2||step!=lesson.steps.Length-1||!WorkshopProgression.Available(language,topic,exerciseTier))return;
        // The attempt starts only after the page turn. Closing during the turn creates no phantom attempt.
        Transition(3,true,()=>{answered=false;selectedAnswer=-1;});
    }
    public void Submit(int index)
    {
        if(!CanUse||page!=3||answered||index<0||index>=3)return;
        answered=true;selectedAnswer=index;correct=index==lesson.correct;
        QuestionPerformanceTracker.Complete(attempt,correct);attempt=null;
        if(correct){WorkshopProgression.MarkPassed(language,topic,exerciseTier);if(exerciseTier==0)LearningQuests.RecordCorrect(language,lesson.topic);}
        Transition(4,true);
    }
    void Refresh()
    {
        for(int i=0;i<5;i++){leftSections[i].SetActive(i==page);rightSections[i].SetActive(i==page);}
        header.text=mentor.Role.ToUpperInvariant()+"  /  "+CharacterAlmanac.LanguageName(language);
        pageNumber.text=page==0?"CONTENTS":new[]{"","01 / UNDERSTAND","02 / WATCH","03 / TRY","04 / REVIEW"}[page];
        overview.text="THE LEARNING BOOK\n\n"+CharacterAlmanac.LanguageName(language)+"\n\nSELECTED\n"+lesson.title.Split('·')[1].Trim()+" / "+WorkshopProgression.TierName(exerciseTier)+"\n\nChoose your topic and level opposite, then press Next page when ready.\n\nRead, watch the code, then try a question. Step buttons advance instructions; page arrows turn the book.";
        chapter.text=lesson.title+"\n"+WorkshopProgression.TierName(exerciseTier);
        chapterIcon.sprite=Art(TopicArt(topic));
        stateLegend.text=new[]{"Paper: the messages shown so far","Coins: the value stored in the box","Key: what we have  /  Gate: the decision","Arrow: a repeat to do  /  Check: a repeat done","Shelf cells: numbered places for values","Paper: input  /  Cross: health value"}[topic];
        definition.text=lesson.meaning;
        context.text=language=="python"?"Python 3: run as a script. Indentation groups instructions.":language=="csharp"?"C#: run statements inside static Main, with using System;. Put method definitions in the class.":"Java: run statements inside public static void main(String[] args). Put method definitions in the class.";
        example.text=lesson.code;example.richText=false;
        var state=lesson.steps[step];string[] lines=lesson.code.Split('\n');
        code.text=string.Join("\n",lines.Select((line,i)=>(i==state.line?"<mark=#BB953D55>":"")+(i+1).ToString("00")+"  <noparse>"+line+"</noparse>"+(i==state.line?"</mark>":"")));
        trace.text=state.explanation;console.text="MESSAGES (CONSOLE)\n"+(string.IsNullOrEmpty(state.output)?"(nothing shown yet)":state.output);console.richText=false;
        stepLabel.text="Instruction step "+step+" of "+(lesson.steps.Length-1);
        DrawState();challenge.text=lesson.challenge;challenge.richText=false;
        instructions.text="What messages or numbers will this code show?\n\nRead the code from top to bottom. Choose one answer. There is no timer or health penalty. You can turn back to watch the example again.";
        for(int i=0;i<3;i++){answers[i].GetComponentInChildren<TMP_Text>().text=((char)('A'+i))+".  "+lesson.answers[i];answers[i].interactable=!answered;}
        resultTitle.text=correct?"CORRECT":"LET'S REVIEW";resultTitle.color=correct?new Color(.08f,.42f,.16f):Ink;
        correctMark.SetActive(answered&&correct);
        resultAnswer.text="Your answer:\n"+(selectedAnswer<0?"":lesson.answers[selectedAnswer])+"\n\nCorrect output:\n"+lesson.answers[lesson.correct];resultAnswer.richText=false;
        resultExplanation.text=lesson.explanation+"\n\n"+(correct?"Your progress is saved. ":"Replay the example, then return to practice. ")+LearningQuests.Objective(mentor.workshop);
        for(int i=0;i<2;i++){int t=mentor.workshop*2+i;topics[i].GetComponentInChildren<TMP_Text>().text=(i+1)+". "+VisualCodeLessons.Get(language,t).title.Split('·')[1].Trim()+(WorkshopProgression.Passed(language,t,0)?"  / Done":"");topics[i].GetComponent<UnityEngine.UI.Image>().color=t==topic?Blue:Color.white;topicIcons[i].sprite=Art(TopicArt(t));}
        for(int i=0;i<3;i++){bool available=WorkshopProgression.Available(language,topic,i);bool passed=WorkshopProgression.Passed(language,topic,i);levels[i].interactable=available;levels[i].GetComponentInChildren<TMP_Text>().text=WorkshopProgression.TierName(i)+(passed?" / Done":available?"":" / Locked");levels[i].GetComponent<UnityEngine.UI.Image>().color=i==exerciseTier?Blue:Color.white;levelIcons[i].sprite=Art(passed?"Complete":available?"Book":"Lock");}
        previousStep.interactable=step>0;nextStep.interactable=step<lesson.steps.Length-1;
        practice.interactable=step==lesson.steps.Length-1&&WorkshopProgression.Available(language,topic,exerciseTier);
        if(step==lesson.steps.Length-1&&!WorkshopProgression.Available(language,topic,exerciseTier))trace.text+="\n\nPractice locked: "+WorkshopProgression.Requirement(topic,exerciseTier);
        previousPage.interactable=page>0;nextPage.interactable=page<2||page==2&&practice.interactable||page==4;
        nextPage.GetComponentInChildren<TMP_Text>().text=page==4?"Back to contents":"Next page >";
        previousPage.GetComponentInChildren<TMP_Text>().text=page==4?"< Review demo":"< Previous page";
    }
    void DrawState()
    {
        for(int i=cellsRoot.transform.childCount-1;i>=0;i--){var child=cellsRoot.transform.GetChild(i);child.gameObject.SetActive(false);if(Application.isPlaying)Destroy(child.gameObject);else DestroyImmediate(child.gameObject);}
        var state=lesson.steps[step];
        for(int i=0;i<state.cells.Length;i++)
        {
            var cell=Panel("State "+i,cellsRoot.transform,i==state.activeCell?new Color(.94f,.73f,.33f):new Color(.86f,.77f,.58f),true);
            Place(cell,i*152,0,144,108);var label=Text("Value",cell.transform,state.cells[i],23);Fill(label.gameObject,10);label.alignment=TextAlignmentOptions.Center;
            {
                string icon=TopicArt(topic);
                if(topic==2)icon=i==0?"Key":state.cells[i].Contains("OPEN")||state.cells[i].Contains("TRUE")?"Complete":"Lock";
                if(topic==3)icon=state.cells[i].Contains("visited")?"Complete":"Step";
                if(topic==5)icon=i==0?"Scroll":"Heal";
                Icon(icon,cell.transform,56,6,32,32);
                label.rectTransform.offsetMax=new Vector2(-10,-38);label.fontSizeMin=18;
            }
        }
    }
    void Build()
    {
        font=Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF")??TMP_Settings.defaultFontAsset;
        if(font!=null){textMaterial=new Material(font.material);textMaterial.DisableKeyword("UNDERLAY_ON");textMaterial.DisableKeyword("UNDERLAY_INNER");textMaterial.SetFloat("_OutlineWidth",0);textMaterial.SetFloat("_FaceDilate",0);}
        root=new GameObject("Workshop Learning Book Canvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster),typeof(CanvasGroup));root.transform.SetParent(transform,false);
        root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;root.GetComponent<Canvas>().sortingOrder=260;
        var scaler=root.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
        var shade=Panel("Dim world",root.transform,new Color(.025f,.02f,.015f,.94f),false);Fill(shade,0);
        var desk=Panel("Book cover",shade.transform,new Color(.22f,.12f,.065f),false);Centre(desk,1200,604,0,0);
        bookCover=desk;
        book=new GameObject("Open book",typeof(RectTransform),typeof(CanvasGroup),typeof(WorkshopBookTurn));book.transform.SetParent(shade.transform,false);Centre(book,1160,580,0,0);bookInput=book.GetComponent<CanvasGroup>();turn=book.GetComponent<WorkshopBookTurn>();
        leftPage=Panel("Left paper sheet",book.transform,Color.white,true);Place(leftPage,10,10,570,560);
        rightPage=Panel("Right paper sheet",book.transform,new Color(.98f,.96f,.91f),true);Place(rightPage,580,10,570,560);
        var spine=Panel("Book spine shadow",book.transform,new Color(.22f,.13f,.07f,.38f),false);Place(spine,574,10,12,560);spine.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
        for(int i=0;i<5;i++){leftSections[i]=Section("Left chapter "+i,leftPage.transform);rightSections[i]=Section("Right chapter "+i,rightPage.transform);}
        header=Text("Teacher",shade.transform,"",23);header.color=new Color(.96f,.86f,.66f);Centre(header.gameObject,800,34,0,326);
        Button("Close [Esc]",shade.transform,Close,1100,16,150,42);
        pageNumber=Text("Page number",shade.transform,"",17);pageNumber.color=new Color(.96f,.86f,.66f);Centre(pageNumber.gameObject,300,32,0,-326);pageNumber.alignment=TextAlignmentOptions.Center;
        previousPage=Button("< Previous page",shade.transform,PreviousPage,60,660,220,42);
        nextPage=Button("Next page >",shade.transform,NextPage,1000,660,220,42);
        Button("Contents",shade.transform,Contents,60,16,150,42);
        overview=Text("Book introduction",leftSections[0].transform,"",25);Place(overview.gameObject,44,40,482,470);overview.fontSizeMin=21;
        Icon("Book",leftSections[0].transform,44,478,56,56);
        Icon("Quill",leftSections[0].transform,454,474,68,68);
        Title(rightSections[0],"CONTENTS",42);
        for(int i=0;i<2;i++){int index=i;topics[i]=Button("Chapter",rightSections[0].transform,()=>SelectTopic(mentor.workshop*2+index),44,110+i*68,482,56);topicIcons[i]=Icon("Book",topics[i].transform,14,8,40,40);topics[i].GetComponentInChildren<TMP_Text>().rectTransform.offsetMin=new Vector2(64,8);}
        var difficulty=Text("Level heading",rightSections[0].transform,"PRACTICE LEVEL FOR SELECTED CHAPTER",17);Place(difficulty.gameObject,44,275,482,32);
        for(int i=0;i<3;i++){int level=i;levels[i]=Button("Level",rightSections[0].transform,()=>SelectExerciseTier(level),44,325+i*60,482,48);levelIcons[i]=Icon("Book",levels[i].transform,18,8,32,32);levels[i].GetComponentInChildren<TMP_Text>().rectTransform.offsetMin=new Vector2(64,8);}
        chapterIcon=Icon("Book",leftSections[1].transform,44,48,48,48);
        chapter=Text("Chapter title",leftSections[1].transform,"",28);chapter.fontStyle=FontStyles.Bold;chapter.fontSizeMin=22;Place(chapter.gameObject,108,40,418,90);
        definition=Text("Meaning",leftSections[1].transform,"",24);Place(definition.gameObject,44,150,482,292);definition.fontSizeMin=18;
        context=Text("Code context",leftSections[1].transform,"",17);Place(context.gameObject,44,455,482,70);
        Title(rightSections[1],"A FIRST EXAMPLE",42);example=CodeText("Example code",rightSections[1].transform,110,300);
        var readHint=Text("Reading hint",rightSections[1].transform,"Read the meaning first. Turn the page to watch each instruction change the program's state.",22);Place(readHint.gameObject,44,442,482,84);
        Title(leftSections[2],"WATCH THE CODE",40);code=CodeText("Highlighted code",leftSections[2].transform,100,260);
        trace=Text("Step explanation",leftSections[2].transform,"",22);Place(trace.gameObject,44,390,482,120);trace.fontSizeMin=18;
        Title(rightSections[2],"PROGRAM STATE",40);cellsRoot=Section("State boxes",rightSections[2].transform);Place(cellsRoot,52,115,456,108);
        stateLegend=Text("Visual icon meaning",rightSections[2].transform,"",17);stateLegend.fontSizeMin=14;Place(stateLegend.gameObject,44,230,482,30);
        console=Text("Console",rightSections[2].transform,"",24);Place(console.gameObject,44,265,482,130);
        stepLabel=Text("Step counter",rightSections[2].transform,"",18);Place(stepLabel.gameObject,44,410,482,30);
        previousStep=Button("< Step",rightSections[2].transform,()=>Move(-1),44,456,140,42);replay=Button("Replay",rightSections[2].transform,Replay,200,456,140,42);nextStep=Button("Step >",rightSections[2].transform,()=>Move(1),356,456,170,42);
        practice=Button("Turn to practice >",rightSections[2].transform,BeginPrediction,200,510,326,36);
        Title(leftSections[3],"TRY IT YOURSELF",40);challenge=CodeText("Question code",leftSections[3].transform,110,330);
        var practiceHint=Text("Practice hint",leftSections[3].transform,"This example is different from the demonstration. Follow its instructions in order.",21);Place(practiceHint.gameObject,44,464,482,70);
        instructions=Text("Answer instructions",rightSections[3].transform,"",23);Place(instructions.gameObject,44,40,482,155);
        for(int i=0;i<3;i++){int answer=i;answers[i]=Button("Answer",rightSections[3].transform,()=>Submit(answer),44,230+i*94,482,80);answers[i].GetComponentInChildren<TMP_Text>().fontSizeMin=16;}
        resultTitle=Text("Result title",leftSections[4].transform,"",32);resultTitle.fontStyle=FontStyles.Bold;Place(resultTitle.gameObject,44,44,482,60);
        resultAnswer=Text("Answer comparison",leftSections[4].transform,"",26);Place(resultAnswer.gameObject,44,145,482,330);resultAnswer.fontSizeMin=20;
        Title(rightSections[4],"WHY THIS HAPPENS",44);resultExplanation=Text("Answer explanation",rightSections[4].transform,"",24);Place(resultExplanation.gameObject,44,120,482,300);resultExplanation.fontSizeMin=19;
        Button("Replay demonstration",rightSections[4].transform,Replay,44,452,482,48);
        Button("Return to contents",rightSections[4].transform,Contents,44,512,482,36);
        var tick=Section("Green correct check",leftSections[4].transform);Place(tick,435,44,60,50);
        Icon("Complete",tick.transform,4,2,48,48);
        correctMark=tick;
        bookChrome=shade.transform.Cast<Transform>().Where(t=>t.gameObject!=book&&t.gameObject!=bookCover).Select(t=>{
            var group=t.GetComponent<CanvasGroup>();if(group==null)group=t.gameObject.AddComponent<CanvasGroup>();return group;
        }).ToArray();
    }
    public bool AnswerWasCorrect=>answered&&correct;
    TMP_Text CodeText(string name,Transform parent,float y,float height){var t=Text(name,parent,"",24);Place(t.gameObject,44,y,482,height);t.fontSizeMin=13;t.textWrappingMode=TextWrappingModes.NoWrap;return t;}
    void Title(GameObject section,string value,float y){Icon("Banner",section.transform,36,y-24,498,84,false);var t=Text(value,section.transform,value,25);t.fontStyle=FontStyles.Bold;t.alignment=TextAlignmentOptions.Center;Place(t.gameObject,70,y,430,36);}
    static Sprite Art(string name)
    {
        Sprite cached;if(artwork.TryGetValue(name,out cached)&&cached!=null)return cached;
        var texture=Resources.Load<Texture2D>("UI/WorkshopArt/"+name);if(texture==null)return null;
        texture.filterMode=FilterMode.Point;texture.wrapMode=TextureWrapMode.Clamp;
        cached=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);artwork[name]=cached;return cached;
    }
    UnityEngine.UI.Image Icon(string name,Transform parent,float x,float y,float width,float height,bool preserve=true)
    {
        var go=Panel(name+" artwork",parent,Color.white,false);Place(go,x,y,width,height);var image=go.GetComponent<UnityEngine.UI.Image>();image.sprite=Art(name);image.preserveAspect=preserve;image.raycastTarget=false;return image;
    }
    GameObject Section(string name,Transform parent){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);Fill(go,0);return go;}
    GameObject Panel(string name,Transform parent,Color color,bool sheet){var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(parent,false);var image=go.GetComponent<UnityEngine.UI.Image>();image.color=color;if(sheet){image.sprite=Paper;image.type=UnityEngine.UI.Image.Type.Sliced;}return go;}
    TMP_Text Text(string name,Transform parent,string value,int size){var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var text=go.GetComponent<TMP_Text>();if(font!=null)text.font=font;if(textMaterial!=null)text.fontSharedMaterial=textMaterial;text.text=value;text.color=Ink;text.fontSize=size;text.enableAutoSizing=true;text.fontSizeMin=size-2;text.fontSizeMax=size;text.textWrappingMode=TextWrappingModes.Normal;text.raycastTarget=false;return text;}
    UnityEngine.UI.Button Button(string name,Transform parent,UnityEngine.Events.UnityAction action,float x,float y,float width,float height){var go=Panel(name,parent,new Color(.91f,.83f,.67f),true);Place(go,x,y,width,height);var button=go.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=go.GetComponent<UnityEngine.UI.Image>();button.onClick.AddListener(action);var label=Text("Label",go.transform,name,21);label.richText=false;label.alignment=TextAlignmentOptions.Center;label.fontSizeMin=16;Fill(label.gameObject,8);var colors=button.colors;colors.highlightedColor=Blue;colors.selectedColor=Blue;colors.disabledColor=new Color(.78f,.77f,.72f);button.colors=colors;return button;}
    static void Centre(GameObject go,float width,float height,float x,float y){var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(width,height);r.anchoredPosition=new Vector2(x,y);}
    static void Place(GameObject go,float x,float y,float width,float height){var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);}
    static void Fill(GameObject go,float inset){var r=(RectTransform)go.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.one*inset;r.offsetMax=-Vector2.one*inset;}
}

