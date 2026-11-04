using System.Collections;
using System.Text;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Unrolls the supplied scroll parts, waits for review, then rolls shut before returning.</summary>
public sealed class BattleReportScroll : MonoBehaviour
{
    private static BattleReportScroll current;
    public static bool IsOpen=>current!=null;
    private RectTransform paper,upper,lower;
    private CanvasGroup content;
    private TMP_Text title,summary,details;
    private bool ready,continuePressed,restore;
    private float previousTime;
    private TMP_FontAsset font;
    private Material textMaterial;
    private readonly System.Collections.Generic.List<Sprite> ownedSprites=new System.Collections.Generic.List<Sprite>();
    public static IEnumerator Present(string outcome,int xp,int gold=0)
    {
        if(!BattleQuestionReport.Pending||IsOpen)yield break;
        BattleQuestionReport.Finish();
        var go=new GameObject("Battle Report Scroll",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(CanvasGroup));
        var report=go.AddComponent<BattleReportScroll>();current=report;
        yield return report.Run(outcome,xp,gold);
        Destroy(go);
    }
    IEnumerator Run(string outcome,int xp,int gold)
    {
        previousTime=Time.timeScale;restore=true;Time.timeScale=0;Build();
        title.text=outcome+" / BATTLE CHRONICLE";
        summary.text=BattleQuestionReport.Summary()+"\nRewards: "+xp+" XP / "+gold+" gold";
        details.text=Details();
        SettingsUiVisibility.EnsureInstance();SettingsUiVisibility.RefreshNow();
        yield return Animate(true);ready=true;
        while(!continuePressed)yield return null;
        var save=SaveSlots.Read(SaveSlots.Active);
        if(outcome=="VICTORY"&&BattleQuestionReport.EncounterId==BattleTutorialGuide.EncounterId&&save?.defeatedEncounters?.Contains(BattleTutorialGuide.EncounterId)==true)WardenQuest.Record("tutorial-report");
        if(outcome=="VICTORY"&&BattleQuestionReport.EncounterId==WardenQuest.FinalEncounter&&save?.defeatedEncounters?.Contains(WardenQuest.FinalEncounter)==true)WardenQuest.Record("final-report");
        ready=false;yield return Animate(false);
        if(!GameControlsGuide.SettingsOpen)Time.timeScale=previousTime;restore=false;
    }
    void Update(){if(ready&&!GameControlsGuide.SettingsOpen&&Input.GetKeyDown(KeyCode.Return))continuePressed=true;}
    public void Continue(){if(ready&&!GameControlsGuide.SettingsOpen)continuePressed=true;}
    public void Sample(float open)
    {
        open=Mathf.Clamp01(open);paper.sizeDelta=new Vector2(760,Mathf.Lerp(2,600,open));
        upper.anchoredPosition=new Vector2(0,Mathf.Lerp(28,308,open));lower.anchoredPosition=new Vector2(0,-Mathf.Lerp(28,308,open));
        content.alpha=Mathf.Clamp01((open-.8f)/.2f);content.interactable=content.blocksRaycasts=open>=.999f;
    }
    IEnumerator Animate(bool opening)
    {
        float elapsed=0;Sample(opening?0:1);
        while(elapsed<.55f){if(!GameControlsGuide.SettingsOpen)elapsed+=Time.unscaledDeltaTime;float p=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.55f));Sample(opening?p:1-p);yield return null;}
        Sample(opening?1:0);
    }
    public static string Details()
    {
        if(BattleQuestionReport.Entries.Count==0)return "No questions were attempted in this battle.\n\nAccuracy is unavailable until at least one question is answered or times out.";
        var text=new StringBuilder("<color=#49351D>Scroll down to review your questions.</color>\n\n");int number=0;
        foreach(var group in BattleQuestionReport.Entries.GroupBy(e=>new{e.language,e.prompt,e.code,e.correctAnswer}))
        {
            var e=group.Last();bool mixed=group.Select(a=>a.outcome).Distinct().Count()>1;
            string state=e.outcome=="correct"?"CORRECT":e.outcome=="incorrect"?"WRONG":e.outcome=="timed_out"?"TIMED OUT":"UNFINISHED";
            string color=e.outcome=="correct"?"#18542A":e.outcome=="incorrect"?"#80291D":"#66411C";
            if(mixed){state="MIXED RESULTS";color="#66411C";}
            text.Append("<size=25><b>QUESTION ").Append(++number).Append("</b></size>  <color=").Append(color).Append("><b>").Append(state).Append("</b></color>\n");
            text.Append("<size=18><color=#49351D>Topic: ").Append(Safe(e.topic)).Append("</color></size>\n\n");
            text.Append(Safe(e.prompt)).Append("\n\n");
            if(!string.IsNullOrEmpty(e.code))text.Append("<b>CODE</b>\n<color=#182D38>").Append(Safe(e.code)).Append("</color>\n\n");
            if(group.Count()>1)text.Append("<b>Seen ").Append(group.Count()).Append(" times</b> / Correct: ").Append(group.Count(a=>a.outcome=="correct")).Append(" / Wrong: ").Append(group.Count(a=>a.outcome=="incorrect")).Append(" / Timed out: ").Append(group.Count(a=>a.outcome=="timed_out")).Append(" / Unfinished: ").Append(group.Count(a=>a.outcome=="abandoned")).Append("\n\n");
            text.Append(group.Count()>1?"<b>Latest answer</b>\n":"<b>Your answer</b>\n").Append(Safe(e.answer)).Append("\n\n<b>Correct answer</b>\n<color=#18542A>").Append(Safe(e.correctAnswer)).Append("</color>\n\n<size=18><color=#49351D>Total response time: ").Append(group.Sum(a=>a.duration).ToString("0.0")).Append(" seconds</color></size>\n\n<color=#806338>________________________________________</color>\n\n");
        }
        return text.ToString();
    }
    static string Safe(string value)=>"<noparse>"+(value??"").Replace("</noparse>","")+"</noparse>";
    void Build()
    {
        font=Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF")??TMP_Settings.defaultFontAsset;
        if(font!=null){textMaterial=new Material(font.material);textMaterial.DisableKeyword("UNDERLAY_ON");textMaterial.DisableKeyword("UNDERLAY_INNER");textMaterial.SetFloat("_OutlineWidth",0);textMaterial.SetFloat("_FaceDilate",0);}
        GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;GetComponent<Canvas>().sortingOrder=1200;
        var scaler=GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        var shade=Image("Dim battle",transform,null,new Color(.015f,.01f,.005f,.94f));var sr=(RectTransform)shade.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.offsetMin=sr.offsetMax=Vector2.zero;
        paper=(RectTransform)Image("Scroll paper",transform,"paper",Color.white).transform;Centre(paper,760,600);
        var body=new GameObject("Report contents",typeof(RectTransform),typeof(CanvasGroup));body.transform.SetParent(paper,false);Centre((RectTransform)body.transform,720,500);content=body.GetComponent<CanvasGroup>();
        title=Text("Report title",body.transform,28);Place(title.rectTransform,20,0,680,38);title.fontStyle=FontStyles.Bold;title.alignment=TextAlignmentOptions.Center;title.enableAutoSizing=true;title.fontSizeMin=20;title.fontSizeMax=28;
        summary=Text("Battle summary",body.transform,19);Place(summary.rectTransform,24,46,672,94);summary.alignment=TextAlignmentOptions.Center;
        var viewport=Image("Question list viewport",body.transform,null,new Color(1,1,1,.001f));Place((RectTransform)viewport.transform,26,150,642,288);viewport.AddComponent<RectMask2D>();
        details=Text("Questions and answers",viewport.transform,23);details.richText=true;details.textWrappingMode=TextWrappingModes.Normal;details.lineSpacing=5;details.paragraphSpacing=8;details.margin=new Vector4(8,6,18,12);
        var rect=details.rectTransform;rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,1);rect.offsetMin=rect.offsetMax=Vector2.zero;details.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var scroll=viewport.AddComponent<ScrollRect>();scroll.viewport=(RectTransform)viewport.transform;scroll.content=rect;scroll.horizontal=false;scroll.scrollSensitivity=35;scroll.movementType=ScrollRect.MovementType.Clamped;
        var track=Image("Question scrollbar",body.transform,null,new Color(.65f,.48f,.22f));Place((RectTransform)track.transform,680,150,14,288);
        var handle=Image("Scrollbar handle",track.transform,null,new Color(.27f,.15f,.06f));var hr=(RectTransform)handle.transform;hr.anchorMin=Vector2.zero;hr.anchorMax=Vector2.one;hr.offsetMin=new Vector2(2,2);hr.offsetMax=new Vector2(-2,-2);
        var bar=track.AddComponent<Scrollbar>();bar.direction=Scrollbar.Direction.BottomToTop;bar.handleRect=hr;bar.targetGraphic=handle.GetComponent<Image>();scroll.verticalScrollbar=bar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
        var button=Image("Continue",body.transform,null,new Color(.37f,.22f,.09f));Place((RectTransform)button.transform,190,448,340,42);var b=button.AddComponent<Button>();b.targetGraphic=button.GetComponent<Image>();b.onClick.AddListener(Continue);
        var caption=Text("Continue caption",button.transform,22);caption.text="Seal report & continue [Enter]";caption.enableAutoSizing=true;caption.fontSizeMin=17;caption.fontSizeMax=22;caption.fontStyle=FontStyles.Bold;caption.color=new Color(1,.94f,.72f);caption.alignment=TextAlignmentOptions.Center;Centre(caption.rectTransform,330,38);
        var border=button.AddComponent<Outline>();border.effectColor=new Color(.69f,.46f,.16f);border.effectDistance=new Vector2(2,2);
        var headingRule=Image("Gold heading rule",body.transform,null,new Color(.51f,.34f,.12f));Place((RectTransform)headingRule.transform,80,39,560,2);headingRule.GetComponent<Image>().raycastTarget=false;
        upper=(RectTransform)Image("Upper scroll roller",transform,"upper",Color.white).transform;Centre(upper,920,94);
        lower=(RectTransform)Image("Lower scroll roller",transform,"lower",Color.white).transform;Centre(lower,920,94);
    }
    GameObject Image(string name,Transform parent,string asset,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var image=go.GetComponent<Image>();image.color=color;
        if(asset!=null){var tex=Resources.Load<Texture2D>("UI/BattleReport/"+asset);if(tex!=null){tex.wrapMode=TextureWrapMode.Clamp;image.sprite=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),new Vector2(.5f,.5f),100);ownedSprites.Add(image.sprite);}}
        return go;
    }
    TMP_Text Text(string name,Transform parent,int size){var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var text=go.GetComponent<TMP_Text>();if(font!=null)text.font=font;if(textMaterial!=null)text.fontSharedMaterial=textMaterial;text.fontSize=size;text.color=new Color(.18f,.1f,.035f);text.raycastTarget=false;return text;}
    static void Centre(RectTransform r,float width,float height){r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(width,height);}
    static void Place(RectTransform r,float x,float y,float width,float height){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);}
    void OnDestroy(){if(current==this)current=null;if(restore&&!GameControlsGuide.SettingsOpen)Time.timeScale=previousTime;if(textMaterial!=null){if(Application.isPlaying)Destroy(textMaterial);else DestroyImmediate(textMaterial);}foreach(var sprite in ownedSprites)if(sprite!=null){if(Application.isPlaying)Destroy(sprite);else DestroyImmediate(sprite);}}
}
