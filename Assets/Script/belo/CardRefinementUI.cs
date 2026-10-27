using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CardRefinementUI : MonoBehaviour
{
    private static CardRefinementUI instance;
    public static bool IsOpen => instance != null && instance.opened;
    private CharacterInventory inventory;
    private BlacksmithNPC smith;
    private GameObject overlay, listContent;
    private TMP_Text coins, selectedName, currentEffects, nextEffects, price, message, actionCaption;
    private Image cardArt;
    private Button action, cancel;
    private string selectedId;
    private bool opened, confirming;
    private int openedFrame, slot;
    private float previousTimeScale;
    private TMP_FontAsset font;
    private Material textMaterial;
    private static readonly Color Gold = new Color(1f,.86f,.54f);
    private static readonly Color Ink = new Color(.17f,.085f,.03f);

    public static void Show(CharacterInventory cards, BlacksmithNPC owner)
    {
        if (cards == null || IsOpen || GameControlsGuide.SettingsOpen || CharacterAlmanac.IsOpen) return;
        if (instance == null) instance = new GameObject("Blacksmith Forge UI").AddComponent<CardRefinementUI>();
        instance.inventory = cards; instance.smith = owner; instance.slot = SaveSlots.Active;
        instance.previousTimeScale = Time.timeScale;
        instance.opened = true; instance.openedFrame = Time.frameCount;
        instance.confirming = false; instance.selectedId = null;
        Time.timeScale = 0f;
        instance.overlay.SetActive(true);
        instance.Refresh();
    }

    void Awake() { instance = this; BuildUI(); }
    void Update()
    {
        if (!opened) return;
        if (inventory == null || smith == null || !smith.isActiveAndEnabled || SaveSlots.Active != slot) { Close(); return; }
        if (Time.frameCount == openedFrame) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { if (confirming) CancelConfirmation(); else Close(); }
    }
    public void Close()
    {
        if (!opened) return;
        opened = false; confirming = false;
        Time.timeScale = previousTimeScale;
        if (overlay != null) overlay.SetActive(false);
    }
    void OnDisable() { Close(); }
    void OnDestroy()
    {
        Close();
        if (instance == this) instance = null;
        if (textMaterial != null) { if (Application.isPlaying) Destroy(textMaterial); else DestroyImmediate(textMaterial); }
    }
    void BackToSmith()
    {
        var host = smith != null ? smith.GetComponent<Npc_Talk>() : null;
        Close();
        if (host != null && host.isActiveAndEnabled) host.BeginConversation();
    }

    void Refresh()
    {
        coins.text = "Your purse: " + inventory.Coins + " coins";
        for (int i=listContent.transform.childCount-1;i>=0;i--)
        {
            var child=listContent.transform.GetChild(i).gameObject; child.SetActive(false);
            if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
        }
        if (selectedId == null || inventory.GetQuantity(selectedId) == 0)
            selectedId = inventory.Cards.Where(s => inventory.GetCard(s.id) != null).OrderBy(s => inventory.GetCard(s.id).name).FirstOrDefault()?.id;
        foreach (var stack in inventory.Cards.OrderBy(s => inventory.GetCard(s.id)?.name))
        {
            var card = inventory.GetCard(stack.id);
            if (card == null) continue;
            string id = stack.id;
            var button = MakeButton("Refine " + id, listContent.transform,
                card.name + "\nLevel " + inventory.GetLevel(id) + "/5  ·  Owned: " + stack.quantity, () => Select(id));
            button.gameObject.AddComponent<LayoutElement>().preferredHeight = 82;
            if (id == selectedId) button.GetComponent<Image>().color = new Color(1f,.82f,.45f);
        }
        RenderSelection();
    }
    void Select(string id) { selectedId = id; confirming = false; Refresh(); }
    void CancelConfirmation() { confirming = false; RenderSelection(); }
    void RenderSelection(string result = null)
    {
        var card = inventory.GetCard(selectedId);
        if (card == null)
        {
            selectedName.text = "Your collection is empty"; currentEffects.text = "Collect cards from chests and return to the forge.";
            nextEffects.text = price.text = ""; message.text = "No card selected."; cardArt.enabled = false;
            action.interactable = cancel.interactable = false; return;
        }
        int level = inventory.GetLevel(selectedId);
        bool max = level >= CardRefinementRules.MaxLevel;
        selectedName.text = card.name;
        cardArt.sprite = card.Image; cardArt.enabled = card.Image != null;
        currentEffects.text = "<b>NOW  ·  LEVEL " + level + "</b>\n\n" + CardRefinementRules.Describe(card, level);
        nextEffects.text = max ? "<b>MASTERWORK</b>\n\nMaximum level reached." : "<b>AFTER  ·  LEVEL " + (level+1) + "</b>\n\n" + CardRefinementRules.Describe(card, level+1);
        int cost = CardRefinementRules.DuplicateCost(level);
        price.text = max ? "No further refinement is needed." :
            "Cost: " + cost + " spare " + (cost == 1 ? "copy" : "copies") + " + " + CardRefinementRules.CoinCost(level) + " coins\n" +
            "Owned: " + inventory.GetQuantity(selectedId) + "  ·  Spare copies: " + Mathf.Max(0,inventory.GetQuantity(selectedId)-1);
        bool ready = inventory.CanRefine(selectedId, out string reason);
        action.interactable = ready;
        cancel.interactable = confirming;
        actionCaption.text = confirming ? "Confirm refinement" : max ? "Maximum level" : "Refine selected card";
        message.text = result ?? (confirming ? "Confirm spending the copies and coins shown above? This cannot be undone." : reason);
        message.color = result != null || ready ? Ink : new Color(.52f,.13f,.07f);
    }
    void Refine()
    {
        if (!opened || inventory == null || SaveSlots.Active != slot) return;
        if (!inventory.CanRefine(selectedId, out string reason)) { confirming = false; RenderSelection(reason); return; }
        if (!confirming) { confirming = true; RenderSelection(); return; }
        confirming = false;
        inventory.TryRefine(selectedId, out string result);
        Refresh(); RenderSelection(result);
    }

    void BuildUI()
    {
        font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF") ?? TMP_Settings.defaultFontAsset;
        if (font != null)
        {
            textMaterial = new Material(font.material) { name = "Forge Clear Text" };
            textMaterial.DisableKeyword("UNDERLAY_ON"); textMaterial.DisableKeyword("UNDERLAY_INNER");
            textMaterial.SetFloat("_OutlineWidth",0); textMaterial.SetFloat("_OutlineSoftness",0); textMaterial.SetFloat("_FaceDilate",0);
        }
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=350;
        gameObject.AddComponent<GraphicRaycaster>();var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        overlay=Panel("Forge Overlay",transform,new Color(.025f,.015f,.01f,.88f));Stretch(overlay,0,0,0,0);
        var frame=Panel("The Blacksmith's Forge",overlay.transform,Color.white);frame.GetComponent<Image>().sprite=GameControlsGuide.Frame(false);frame.GetComponent<Image>().type=Image.Type.Sliced;Stretch(frame,40,25,40,25);
        var heading=Label("Forge Title",frame.transform,"THE BLACKSMITH'S FORGE",29,Gold);Position(heading.gameObject,26,-18,800,44);
        var close=MakeButton("Close Forge",frame.transform,"Close [Esc]",Close);Position(close.gameObject,984,-20,190,42);
        var back=MakeButton("Back To Blacksmith",frame.transform,"< Back to blacksmith",BackToSmith);Position(back.gameObject,26,-76,255,40);
        coins=Label("Coin Balance",frame.transform,"",22,Gold);Position(coins.gameObject,760,-76,414,40);coins.alignment=TextAlignmentOptions.MidlineRight;
        var list=CreateList(frame.transform);Stretch(list.gameObject,26,136,780,66);listContent=list.content.gameObject;
        var body=Panel("Refinement Details",frame.transform,Color.white);body.GetComponent<Image>().sprite=GameControlsGuide.Frame(true);body.GetComponent<Image>().type=Image.Type.Sliced;Stretch(body,444,136,26,66);
        selectedName=Label("Selected Card",body.transform,"",27,Ink);Position(selectedName.gameObject,22,-18,570,62);selectedName.fontStyle=FontStyles.Bold;
        var art=Panel("Card Art",body.transform,Color.white);cardArt=art.GetComponent<Image>();cardArt.preserveAspect=true;cardArt.raycastTarget=false;Position(art,628,-16,80,80);
        currentEffects=Label("Current Effects",body.transform,"",21,Ink);Position(currentEffects.gameObject,22,-112,320,142);
        nextEffects=Label("Refined Effects",body.transform,"",21,Ink);Position(nextEffects.gameObject,370,-112,320,142);
        price=Label("Refinement Price",body.transform,"",22,Ink);Position(price.gameObject,22,-266,666,72);
        action=MakeButton("Refine Card",body.transform,"Refine selected card",Refine);var ar=(RectTransform)action.transform;ar.anchorMin=Vector2.zero;ar.anchorMax=new Vector2(1,0);ar.offsetMin=new Vector2(22,74);ar.offsetMax=new Vector2(-154,122);actionCaption=action.GetComponentInChildren<TMP_Text>();
        cancel=MakeButton("Cancel Refinement",body.transform,"Cancel",CancelConfirmation);var cr=(RectTransform)cancel.transform;cr.anchorMin=cr.anchorMax=new Vector2(1,0);cr.pivot=new Vector2(1,0);cr.anchoredPosition=new Vector2(-22,74);cr.sizeDelta=new Vector2(118,48);
        message=Label("Forge Message",body.transform,"",18,Ink);var mr=(RectTransform)message.transform;mr.anchorMin=Vector2.zero;mr.anchorMax=new Vector2(1,0);mr.offsetMin=new Vector2(22,12);mr.offsetMax=new Vector2(-22,64);
        var footer=Label("Forge Rules",frame.transform,"All remaining copies share the upgrade. Consumed copies leave your collection and battle deck. Maximum level: 5.",17,Gold);var fr=(RectTransform)footer.transform;fr.anchorMin=Vector2.zero;fr.anchorMax=new Vector2(1,0);fr.offsetMin=new Vector2(26,10);fr.offsetMax=new Vector2(-26,52);
        overlay.SetActive(false);
    }
    TMP_Text Label(string name,Transform parent,string caption,int size,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var text=go.GetComponent<TextMeshProUGUI>();
        if(font!=null)text.font=font;if(textMaterial!=null)text.fontSharedMaterial=textMaterial;
        text.text=caption;text.fontSize=size;text.color=color;text.textWrappingMode=TextWrappingModes.Normal;text.raycastTarget=false;return text;
    }
    Button MakeButton(string name,Transform parent,string caption,UnityEngine.Events.UnityAction callback)
    {
        var go=Panel(name,parent,Color.white);var image=go.GetComponent<Image>();image.sprite=GameControlsGuide.Frame(false);image.type=Image.Type.Sliced;
        var button=go.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(callback);
        var text=Label("Label",go.transform,caption,20,Gold);text.alignment=TextAlignmentOptions.Center;text.enableAutoSizing=true;text.fontSizeMin=14;text.fontSizeMax=20;Stretch(text.gameObject,12,6,12,6);return button;
    }
    static ScrollRect CreateList(Transform parent)
    {
        var go=new GameObject("Owned Cards",typeof(RectTransform),typeof(ScrollRect));go.transform.SetParent(parent,false);
        var view=Panel("Viewport",go.transform,new Color(.12f,.065f,.025f));Stretch(view,0,0,0,0);view.AddComponent<RectMask2D>();
        var content=new GameObject("Content",typeof(RectTransform),typeof(VerticalLayoutGroup),typeof(ContentSizeFitter));content.transform.SetParent(view.transform,false);
        var r=(RectTransform)content.transform;r.anchorMin=new Vector2(0,1);r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,1);r.offsetMin=r.offsetMax=Vector2.zero;
        var layout=content.GetComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(8,8,8,8);layout.spacing=8;layout.childControlWidth=layout.childControlHeight=true;layout.childForceExpandHeight=false;
        content.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var scroll=go.GetComponent<ScrollRect>();scroll.viewport=(RectTransform)view.transform;scroll.content=r;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=32;return scroll;
    }
    static GameObject Panel(string name,Transform parent,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);go.GetComponent<Image>().color=color;return go;
    }
    static void Position(GameObject go,float x,float y,float width,float height)
    {var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(width,height);}
    static void Stretch(GameObject go,float left,float top,float right,float bottom)
    {var r=(RectTransform)go.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(left,bottom);r.offsetMax=new Vector2(-right,-top);}
}
