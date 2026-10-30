using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Optional, event-driven coaching for the opening-area practice encounter.</summary>
public sealed class BattleTutorialGuide : MonoBehaviour
{
    public const string EncounterId = "Tutorial Training Goblin";
    private static BattleTutorialGuide instance;
    public static bool IsTraining => instance != null;
    public static bool BlocksInput => instance != null && instance.modalOpen;
    public static bool IsTutorialEncounter(string id) => id == EncounterId;
    private GameObject canvasRoot, modal, hintPanel;
    private TMP_Text title, body, hint, continueLabel;
    private TMP_Text pageLabel;
    private Button backButton;
    private Button reviewButton;
    private sealed class GuidePage
    {
        public string heading, message, button;
        public Action next;
        public bool intro;
    }
    private readonly List<GuidePage> history = new List<GuidePage>();
    private readonly List<GuidePage> reviewPages = new List<GuidePage>();
    private bool reviewingHistory;
    private int viewedPage;
    private List<GuidePage> VisiblePages => reviewingHistory ? reviewPages : history;
    private bool modalOpen, skipped, explainedQuestion, explainedTurn, playedCard, enemyTurnSeen, turnSummaryPending, lastCorrect;
    private float previousTimeScale;
    private int openedFrame, practiceIndex;
    private Action continuation;
    private readonly List<MultipleChoiceQuestionData> practiceQuestions = new List<MultipleChoiceQuestionData>();
    private static readonly Color Gold = new Color(1f,.86f,.54f);
    private static readonly Color Ink = new Color(.19f,.095f,.035f);

    void Awake() { instance = this; BuildUI(); }
    void OnEnable()
    {
        ActionSystem.SubscribedReaction<PlayCardGA>(OnCardPlayed,ReactionTiming.POST);
        ActionSystem.SubscribedReaction<EnemyTurnGA>(OnEnemyTurn,ReactionTiming.POST);
    }
    void OnDisable()
    {
        ActionSystem.UnSubscribedReaction<PlayCardGA>(OnCardPlayed,ReactionTiming.POST);
        ActionSystem.UnSubscribedReaction<EnemyTurnGA>(OnEnemyTurn,ReactionTiming.POST);
        if(modalOpen) Dismiss(false);
    }
    void OnDestroy()
    {
        if(instance==this) instance=null;
        foreach(var question in practiceQuestions) if(question!=null)
        {
            if(Application.isPlaying)Destroy(question);else DestroyImmediate(question);
        }
    }
    IEnumerator Start()
    {
        // Allow MatchSetup.Start to begin the initial draw even if this guide
        // was already attached in an authored or preview scene.
        yield return null;
        SetHint("Preparing your practice hand...");
        while(ActionSystem.Instance==null||ActionSystem.Instance.IsPerforming) yield return null;
        if(!EncounterBattleReturn.BattleEnded) ShowIntro(0);
    }
    void Update()
    {
        var question=FindFirstObjectByType<QuestionUI>();
        bool answering=question!=null&&question.isActiveAndEnabled;
        hintPanel.SetActive(!modalOpen&&!answering&&!EncounterBattleReturn.BattleEnded&&!GameControlsGuide.SettingsOpen);
        reviewButton.interactable=ActionSystem.Instance==null||!ActionSystem.Instance.IsPerforming;
        if(modalOpen&&Time.frameCount>openedFrame)
        {
            if(Input.GetKeyDown(KeyCode.LeftArrow)) Back();
            else if(Input.GetKeyDown(KeyCode.Return)) Dismiss(true);
        }
        if(!modalOpen&&!skipped&&turnSummaryPending&&!answering&&ActionSystem.Instance!=null&&!ActionSystem.Instance.IsPerforming&&
            !EncounterBattleReturn.BattleEnded&&HeroSystem.Instance!=null&&HeroSystem.Instance.HeroView.CurrentHealth>0&&EnemySystem.Instance.Enemies.Count>0)
        {
            turnSummaryPending=false;
            ShowModal("YOUR NEXT TURN","The enemy has finished its turn. Your mana refills to 5, the old hand is discarded, and 5 cards are drawn.\n\nCheck your health, choose cards you can afford, and keep reducing the enemy's health. Guard or armor cards help you survive. You can now finish the practice battle at your own pace.","Continue practice",null);
        }
    }

    private static readonly string[] IntroTitles={"FIELD TRAINING  /  1 OF 4","YOUR HAND AND MANA  /  2 OF 4","CARDS AND QUESTIONS  /  3 OF 4","TURNS AND VICTORY  /  4 OF 4"};
    private static readonly string[] IntroText={
            "This is a practice battle against a weak training goblin.\n\nYour health bar shows how much damage you can take. The enemy has its own health bar and an ATK value. Reduce its health to zero to win. If yours reaches zero, the battle ends in defeat.\n\nThe guide pauses the battle while you read.",
            "Your cards are shown along the bottom. Hover over a card to read its effect and mana cost.\n\nYou have 5 mana each turn, shown by the blue orbs near your health display. You can play several cards as long as you can pay their costs. An unaffordable card cannot be used.\n\nAttack cards deal damage; support cards can give armor or other effects.",
            "For a card that needs an enemy target, drag its targeting arrow onto the goblin and release. For a card without a target, drag it upward out of your hand and release.\n\nA programming question appears before the card resolves. A correct answer activates the effect. A wrong answer or timeout lets the enemy counterattack. The card and mana are still spent.",
            "When you have finished playing cards, press End Turn. The enemy attacks, your old hand is discarded, mana refills to 5, and you draw 5 cards.\n\nThis practice battle gives you 60 seconds per question. Defeat the goblin to return to the opening area. Retreat also returns you, but leaves the goblin available to try again."
        };

    void ShowIntro(int page)
    {
        ShowModal(IntroTitles[page],IntroText[page],page==3?"Begin practice":"Next [Enter]",()=>{
            if(skipped)return;
            if(page<3)ShowIntro(page+1);
            else SetHint("Try a card: hover to inspect it, then drag an affordable attack card onto the goblin. Non-targeted cards are dragged above your hand.");
        },true);
    }
    void ShowModal(string heading,string message,string button,Action next,bool intro=false)
    {
        if(!modalOpen)previousTimeScale=Time.timeScale;
        reviewingHistory=false;
        history.Add(new GuidePage {heading=heading,message=message,button=button,next=next,intro=intro});
        viewedPage=history.Count-1;
        DisplayPage();
        modalOpen=true;openedFrame=Time.frameCount;
        modal.SetActive(true);hintPanel.SetActive(false);Time.timeScale=0f;
        if(CardViewHoverSystem.Instance!=null)CardViewHoverSystem.Instance.Hide();
    }
    void DisplayPage()
    {
        var page=VisiblePages[viewedPage];
        title.text=page.heading;body.text=page.message;
        bool latest=viewedPage==VisiblePages.Count-1;
        continueLabel.text=reviewingHistory?(latest?"Return to battle":"Forward [Enter]"):
            latest?page.button:"Forward [Enter]";
        continuation=page.next;
        backButton.interactable=viewedPage>0;
        pageLabel.text=reviewingHistory?"REVIEW  ·  "+(viewedPage+1)+" / "+VisiblePages.Count:
            latest?"FIELD MANUAL":"EARLIER GUIDE";
    }
    public void Back()
    {
        if(!modalOpen||viewedPage<=0)return;
        viewedPage--;DisplayPage();
    }
    public void ReviewGuides()
    {
        if(modalOpen||EncounterBattleReturn.BattleEnded||
            (ActionSystem.Instance!=null&&ActionSystem.Instance.IsPerforming)||GameControlsGuide.SettingsOpen)return;
        var question=FindFirstObjectByType<QuestionUI>();
        if(question!=null&&question.isActiveAndEnabled)return;
        reviewPages.Clear();
        for(int i=0;i<IntroTitles.Length;i++)reviewPages.Add(new GuidePage {heading=IntroTitles[i],message=IntroText[i],intro=true});
        reviewPages.AddRange(history.FindAll(page=>!page.intro));
        previousTimeScale=Time.timeScale;reviewingHistory=true;viewedPage=0;modalOpen=true;openedFrame=Time.frameCount;
        DisplayPage();modal.SetActive(true);hintPanel.SetActive(false);Time.timeScale=0f;
    }
    void Dismiss(bool invoke)
    {
        if(!modalOpen)return;
        if(invoke&&viewedPage<VisiblePages.Count-1){viewedPage++;DisplayPage();return;}
        modalOpen=false;modal.SetActive(false);Time.timeScale=previousTimeScale;
        // History navigation never replays a card, question, or enemy-turn callback.
        var next=reviewingHistory?null:continuation;continuation=null;reviewingHistory=false;
        if(invoke)next?.Invoke();
    }
    void Skip()
    {
        skipped=true;viewedPage=VisiblePages.Count-1;DisplayPage();Dismiss(true);
        SetHint("Guides skipped. Use Review Guide whenever you want to reread the field manual.");
    }
    void SetHint(string message) { if(hint!=null)hint.text=message; }
    void OnCardPlayed(PlayCardGA action)
    {
        if(skipped)return;
        playedCard=true;
        SetHint((lastCorrect?"Correct: your card's effect resolved. ":"Wrong answer or timeout: the card was spent and the goblin counterattacked. ")+ (enemyTurnSeen?"Keep practicing and defeat the goblin.":"Now try End Turn to see how the enemy turn and refill work."));
    }
    void OnEnemyTurn(EnemyTurnGA action)
    {
        if(skipped)return;
        if(!enemyTurnSeen)turnSummaryPending=true;
        enemyTurnSeen=true;
        SetHint(playedCard?"Your new turn is ready. Use your cards to finish the practice battle.":"Your new turn is ready. Try an affordable card, then answer its question.");
    }
    public static IEnumerator ExplainQuestion()
    {
        var guide=instance;
        if(guide==null||guide.skipped||guide.explainedQuestion)yield break;
        guide.explainedQuestion=true;
        guide.ShowModal("READ, TRACE, THEN ANSWER","Your first programming question is next. Read the question, trace the code, and click an answer.\n\nThe timer begins only after you close this guide. You have at least 60 seconds in practice. Correct answers activate your card; wrong answers or timeouts trigger a counterattack.\n\nThere is no need to rush. You can study the Almanac [L] back in the opening area.","Show the question",null);
        while(guide!=null&&guide.modalOpen)yield return null;
    }
    public static void NotifyAnswer(bool correct) { if(instance!=null)instance.lastCorrect=correct; }
    public static bool ExplainEndTurn(Action proceed)
    {
        var guide=instance;
        if(guide==null||guide.skipped||guide.explainedTurn)return false;
        guide.explainedTurn=true;
        guide.ShowModal("WATCH THE ENEMY TURN","End Turn hands control to the enemy. Watch the attack and your health bar.\n\nAfter the enemy finishes, your mana refills to 5 and a new hand of 5 cards is drawn. Unplayed cards go to the discard pile. When the draw pile runs out, discarded cards are shuffled back in.\n\nYou can inspect the draw and discard piles by clicking their icons.","Start enemy turn",proceed);
        return true;
    }
    public static IEnumerator ExplainResult(bool victory)
    {
        var guide=instance;
        if(guide==null||guide.skipped)yield break;
        guide.turnSummaryPending=false;
        guide.ShowModal(victory?"PRACTICE COMPLETE":"PRACTICE ENDED",victory?
            "You defeated the training goblin. You have practiced health, mana, cards, questions, and turn order.\n\nYou will now return to the opening area. This practice enemy stays defeated in your active save slot. Use the Almanac [L] to keep learning before the cave battles.":
            "Your health reached zero. You will return to the opening area, and the training goblin will remain available for another attempt.\n\nRead the Almanac [L], inspect card costs and effects, and try using defensive cards before ending your turn.","Return to exploration",null);
        while(guide!=null&&guide.modalOpen)yield return null;
    }
    public static QuestionData NextPracticeQuestion()
    {
        if(instance==null)return null;
        var guide=instance;
        if(guide.practiceQuestions.Count==0)
        {
            string language=CharacterAlmanac.LanguageForCharacter(PlayerPrefs.GetInt("SelectedCharacter",0));
            string print=language=="java"?"System.out.println":language=="csharp"?"Console.WriteLine":"print";
            string end=language=="python"?"":";";
            string declaration=language=="python"?"":"int ";
            string falseText=language=="java"?"false":"False";
            string trueText=language=="java"?"true":"True";
            string[] prompts={"What number does this code print?","What is the final value of coins?","Does this comparison print true or false?"};
            string[] code={declaration+"sparks = 4"+end+"\n"+print+"(sparks + 2)"+end,
                declaration+"coins = 9"+end+"\ncoins -= 3"+end+"\n"+print+"(coins)"+end,
                declaration+"lamps = 3"+end+"\n"+print+"(lamps >= 5)"+end};
            string[][] answers={new[]{"4","6","2","8"},new[]{"3","9","6","12"},new[]{falseText,trueText,"0","5"}};
            int[] correct={1,2,0};
            for(int i=0;i<3;i++)
            {
                var question=ScriptableObject.CreateInstance<MultipleChoiceQuestionData>();question.name="Training "+language+" "+i;
                question.questionText=prompts[i];question.codeSnippet=code[i];question.options=answers[i];question.correctAnswerIndex=correct[i];question.timeLimitInSeconds=60;
                guide.practiceQuestions.Add(question);
            }
        }
        return guide.practiceQuestions[guide.practiceIndex++%guide.practiceQuestions.Count];
    }

    void BuildUI()
    {
        canvasRoot=new GameObject("Battle Training Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvasRoot.transform.SetParent(transform,false);
        canvasRoot.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvasRoot.GetComponent<Canvas>().sortingOrder=400;
        var scaler=canvasRoot.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        hintPanel=Panel("Training Hint",canvasRoot.transform,true);var hr=(RectTransform)hintPanel.transform;hr.anchorMin=hr.anchorMax=new Vector2(.5f,1);hr.pivot=new Vector2(.5f,1);hr.anchoredPosition=new Vector2(0,-12);hr.sizeDelta=new Vector2(620,110);hintPanel.GetComponent<Image>().raycastTarget=false;
        var hintHeading=Label("Training Hint Heading",hintPanel.transform,17,Ink);hintHeading.text="FIELD TRAINING";hintHeading.fontStyle=FontStyles.Bold;Stretch(hintHeading.gameObject,20,9,190,76);
        hint=Label("Training Hint Text",hintPanel.transform,19,Ink);Stretch(hint.gameObject,20,38,20,12);hint.enableAutoSizing=true;hint.fontSizeMin=16;hint.fontSizeMax=19;
        reviewButton=Button("Review Guide",hintPanel.transform,"Review guide",ReviewGuides);var rr=(RectTransform)reviewButton.transform;rr.anchorMin=rr.anchorMax=Vector2.one;rr.pivot=Vector2.one;rr.anchoredPosition=new Vector2(-18,-8);rr.sizeDelta=new Vector2(156,28);
        modal=Panel("Training Overlay",canvasRoot.transform,false);modal.GetComponent<Image>().sprite=null;modal.GetComponent<Image>().color=new Color(0,0,0,.76f);Stretch(modal,0,0,0,0);
        var frame=Panel("Guide Scroll",modal.transform,false);var fr=(RectTransform)frame.transform;fr.anchorMin=fr.anchorMax=new Vector2(.5f,.5f);fr.sizeDelta=new Vector2(880,450);
        var caption=Label("Field Manual Caption",frame.transform,14,Gold);caption.text="THE TRAVELLER'S FIELD MANUAL";caption.characterSpacing=2;caption.alignment=TextAlignmentOptions.Center;Stretch(caption.gameObject,28,12,28,418);
        title=Label("Guide Heading",frame.transform,26,Gold);Stretch(title.gameObject,28,34,28,380);title.fontStyle=FontStyles.Bold;title.alignment=TextAlignmentOptions.Center;
        var paper=Panel("Guide Parchment",frame.transform,true);Stretch(paper,16,78,16,88);
        body=Label("Guide Instructions",paper.transform,24,Ink);Stretch(body.gameObject,22,18,22,18);body.enableAutoSizing=true;body.fontSizeMin=18;body.fontSizeMax=24;
        var next=Button("Continue Guide",frame.transform,"Next [Enter]",()=>Dismiss(true));var nr=(RectTransform)next.transform;nr.anchorMin=nr.anchorMax=new Vector2(1,0);nr.pivot=new Vector2(1,0);nr.anchoredPosition=new Vector2(-26,24);nr.sizeDelta=new Vector2(250,46);continueLabel=next.GetComponentInChildren<TMP_Text>();
        var skip=Button("Skip Guides",frame.transform,"Skip guides",Skip);var sr=(RectTransform)skip.transform;sr.anchorMin=sr.anchorMax=Vector2.zero;sr.pivot=Vector2.zero;sr.anchoredPosition=new Vector2(26,24);sr.sizeDelta=new Vector2(180,46);
        backButton=Button("Back Guide",frame.transform,"Back [Left]",Back);var br=(RectTransform)backButton.transform;br.anchorMin=br.anchorMax=Vector2.zero;br.pivot=Vector2.zero;br.anchoredPosition=new Vector2(224,24);br.sizeDelta=new Vector2(170,46);
        pageLabel=Label("Guide Page",frame.transform,17,Gold);pageLabel.alignment=TextAlignmentOptions.Center;var pr=(RectTransform)pageLabel.transform;pr.anchorMin=pr.anchorMax=Vector2.zero;pr.pivot=Vector2.zero;pr.anchoredPosition=new Vector2(402,24);pr.sizeDelta=new Vector2(190,46);
        for(int x=0;x<2;x++)for(int y=0;y<2;y++)
        {
            var stud=new GameObject("Brass Corner Stud",typeof(RectTransform),typeof(Image));stud.transform.SetParent(frame.transform,false);var image=stud.GetComponent<Image>();image.color=Gold;image.raycastTarget=false;var rect=(RectTransform)stud.transform;rect.anchorMin=rect.anchorMax=new Vector2(x,y);rect.anchoredPosition=new Vector2(x==0?11:-11,y==0?11:-11);rect.sizeDelta=new Vector2(6,6);
        }
        modal.SetActive(false);
    }
    static GameObject Panel(string name,Transform parent,bool paper){var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var image=go.GetComponent<Image>();image.sprite=GameControlsGuide.Frame(paper);image.type=Image.Type.Sliced;return go;}
    static TMP_Text Label(string name,Transform parent,int size,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var text=go.GetComponent<TextMeshProUGUI>();text.fontSize=size;text.color=color;text.textWrappingMode=TextWrappingModes.Normal;text.raycastTarget=false;return text;}
    static Button Button(string name,Transform parent,string caption,UnityEngine.Events.UnityAction action){var go=Panel(name,parent,false);var button=go.AddComponent<Button>();button.onClick.AddListener(action);button.targetGraphic=go.GetComponent<Image>();var text=Label("Label",go.transform,21,Gold);text.text=caption;text.alignment=TextAlignmentOptions.Center;text.enableAutoSizing=true;text.fontSizeMin=16;text.fontSizeMax=21;Stretch(text.gameObject,10,5,10,5);return button;}
    static void Stretch(GameObject go,float left,float top,float right,float bottom){var rect=(RectTransform)go.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(left,bottom);rect.offsetMax=new Vector2(-right,-top);}
}
