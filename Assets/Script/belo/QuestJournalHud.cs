using TMPro;
using UnityEngine;

/// <summary>Supports both a fixed quest icon and a scene-authored sliding drawer.</summary>
[RequireComponent(typeof(CanvasGroup))]
public sealed class QuestJournalHud : MonoBehaviour
{
    [SerializeField] private RectTransform slideRoot;
    [SerializeField] private RectTransform panel;
    [SerializeField] private UnityEngine.UI.Button arrowButton;
    [SerializeField] private RectTransform arrowArtwork;
    [SerializeField] private TMP_Text objectiveLabel;
    [SerializeField, Min(0.05f)] private float slideDuration = 0.25f;

    private CanvasGroup visibility;
    private ElfQuestTracker tracker;
    private bool fallbackMinimized = true;
    private Vector2 closedPosition;
    private Vector3 arrowScale;
    private float reveal;
    private string previousObjective;
    private float originalPanelHeight,originalTextHeight;
    private Vector2 originalTextPosition;
    private UnityEngine.UI.ScrollRect objectiveScroll;
    private CanvasGroup panelVisibility;

    void Awake()
    {
        visibility = GetComponent<CanvasGroup>();
        ConfigureLayout();
        if(Application.isPlaying&&GetComponent<QuestReturnPanel>()==null)gameObject.AddComponent<QuestReturnPanel>();
        if(panel!=null)originalPanelHeight=panel.rect.height;
        if(objectiveLabel!=null){originalTextHeight=objectiveLabel.rectTransform.rect.height;originalTextPosition=objectiveLabel.rectTransform.anchoredPosition;}
        if (slideRoot != null) closedPosition = slideRoot.anchoredPosition;
        if (arrowArtwork != null) arrowScale = arrowArtwork.localScale;
        FindTracker();
        reveal = IsMinimized ? 0f : 1f;
        if (arrowButton != null) arrowButton.onClick.AddListener(ToggleJournal);
        ApplyPanelVisibility();
    }

    public void ToggleJournal()
    {
        if (GameControlsGuide.GameplayOverlayHidden || GameControlsGuide.MapHidden) return;
        FindTracker();
        if (tracker != null) tracker.ToggleJournal();
        else fallbackMinimized = !fallbackMinimized;
        ApplyPanelVisibility();
    }

    void Update()
    {
        bool visible = !GameControlsGuide.GameplayOverlayHidden && !GameControlsGuide.MapHidden;
        visibility.alpha = visible ? 1f : 0f;
        visibility.interactable = visible;
        visibility.blocksRaycasts = visible;
        if (!visible) return;

        FindTracker();
        FitToCanvas();
        reveal = Mathf.MoveTowards(reveal, IsMinimized ? 0f : 1f,
            Mathf.Min(Time.unscaledDeltaTime, 0.05f) / Mathf.Max(0.05f, slideDuration));
        ApplyPanelVisibility();
        string objective = tracker != null ? tracker.ObjectiveText : "Speak to the elf at the cave entrance [E].";
        if (objectiveLabel != null && objective != previousObjective)
        {
            objectiveLabel.text = objective;
            if(objectiveScroll!=null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(objectiveLabel.rectTransform);
                objectiveScroll.verticalNormalizedPosition=1;
            }
            else if(panel!=null)
            {
                float height=Mathf.Max(originalTextHeight,objectiveLabel.GetPreferredValues(objective,objectiveLabel.rectTransform.rect.width,Mathf.Infinity).y+12);
                float extra=height-originalTextHeight;
                panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,originalPanelHeight+extra);
                objectiveLabel.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
                objectiveLabel.rectTransform.anchoredPosition=originalTextPosition+Vector2.down*extra*(1-objectiveLabel.rectTransform.pivot.y);
            }
            previousObjective = objective;
        }
    }

    public void ConfigureLayout()
    {
        foreach(var r in GetComponentsInChildren<RectTransform>(true))
        {
            if(panel==null&&r.name=="Quest Journal Panel")panel=r;
            if(slideRoot==null&&r.name=="Quest Journal Position")slideRoot=r;
            if(arrowButton==null&&r.name=="Quest Journal Arrow")arrowButton=r.GetComponent<UnityEngine.UI.Button>();
            if(arrowArtwork==null&&r.name=="Arrow Artwork")arrowArtwork=r;
            if(objectiveLabel==null&&r.name.Contains("Objective"))objectiveLabel=r.GetComponent<TMP_Text>();
        }
        if(panel==null)return;
        panel.sizeDelta=new Vector2(520,480);panel.pivot=new Vector2(1,1);
        panelVisibility=panel.GetComponent<CanvasGroup>();if(panelVisibility==null)panelVisibility=panel.gameObject.AddComponent<CanvasGroup>();
        foreach(var t in panel.GetComponentsInChildren<TMP_Text>(true))
        {
            if(t.name=="Journal Header"){t.text="QUEST JOURNAL  [Q]";t.fontSize=28;Top(t.rectTransform,18,12,484,38);}
            if(t.name=="Quest Name"){t.text="Your journey and rewards";t.fontSize=23;Top(t.rectTransform,18,54,484,32);}
        }
        objectiveScroll=panel.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
        if(objectiveLabel==null)return;
        if(objectiveScroll==null)
        {
            var go=new GameObject("Scrollable Objectives",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.ScrollRect));go.transform.SetParent(panel,false);go.GetComponent<UnityEngine.UI.Image>().color=new Color(.9f,.81f,.62f);
            objectiveScroll=go.GetComponent<UnityEngine.UI.ScrollRect>();
            var view=new GameObject("Viewport",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Mask));view.transform.SetParent(go.transform,false);var v=view.GetComponent<RectTransform>();v.anchorMin=Vector2.zero;v.anchorMax=Vector2.one;v.offsetMin=v.offsetMax=Vector2.zero;view.GetComponent<UnityEngine.UI.Mask>().showMaskGraphic=false;
            objectiveScroll.viewport=v;objectiveLabel.transform.SetParent(v,false);
        }
        objectiveLabel.fontSize=26;objectiveLabel.enableAutoSizing=false;objectiveLabel.lineSpacing=4;objectiveLabel.paragraphSpacing=6;objectiveLabel.color=new Color(.19f,.12f,.06f);objectiveLabel.richText=true;objectiveLabel.textWrappingMode=TextWrappingModes.Normal;
        var readableFont=Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");if(readableFont!=null){objectiveLabel.font=readableFont;objectiveLabel.fontSharedMaterial=readableFont.material;}objectiveLabel.fontStyle=FontStyles.Normal;objectiveLabel.alignment=TextAlignmentOptions.TopLeft;
        if(objectiveScroll!=null)
        {
            Top((RectTransform)objectiveScroll.transform,18,98,484,318);
            objectiveScroll.horizontal=false;objectiveScroll.vertical=true;objectiveScroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;
            var fitter=objectiveLabel.GetComponent<UnityEngine.UI.ContentSizeFitter>();if(fitter==null)fitter=objectiveLabel.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();fitter.horizontalFit=UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;fitter.verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            objectiveScroll.content=objectiveLabel.rectTransform;
            objectiveLabel.rectTransform.anchorMin=new Vector2(0,1);objectiveLabel.rectTransform.anchorMax=Vector2.one;objectiveLabel.rectTransform.pivot=new Vector2(.5f,1);objectiveLabel.rectTransform.anchoredPosition=Vector2.zero;objectiveLabel.rectTransform.sizeDelta=new Vector2(-20,0);
        }
        var existing=panel.Find("Return to quest NPC");
        if(existing==null)
        {
            var go=new GameObject("Return to quest NPC",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));go.transform.SetParent(panel,false);Top(go.GetComponent<RectTransform>(),18,428,484,40);
            var image=go.GetComponent<UnityEngine.UI.Image>();image.color=new Color(.25f,.16f,.08f);
            var text=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));text.transform.SetParent(go.transform,false);var label=text.GetComponent<TextMeshProUGUI>();label.font=objectiveLabel.font;label.text="Return to quest NPC";label.fontSize=24;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;label.color=new Color(1,.9f,.65f);var rt=label.rectTransform;rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;
            existing=go.transform;
        }
        var button=existing.GetComponent<UnityEngine.UI.Button>();button.onClick.RemoveListener(OpenReturnPanel);button.onClick.AddListener(OpenReturnPanel);
        FitToCanvas();
    }
    void FitToCanvas()
    {
        var canvas=GetComponent<Canvas>();if(canvas==null||panel==null||slideRoot==null)return;
        float pixelHeight=canvas.renderMode==RenderMode.ScreenSpaceCamera&&canvas.worldCamera!=null?canvas.worldCamera.pixelHeight:canvas.pixelRect.height;
        float canvasHeight=pixelHeight/Mathf.Max(.01f,canvas.scaleFactor);
        float height=Mathf.Min(480,Mathf.Max(240,canvasHeight-160));
        panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
        slideRoot.anchoredPosition=new Vector2(slideRoot.anchoredPosition.x,-Mathf.Min(264,Mathf.Max(96,canvasHeight-height-24)));
        if(objectiveScroll!=null)Top((RectTransform)objectiveScroll.transform,18,98,484,height-162);
        var action=panel.Find("Return to quest NPC") as RectTransform;if(action!=null)Top(action,18,height-52,484,40);
    }
    void OpenReturnPanel(){GetComponent<QuestReturnPanel>()?.OpenManually();}
    static void Top(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);r.localScale=Vector3.one;}

    private void FindTracker()
    {
        if (tracker == null) tracker = FindFirstObjectByType<ElfQuestTracker>();
    }

    public static bool OccludesWorldRect(Rect screenRect)
    {
        foreach(var hud in FindObjectsByType<QuestJournalHud>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            var visibility=hud.GetComponent<CanvasGroup>();
            if(!hud.isActiveAndEnabled||visibility!=null&&visibility.alpha<.05f||hud.panel==null||!hud.panel.gameObject.activeInHierarchy)continue;
            var panelGroup=hud.panel.GetComponent<CanvasGroup>();if(panelGroup!=null&&panelGroup.alpha<.05f)continue;
            var canvas=hud.GetComponent<Canvas>();var camera=canvas!=null&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;
            var corners=new Vector3[4];hud.panel.GetWorldCorners(corners);
            var first=RectTransformUtility.WorldToScreenPoint(camera,corners[0]);var last=RectTransformUtility.WorldToScreenPoint(camera,corners[2]);
            var occupied=Rect.MinMaxRect(Mathf.Min(first.x,last.x),Screen.height-Mathf.Max(first.y,last.y),Mathf.Max(first.x,last.x),Screen.height-Mathf.Min(first.y,last.y));
            if(occupied.Overlaps(screenRect))return true;
        }
        return false;
    }

    private bool IsMinimized => tracker != null ? ElfQuestTracker.IsMinimized : fallbackMinimized;

    private void ApplyPanelVisibility()
    {
        if (panel == null) return;
        if (slideRoot != null)
        {
            // Existing authored drawers keep their slide and arrow animation.
            // Fixed-icon prefabs leave slideRoot unassigned.
            if (!panel.gameObject.activeSelf) panel.gameObject.SetActive(true);
            ApplySlide();
            return;
        }
        bool open = !IsMinimized;
        if (panel.gameObject.activeSelf != open) panel.gameObject.SetActive(open);
    }

    private void ApplySlide()
    {
        if (slideRoot == null || panel == null) return;
        panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(1,1);
        panel.anchoredPosition=new Vector2(Mathf.Lerp(panel.rect.width+24,-112,Mathf.SmoothStep(0,1,reveal)),0);
        if(panelVisibility!=null){panelVisibility.alpha=reveal;panelVisibility.interactable=panelVisibility.blocksRaycasts=reveal>.95f;}
        if (arrowArtwork != null)
            arrowArtwork.localScale = new Vector3(arrowScale.x * (IsMinimized ? 1f : -1f), arrowScale.y, arrowScale.z);
    }

    void OnDestroy()
    {
        if (arrowButton != null) arrowButton.onClick.RemoveListener(ToggleJournal);
    }
}
