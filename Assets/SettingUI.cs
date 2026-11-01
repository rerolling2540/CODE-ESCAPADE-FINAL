using UnityEngine;
using UnityEngine.SceneManagement;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;

    [Header("Game Save")]
    [SerializeField] private Transform player;

    public void OpenSettings()
    {
        foreach (var hud in FindObjectsByType<ExplorationHUD>(FindObjectsSortMode.None)) hud.CloseInventoryFromButton();
        GameControlsGuide.EnsureSettings(settingsPanel);
        settingsPanel.SetActive(true);
        Time.timeScale = 0f;
        SettingsUiVisibility.RefreshNow();
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        SettingsUiVisibility.RefreshNow();
        Time.timeScale = 1f;
    }

    public void OpenMainMenu()
    {
        // Save current player position
        var save = GameSave.EnsureInstance();
        if (player != null) save.SaveGame(player);
        else save.SaveCurrentPlayer();

        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
/// <summary>One controls reference shared by the main menu and in-game settings.</summary>
public static class GameControlsGuide
{
    private static readonly System.Collections.Generic.List<GameObject> settingsPanels = new System.Collections.Generic.List<GameObject>();
    public static bool SettingsOpen
    {
        get
        {
            settingsPanels.RemoveAll(panel => panel == null);
            return settingsPanels.Exists(panel => panel.activeInHierarchy && panel.scene == SceneManager.GetActiveScene());
        }
    }
    public static bool IsSettingsBranch(Transform branch) => settingsPanels.Exists(panel =>
        panel != null && panel.activeInHierarchy && panel.scene == SceneManager.GetActiveScene() &&
        (panel.transform == branch || panel.transform.IsChildOf(branch)));
    public static bool IsSettingsContent(Transform branch) => settingsPanels.Exists(panel =>
        panel != null && panel.activeInHierarchy && panel.scene == SceneManager.GetActiveScene() &&
        (panel.transform == branch || branch.IsChildOf(panel.transform)));
    public static void RegisterSettings(GameObject panel)
    {
        if (panel == null) return;
        if (!settingsPanels.Contains(panel)) settingsPanels.Add(panel);
        if (Application.isPlaying) SettingsUiVisibility.EnsureInstance();
    }
    public static bool GameplayOverlayHidden => PlayerDeathFlow.IsActive || SettingsOpen || CharacterAlmanac.IsOpen || CardRefinementUI.IsOpen || SyntaxMerchantUI.IsOpen || BattleTutorialGuide.BlocksInput;
    private static readonly System.Collections.Generic.List<GameObject> mapBlockingPanels = new System.Collections.Generic.List<GameObject>();
    public static void RegisterMapBlocker(GameObject panel)
    {
        if(panel!=null&&!mapBlockingPanels.Contains(panel))mapBlockingPanels.Add(panel);
    }
    public static bool MapHidden
    {
        get
        {
            mapBlockingPanels.RemoveAll(panel=>panel==null);
            return GameplayOverlayHidden || Time.timeScale<=0f || TutorialSignboard.IsOpen || MovementTutorial.AnyOpen ||
                (DialogueManager.Instance!=null&&DialogueManager.Instance.isDialogueActive) ||
                mapBlockingPanels.Exists(panel=>panel.activeInHierarchy&&panel.scene==SceneManager.GetActiveScene());
        }
    }
    private static GUIStyle guideWoodStyle, guidePaperStyle, guideHeadingStyle, guideBodyStyle, guideButtonStyle, guideCaptionStyle;
    public static bool DrawTravellerGuide(string heading,string instructions,float alpha=1f)
    {
        if(guideWoodStyle==null)
        {
            guideWoodStyle=new GUIStyle(GUI.skin.box){border=new RectOffset(10,10,10,10)};
            guideWoodStyle.normal.background=Frame(false).texture;
            guidePaperStyle=new GUIStyle(guideWoodStyle);guidePaperStyle.normal.background=Frame(true).texture;
            guideHeadingStyle=new GUIStyle(GUI.skin.label){fontSize=28,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,wordWrap=true};
            guideHeadingStyle.normal.textColor=new Color(1,.86f,.54f); UiTextColors.Stable(guideHeadingStyle);
            guideCaptionStyle=new GUIStyle(guideHeadingStyle){fontSize=14};
            guideBodyStyle=new GUIStyle(GUI.skin.label){fontSize=22,wordWrap=true,padding=new RectOffset()};guideBodyStyle.normal.textColor=Ink; UiTextColors.Stable(guideBodyStyle);
            guideButtonStyle=new GUIStyle(GUI.skin.button){fontSize=21,fontStyle=FontStyle.Bold,border=new RectOffset(10,10,10,10)};
            guideButtonStyle.normal.background=guideButtonStyle.hover.background=guideButtonStyle.active.background=Frame(false).texture;
            guideButtonStyle.normal.textColor=new Color(1,.86f,.54f); UiTextColors.Stable(guideButtonStyle);guideButtonStyle.hover.textColor = guideButtonStyle.normal.textColor;
        }
        Color old=GUI.color;
        try
        {
            GUI.color=new Color(0,0,0,.65f*alpha);GUI.DrawTexture(new Rect(0,0,1280,720),Texture2D.whiteTexture);
            GUI.color=new Color(1,1,1,alpha);
            GUI.Box(new Rect(220,120,840,470),GUIContent.none,guideWoodStyle);
            GUI.Label(new Rect(250,138,780,24),"THE TRAVELLER'S FIELD MANUAL",guideCaptionStyle);
            GUI.Label(new Rect(250,164,780,45),heading,guideHeadingStyle);
            GUI.Box(new Rect(240,218,800,278),GUIContent.none,guidePaperStyle);
            GUI.Label(new Rect(264,240,752,232),instructions,guideBodyStyle);
            return GUI.Button(new Rect(460,520,360,46),"Back to game  [E / Esc]",guideButtonStyle);
        }
        finally{GUI.color=old;}
    }
    private static Sprite paperFrame, woodFrame;
    private static readonly Color Ink = new Color(0.19f, 0.095f, 0.035f);

    public static Sprite Frame(bool paper)
    {
        var cached = paper ? paperFrame : woodFrame;
        if (cached != null) return cached;
        const int size = 64;
        var texture = new Texture2D(size,size,TextureFormat.RGBA32,false) { filterMode=FilterMode.Point, wrapMode=TextureWrapMode.Clamp, name=paper?"Handbook Parchment":"Handbook Carved Wood" };
        var pixels = new Color[size*size];
        for(int y=0;y<size;y++) for(int x=0;x<size;x++)
        {
            int edge=Mathf.Min(Mathf.Min(x,y),Mathf.Min(size-1-x,size-1-y));
            float grain=((x*17+y*31)%11-5)*0.002f;
            Color color;
            if(edge<2) color=new Color(0.14f,0.075f,0.035f);
            else if(edge<4) color=new Color(0.75f,0.55f,0.26f);
            else if(edge<7) color=new Color(0.33f,0.19f,0.085f);
            else color=paper?new Color(0.89f+grain,0.81f+grain,0.64f+grain):new Color(0.25f+grain,0.135f+grain,0.065f+grain);
            pixels[y*size+x]=color;
        }
        texture.SetPixels(pixels);texture.Apply(false,true);
        var sprite=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(0.5f,0.5f),100,0,SpriteMeshType.FullRect,new Vector4(10,10,10,10));
        if(paper)paperFrame=sprite;else woodFrame=sprite;
        return sprite;
    }

    static void StyleScroll(UnityEngine.UI.ScrollRect scroll)
    {
        var image=scroll.viewport.GetComponent<UnityEngine.UI.Image>();
        image.sprite=Frame(true);image.type=UnityEngine.UI.Image.Type.Sliced;image.color=Color.white;
        scroll.viewport.GetComponent<UnityEngine.UI.Mask>().showMaskGraphic=true;
        var text=scroll.content.GetComponent<TMPro.TMP_Text>();
        text.color=Ink;text.text=Text;text.fontSize=23;text.lineSpacing=5;
        text.margin=new Vector4(24,20,28,24);
        if(text.font!=null)text.fontSharedMaterial=text.font.material;
    }
    public const string Text =
        "<b><color=#703513>TRAVEL</color></b>\n" +
        "<b>A / D or Left / Right</b>  Move\n" +
        "<b>W / Up Arrow / Space</b>  Jump; press again in the air to double-jump\n" +
        "<b>Left Shift</b>  Dash\n\n" +
        "<b><color=#703513>EXPLORE & INTERACT</color></b>\n" +
        "<b>E</b>  Talk to NPCs, read signs, open nearby chests, and enter available portals\n" +
        "<b>E / Space / Enter</b>  Reveal or advance dialogue\n" +
        "<b>Left click</b>  Choose dialogue options and use on-screen buttons\n" +
        "<b>E / Esc</b>  Close a tutorial sign; <b>Esc</b> closes dialogue\n" +
        "Touch an enemy to begin battle.\n\n" +
        "<b><color=#703513>JOURNALS & MENUS</color></b>\n" +
        "<b>M</b>  Show / hide the minimap\n" +
        "<b>Q or click the Quest Journal arrow</b>  Slide quests open / closed; scroll long objectives\n" +
        "<b>I</b>  Open / close inventory; <b>Esc</b> also closes inventory\n" +
        "<b>L or click Almanac</b>  Open / close the learning almanac during exploration\n" +
        "Click the settings icon to open Settings; use Resume to return\n\n" +
        "<b><color=#703513>BATTLE</color></b>\n" +
        "Hover over a card to inspect it. Drag a targeted card onto an enemy to play it.\n" +
        "Answer the programming question using the answer buttons or input field.\n" +
        "Use <b>End Turn</b> to finish your turn. Use <b>Retreat</b> to leave a battle.\n\n" +
        "In the training battle, click <b>Review guide</b> to reread the manual.\n" +
        "Use <b>Back / Left Arrow</b> and <b>Forward / Enter</b> to navigate guide pages.\n\n" +
        "<b><color=#703513>SAVING</color></b>\n" +
        "Choose Play in the main menu to create or continue one of three save slots.";

    public static void FillHelp(GameObject panel)
    {
        if (panel == null) return;
        foreach (var label in panel.GetComponentsInChildren<TMPro.TMP_Text>(true))
            if (label.name == "Controls") { MakeScrollable(label); return; }
    }

    static void MakeScrollable(TMPro.TMP_Text original)
    {
        var existing = original.transform.Find("Guide Viewport");
        if (existing != null)
        {
            var label = existing.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (label != null) label.text = Text;
            StyleScroll(original.GetComponent<UnityEngine.UI.ScrollRect>());
            return;
        }
        original.enabled = false;
        var root = original.gameObject;
        var scroll = root.AddComponent<UnityEngine.UI.ScrollRect>();
        var viewport = new GameObject("Guide Viewport", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Mask));
        viewport.transform.SetParent(root.transform, false);
        var viewRect = (RectTransform)viewport.transform;
        viewRect.anchorMin = Vector2.zero; viewRect.anchorMax = Vector2.one;
        viewRect.offsetMin = Vector2.zero; viewRect.offsetMax = Vector2.zero;
        viewport.GetComponent<UnityEngine.UI.Image>().color = new Color(1,1,1,0.01f);
        viewport.GetComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
        var content = new GameObject("Complete Controls", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI), typeof(UnityEngine.UI.ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var rect = (RectTransform)content.transform;
        rect.anchorMin = new Vector2(0,1); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(0.5f,1);
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        var text = content.GetComponent<TMPro.TextMeshProUGUI>();
        text.font = original.font;
        if (original.fontSharedMaterial != null) text.fontSharedMaterial = original.fontSharedMaterial;
        text.fontSize = 23; text.color = new Color(0.97f,0.94f,0.84f); text.text = Text;
        text.textWrappingMode = TMPro.TextWrappingModes.Normal;
        text.alignment = TMPro.TextAlignmentOptions.TopLeft;
        text.margin = new Vector4(8,8,18,12);
        content.GetComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewRect; scroll.content = rect;
        scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 32;
        scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
        StyleScroll(scroll);
    }

    public static void EnsureSettings(GameObject panel)
    {
        if (panel == null) return;
        if (Application.isPlaying)
            foreach (var hud in Object.FindObjectsByType<ExplorationHUD>(FindObjectsSortMode.None)) hud.CloseInventoryFromButton();
        RegisterSettings(panel);
        TryCloseSettingsGuide(panel);
        if(panel.scene.name=="MainMenu")
        {
            foreach(string name in new[]{"Controls Guide Button","Tutorial Tips Button","Settings Controls Guide","Settings Tutorial Tips"})
            {
                var extra=panel.transform.Find(name);
                if(extra!=null)extra.gameObject.SetActive(false);
            }
            return;
        }
        if (panel.transform.Find("Controls Guide Button") != null)
        {
            StyleSettingsActions(panel);
            return;
        }
        var template = panel.GetComponentInChildren<TMPro.TMP_Text>(true);
        var lobby = panel.transform.Find("Lobby") as RectTransform;
        UnityEngine.UI.Button button;
        if (lobby != null && lobby.GetComponent<UnityEngine.UI.Button>() != null)
        {
            var copy = Object.Instantiate(lobby.gameObject, panel.transform);
            copy.name = "Controls Guide Button";
            copy.SetActive(true);
            button = copy.GetComponent<UnityEngine.UI.Button>();
            // Keep the Lobby artwork and typography, but never copy its navigation action.
            button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            var caption = copy.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (caption != null)
            {
                caption.text="CONTROLS";caption.enabled=true;caption.gameObject.SetActive(true);
                caption.color=new Color(1,.88f,.61f);caption.enableAutoSizing=true;caption.fontSizeMin=16;caption.fontSizeMax=26;
                var labelRect=(RectTransform)caption.transform;
                labelRect.anchorMin=Vector2.zero;labelRect.anchorMax=Vector2.one;labelRect.offsetMin=new Vector2(8,4);labelRect.offsetMax=new Vector2(-8,-4);
            }
        }
        else button = Button(panel.transform, "Controls Guide Button", "CONTROLS", template);
        var rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = Vector2.zero; rect.anchoredPosition = new Vector2(150,38); rect.sizeDelta = new Vector2(240,44);
        // Keep both actions together on the existing Lobby row.
        if (lobby != null)
        {
            float width = Mathf.Min(220f, lobby.rect.width);
            float gap = 20f;
            float y = lobby.anchoredPosition.y;
            lobby.anchorMin = lobby.anchorMax = new Vector2(0.5f,0.5f);
            lobby.pivot = new Vector2(0.5f,0.5f);
            lobby.sizeDelta = new Vector2(width,lobby.rect.height);
            lobby.anchoredPosition = new Vector2(-(width + gap) / 2f,y);
            rect.anchorMin = rect.anchorMax = lobby.anchorMin;
            rect.pivot = lobby.pivot;
            rect.sizeDelta = lobby.sizeDelta;
            rect.anchoredPosition = new Vector2((width + gap) / 2f,y);
        }
        GameObject guide = null;
        button.onClick.AddListener(() => {
            if (guide == null) guide = BuildPanel(panel.transform, template);
            guide.SetActive(true); guide.transform.SetAsLastSibling();
        });
        if (lobby != null && (CharacterAlmanac.IsExplorationScene(panel.scene.name) || panel.scene.name == "Battle Scene"))
        {
            var saveObject = Object.Instantiate(lobby.gameObject, panel.transform);
            saveObject.name = "Save Game Button";
            var saveButton = saveObject.GetComponent<UnityEngine.UI.Button>();
            saveButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            var label = saveObject.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (label != null) label.text = "SAVE GAME";
            var saveRect = (RectTransform)saveObject.transform;
            float width = Mathf.Min(200f, lobby.rect.width);
            foreach (var action in new[] { lobby, saveRect, rect }) action.sizeDelta = new Vector2(width,lobby.rect.height);
            lobby.anchoredPosition = new Vector2(-(width + 16f),lobby.anchoredPosition.y);
            saveRect.anchoredPosition = new Vector2(0,lobby.anchoredPosition.y);
            rect.anchoredPosition = new Vector2(width + 16f,lobby.anchoredPosition.y);
            var feedback = saveObject.AddComponent<SettingsSaveButton>();
            feedback.Configure(saveButton,label);
        }
        var tips=Button(panel.transform,"Tutorial Tips Button","TUTORIAL TIPS",template);
        if(lobby!=null)
        {
            var image=tips.GetComponent<UnityEngine.UI.Image>();
            var original=lobby.GetComponent<UnityEngine.UI.Image>();
            if(original!=null){image.sprite=original.sprite;image.type=original.type;}
            var caption=tips.GetComponentInChildren<TMPro.TMP_Text>();
            var source=lobby.GetComponentInChildren<TMPro.TMP_Text>();
            if(source!=null){caption.font=source.font;caption.color=source.color;}
        }
        GameObject tipsPanel=null;
        tips.onClick.AddListener(()=>{
            if(tipsPanel==null)tipsPanel=BuildPanel(panel.transform,template,true);
            tipsPanel.SetActive(true);tipsPanel.transform.SetAsLastSibling();
        });
        var actions=new System.Collections.Generic.List<RectTransform>();
        bool mainMenu=panel.scene.name=="MainMenu";
        if(mainMenu)
        {
            var note=panel.transform.Find("Settings Note") as RectTransform;
            if(note!=null){note.anchorMin=note.anchorMax=note.pivot=new Vector2(.5f,.5f);note.anchoredPosition=new Vector2(0,-110);note.sizeDelta=new Vector2(580,18);}
        }
        if(lobby!=null&&!mainMenu)actions.Add(lobby);
        var saveAction=panel.transform.Find("Save Game Button") as RectTransform;
        if(saveAction!=null)actions.Add(saveAction);
        actions.Add(rect);actions.Add((RectTransform)tips.transform);
        float baseY=mainMenu?-142:lobby!=null?lobby.anchoredPosition.y:-182;
        float actionWidth=Mathf.Min(230f,((RectTransform)panel.transform).rect.width*.36f);
        for(int i=0;i<actions.Count;i++)
        {
            var action=actions[i];action.anchorMin=action.anchorMax=action.pivot=new Vector2(.5f,.5f);
            action.sizeDelta=new Vector2(actionWidth,mainMenu?46:60);
            action.anchoredPosition=new Vector2((i%2==0?-1:1)*(actionWidth+20)/2,baseY+(i/2)*74);
        }
        StyleSettingsActions(panel);
    }

    public static void StyleSettingsActions(GameObject panel)
    {
        if (panel == null) return;
        var lobby = panel.transform.Find("Lobby");
        var source = lobby != null ? lobby.GetComponentInChildren<TMPro.TMP_Text>(true) : null;
        if (source == null) return;
        foreach (string name in new[] { "Lobby", "Controls Guide Button", "Tutorial Tips Button", "Save Game Button" })
        {
            var action = panel.transform.Find(name);
            if (action == null) continue;
            var label = action.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (label == null) continue;
            label.font = source.font;
            label.fontSharedMaterial = source.fontSharedMaterial;
            label.fontStyle = TMPro.FontStyles.Bold;
            label.fontSize = 22;
            label.enableAutoSizing = false;
            label.color = new Color(1f, .88f, .61f);
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.raycastTarget = false;
            var rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8, 4);
            rect.offsetMax = new Vector2(-8, -4);
        }
    }

    public static bool TryCloseSettingsGuide(GameObject panel)
    {
        if(panel==null)return false;
        foreach(string name in new[]{"Settings Tutorial Tips","Settings Controls Guide"})
        {
            var guide=panel.transform.Find(name);
            if(guide!=null&&guide.gameObject.activeSelf){guide.gameObject.SetActive(false);return true;}
        }
        return false;
    }

    static UnityEngine.UI.Button Button(Transform parent, string name, string caption, TMPro.TMP_Text template)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        go.transform.SetParent(parent,false);
        var image = go.GetComponent<UnityEngine.UI.Image>(); image.sprite=Frame(false);image.type=UnityEngine.UI.Image.Type.Sliced;image.color=Color.white;
        var outline = go.AddComponent<UnityEngine.UI.Outline>(); outline.effectColor = new Color(0.72f,0.52f,0.23f); outline.effectDistance = new Vector2(2,-2);
        var label = new GameObject("Label",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI)); label.transform.SetParent(go.transform,false);
        var rect = (RectTransform)label.transform; rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(8,4);rect.offsetMax=new Vector2(-8,-4);
        var text=label.GetComponent<TMPro.TextMeshProUGUI>();text.text=caption;if(template!=null)text.font=template.font;
        text.fontSize=22;text.enableAutoSizing=true;text.fontSizeMin=14;text.fontSizeMax=22;text.alignment=TMPro.TextAlignmentOptions.Center;text.color=new Color(1,0.88f,0.61f);text.raycastTarget=false;
        var button=go.GetComponent<UnityEngine.UI.Button>();button.targetGraphic=image;
        var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1f,0.92f,0.72f);colors.pressedColor=new Color(0.72f,0.62f,0.43f);colors.selectedColor=colors.highlightedColor;button.colors=colors;
        return button;
    }

    static GameObject BuildPanel(Transform parent, TMPro.TMP_Text template, bool tutorialTips=false)
    {
        var go=new GameObject("Settings Controls Guide",typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(parent,false);
        var rect=(RectTransform)go.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(8,8);rect.offsetMax=new Vector2(-8,-8);
        var frame=go.GetComponent<UnityEngine.UI.Image>();frame.sprite=Frame(false);frame.type=UnityEngine.UI.Image.Type.Sliced;frame.color=Color.white;
        var heading=new GameObject("Handbook Title",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));heading.transform.SetParent(go.transform,false);
        var headingRect=(RectTransform)heading.transform;headingRect.anchorMin=new Vector2(0,1);headingRect.anchorMax=Vector2.one;headingRect.pivot=new Vector2(0.5f,1);headingRect.offsetMin=new Vector2(26,-68);headingRect.offsetMax=new Vector2(-170,-16);
        var headingText=heading.GetComponent<TMPro.TextMeshProUGUI>();if(template!=null)headingText.font=template.font;headingText.text="ADVENTURER'S HANDBOOK";headingText.color=new Color(1,0.86f,0.54f);headingText.fontStyle=TMPro.FontStyles.Bold;headingText.enableAutoSizing=true;headingText.fontSizeMin=16;headingText.fontSizeMax=26;headingText.alignment=TMPro.TextAlignmentOptions.MidlineLeft;headingText.raycastTarget=false;
        var title=new GameObject("Controls",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));title.transform.SetParent(go.transform,false);
        var text=title.GetComponent<TMPro.TextMeshProUGUI>();if(template!=null)text.font=template.font;
        var body=(RectTransform)title.transform;body.anchorMin=Vector2.zero;body.anchorMax=Vector2.one;body.offsetMin=new Vector2(20,44);body.offsetMax=new Vector2(-20,-80);
        MakeScrollable(text);
        if(tutorialTips)
        {
            go.name="Settings Tutorial Tips";
            headingText.text="THE TRAVELLER'S TIPS";
            var content=text.GetComponent<UnityEngine.UI.ScrollRect>().content.GetComponent<TMPro.TMP_Text>();
            content.text=FirstEncounterGuide.ReferenceText;
        }
        var hint=new GameObject("Scroll Hint",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));hint.transform.SetParent(go.transform,false);
        var hintRect=(RectTransform)hint.transform;hintRect.anchorMin=Vector2.zero;hintRect.anchorMax=new Vector2(1,0);hintRect.pivot=new Vector2(0.5f,0);hintRect.offsetMin=new Vector2(20,12);hintRect.offsetMax=new Vector2(-20,36);
        var hintText=hint.GetComponent<TMPro.TextMeshProUGUI>();if(template!=null)hintText.font=template.font;hintText.text="Scroll to read the complete guide";hintText.fontSize=16;hintText.color=new Color(0.93f,0.80f,0.55f);hintText.alignment=TMPro.TextAlignmentOptions.Center;hintText.raycastTarget=false;
        var back=Button(go.transform,"Back to Settings","BACK",template);var backRect=(RectTransform)back.transform;backRect.anchorMin=backRect.anchorMax=Vector2.one;backRect.anchoredPosition=new Vector2(-85,-36);backRect.sizeDelta=new Vector2(130,42);back.onClick.AddListener(()=>go.SetActive(false));
        return go;
    }
}
