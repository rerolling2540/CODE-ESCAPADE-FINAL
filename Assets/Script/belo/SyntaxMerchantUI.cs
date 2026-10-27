using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SyntaxMerchantUI : MonoBehaviour
{
    private static SyntaxMerchantUI instance;
    public static bool IsOpen => instance!=null&&instance.opened;
    private CharacterInventory inventory;
    private SyntaxMerchantNPC merchant;
    private GameObject overlay,listContent;
    private TMP_Text balances,title,description,price,message,primaryCaption;
    private Image art;
    private Button primary,recycle,cancel;
    private ScrollRect detailScroll;
    private readonly System.Collections.Generic.List<Button> tabs=new System.Collections.Generic.List<Button>();
    private string selection;
    private int category,slot,openedFrame;
    private bool opened,confirming,pendingRecycle;
    private float previousTimeScale;
    private TMP_FontAsset font;
    private Material material;
    private static readonly Color Gold=new Color(1f,.86f,.54f),Ink=new Color(.17f,.085f,.03f);

    public static void Show(CharacterInventory cards,SyntaxMerchantNPC owner)
    {
        if(cards==null||owner==null||IsOpen||Time.timeScale<=0||GameControlsGuide.GameplayOverlayHidden)return;
        if(instance==null)instance=new GameObject("Syntax Merchant Market").AddComponent<SyntaxMerchantUI>();
        instance.inventory=cards;instance.merchant=owner;instance.slot=SaveSlots.Active;
        instance.previousTimeScale=Time.timeScale;instance.opened=true;instance.openedFrame=Time.frameCount;
        instance.category=0;instance.selection=null;instance.confirming=false;Time.timeScale=0;
        instance.overlay.SetActive(true);instance.Refresh();
    }
    void Awake(){instance=this;BuildUI();}
    void Update()
    {
        if(!opened)return;
        if(inventory==null||merchant==null||!merchant.isActiveAndEnabled||SaveSlots.Active!=slot){Close();return;}
        if(Time.frameCount!=openedFrame&&Input.GetKeyDown(KeyCode.Escape)){if(confirming)Cancel();else Close();}
    }
    public void Close(){if(!opened)return;opened=false;confirming=false;Time.timeScale=previousTimeScale;if(overlay!=null)overlay.SetActive(false);}
    void OnDisable(){Close();}
    void OnDestroy(){Close();if(instance==this)instance=null;if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}}
    void Back(){var host=merchant!=null?merchant.GetComponent<Npc_Talk>():null;Close();if(host!=null&&host.isActiveAndEnabled)host.BeginConversation();}
    void Cancel(){confirming=false;Render();}
    void Select(string id){selection=id;confirming=false;Refresh();detailScroll.verticalNormalizedPosition=1;}
    void SetCategory(int value){category=value;selection=null;confirming=false;Refresh();}
    void Refresh()
    {
        balances.text="Coins: "+inventory.Coins+"   |   Tokens: "+inventory.LibraryTokens+"   |   Shards: "+inventory.SyntaxShards+"   |   Scraps: "+inventory.LowLogicScraps;
        for(int i=listContent.transform.childCount-1;i>=0;i--){var child=listContent.transform.GetChild(i).gameObject;child.SetActive(false);if(Application.isPlaying)Destroy(child);else DestroyImmediate(child);}
        var rows=new System.Collections.Generic.List<System.Tuple<string,string>>();
        var stock=SyntaxMerchantCatalog.Load();
        if(category==1)
        {
            rows.Add(System.Tuple.Create("scraps","Exchange low-logic scraps\n3 scraps = 1 Library Token"));
            foreach(var stack in inventory.Cards.OrderBy(s=>inventory.GetCard(s.id)?.name))
            {
                var card=inventory.GetCard(stack.id);if(card!=null)rows.Add(System.Tuple.Create("trade:"+stack.id,card.name+"\nOwned: "+stack.quantity+" · Spare: "+Mathf.Max(0,stack.quantity-1)));
            }
        }
        else if(stock!=null)
            foreach(var offer in stock.offers.Where(o=>o!=null&&o.AvailableFor(PlayerPrefs.GetInt("SelectedCharacter",0))))
            {
                bool include=category==0?offer.kind==MerchantItemKind.Card:category==2?offer.kind==MerchantItemKind.SyntaxShard:offer.kind==MerchantItemKind.CosmeticTitle;
                if(include)rows.Add(System.Tuple.Create("offer:"+offer.id,offer.title+"\n"+(offer.kind==MerchantItemKind.CosmeticTitle&&inventory.OwnsCosmetic(offer.id)?"Owned":offer.price+" "+SyntaxMerchantCatalog.CurrencyName(offer.currency))));
            }
        if(!rows.Any(r=>r.Item1==selection))selection=rows.FirstOrDefault()?.Item1;
        foreach(var row in rows)
        {
            string id=row.Item1;var b=MakeButton("Market "+id,listContent.transform,row.Item2,()=>Select(id));b.gameObject.AddComponent<LayoutElement>().preferredHeight=82;
            if(id==selection)b.GetComponent<Image>().color=new Color(1,.82f,.45f);
        }
        for(int i=0;i<tabs.Count;i++)tabs[i].GetComponent<Image>().color=i==category?new Color(1,.82f,.45f):Color.white;
        Render();
    }
    bool Ready(bool recycling,out string reason)
    {
        if(selection==null){reason="No item selected.";return false;}
        if(selection=="scraps"){reason=inventory.CanExchangeScraps?"Exchange 3 scraps for 1 token.":"Requires 3 low-logic scraps and space for another token.";return inventory.CanExchangeScraps;}
        if(selection.StartsWith("trade:"))return inventory.CanTradeSpare(selection.Substring(6),recycling,out reason);
        var offer=SyntaxMerchantCatalog.Load()?.Find(selection.Substring(6));
        if(offer!=null&&offer.kind==MerchantItemKind.CosmeticTitle&&inventory.OwnsCosmetic(offer.id)){reason="Equip or hide this owned title at no cost.";return true;}
        return inventory.CanPurchase(selection.Substring(6),out reason);
    }
    void Render(string result=null)
    {
        art.enabled=false;recycle.interactable=false;cancel.interactable=confirming;
        bool trade=selection!=null&&selection.StartsWith("trade:");
        recycle.gameObject.SetActive(trade&&!confirming);cancel.gameObject.SetActive(confirming);
        ((RectTransform)primary.transform).sizeDelta=new Vector2(confirming?544:trade?360:686,48);
        string caption="Purchase item";
        if(selection==null){title.text="No stock available";description.text="Return later or choose another category.";price.text="";}
        else if(selection=="scraps")
        {
            title.text="Exchange low-logic scraps";description.text="Scraps are recovered by recycling spare cards. Exchange three scraps for one Library Token.\n\nTokens buy syntax shards and scholar title cosmetics.";
            price.text="Give: 3 low-logic scraps\nReceive: 1 Library Token";caption="Exchange scraps";
        }
        else if(selection.StartsWith("trade:"))
        {
            string id=selection.Substring(6);var card=inventory.GetCard(id);title.text=card!=null?card.name:"Unavailable card";
            description.text="<b>SPARE CARD TRADE</b>\n\nTrade one spare copy for 1 Library Token, or recycle one copy into 3 scraps.\n\nYour last copy stays in your collection. Remaining copies keep level "+inventory.GetLevel(id)+". Traded copies leave your battle deck.";
            price.text="Owned: "+inventory.GetQuantity(id)+" · Spare copies: "+Mathf.Max(0,inventory.GetQuantity(id)-1);caption="Trade 1 copy (+1 token)";
            if(card!=null){art.sprite=card.Image;art.enabled=card.Image!=null;}
            recycle.interactable=!confirming&&inventory.CanTradeSpare(id,true,out _);
        }
        else
        {
            var offer=SyntaxMerchantCatalog.Load()?.Find(selection.Substring(6));
            title.text=offer!=null?offer.title:"Unavailable item";
            description.text=offer?.description??"This item is no longer available.";
            price.text=offer!=null?"Price: "+offer.price+" "+SyntaxMerchantCatalog.CurrencyName(offer.currency):"";
            if(offer!=null&&offer.kind==MerchantItemKind.Card&&offer.card!=null)
            {
                art.sprite=offer.card.Image;art.enabled=art.sprite!=null;
                description.text+="\n\n<b>CARD EFFECTS · LEVEL "+inventory.GetLevel(offer.cardId)+"</b>\n"+CardRefinementRules.Describe(offer.card,inventory.GetLevel(offer.cardId));
                price.text+="\nOwned copies: "+inventory.GetQuantity(offer.cardId)+" · Mana: "+offer.card.Mana;
            }
            if(offer!=null&&offer.kind==MerchantItemKind.CosmeticTitle&&inventory.OwnsCosmetic(offer.id))
            {bool equipped=inventory.EquippedCosmetic==offer.id;caption=equipped?"Hide title":"Equip title";price.text=equipped?"Owned · Currently equipped":"Owned · Equip at no cost";}
        }
        bool ready=Ready(confirming&&pendingRecycle,out string reason);primary.interactable=ready;
        primaryCaption.text=confirming?"Confirm "+(pendingRecycle?"recycling":"transaction"):caption;
        message.text=result??(confirming?"Confirm this exchange? The resources shown will be spent immediately.":reason);
        message.color=ready||result!=null?Ink:new Color(.52f,.13f,.07f);
    }
    void Act(bool recycling)
    {
        if(!opened||inventory==null||!inventory.IsCurrentSlot||SaveSlots.Active!=slot)return;
        if(confirming)recycling=pendingRecycle;
        if(!Ready(recycling,out string reason)){confirming=false;Render(reason);return;}
        var offer=selection.StartsWith("offer:")?SyntaxMerchantCatalog.Load()?.Find(selection.Substring(6)):null;
        if(offer!=null&&offer.kind==MerchantItemKind.CosmeticTitle&&inventory.OwnsCosmetic(offer.id))
        {inventory.TryEquipCosmetic(inventory.EquippedCosmetic==offer.id?null:offer.id,out string equipped);Refresh();Render(equipped);return;}
        if(!confirming){confirming=true;pendingRecycle=recycling;Render();if(recycling)price.text="Give: 1 spare copy\nReceive: 3 low-logic scraps";return;}
        confirming=false;string result;
        if(selection=="scraps")inventory.TryExchangeScraps(out result);
        else if(selection.StartsWith("trade:"))inventory.TryTradeSpare(selection.Substring(6),recycling,out result);
        else inventory.TryPurchase(selection.Substring(6),out result);
        Refresh();Render(result);
    }

    void BuildUI()
    {
        font=Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF")??TMP_Settings.defaultFontAsset;
        if(font!=null){material=new Material(font.material){name="Merchant Clear Text"};material.DisableKeyword("UNDERLAY_ON");material.DisableKeyword("UNDERLAY_INNER");material.SetFloat("_OutlineWidth",0);material.SetFloat("_OutlineSoftness",0);material.SetFloat("_FaceDilate",0);}
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=350;gameObject.AddComponent<GraphicRaycaster>();
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        overlay=Panel("Market Overlay",transform,new Color(.025f,.015f,.01f,.88f));Stretch(overlay,0,0,0,0);
        var frame=Paper("The Syntax Market",overlay.transform,false);Stretch(frame,40,25,40,25);
        var heading=Label("Market Title",frame.transform,"THE SYNTAX MERCHANT",29,Gold);Position(heading.gameObject,26,-18,800,44);
        Position(MakeButton("Close Market",frame.transform,"Close [Esc]",Close).gameObject,984,-20,190,42);
        Position(MakeButton("Back To Merchant",frame.transform,"< Back to merchant",Back).gameObject,26,-74,240,40);
        balances=Label("Market Balances",frame.transform,"",19,Gold);Position(balances.gameObject,292,-74,882,40);balances.alignment=TextAlignmentOptions.MidlineRight;
        string[] names={"Buy cards","Trade cards","Supplies","Cosmetics"};
        for(int i=0;i<names.Length;i++){int index=i;var b=MakeButton("Category "+names[i],frame.transform,names[i],()=>SetCategory(index));Position(b.gameObject,26+i*292,-126,280,40);tabs.Add(b);}
        var list=Scroll("Market Stock",frame.transform,true);Stretch(list.gameObject,26,184,780,66);listContent=list.content.gameObject;
        var body=Paper("Market Item",frame.transform,true);Stretch(body,444,184,26,66);
        title=Label("Selected Item",body.transform,"",26,Ink);Position(title.gameObject,22,-16,570,62);title.fontStyle=FontStyles.Bold;
        var artGo=Panel("Item Art",body.transform,Color.white);art=artGo.GetComponent<Image>();art.preserveAspect=true;art.raycastTarget=false;Position(artGo,628,-16,80,80);
        detailScroll=Scroll("Item Description",body.transform,false);Stretch(detailScroll.gameObject,22,96,22,190);description=detailScroll.content.gameObject.AddComponent<TextMeshProUGUI>();ApplyFont(description);description.fontSize=21;description.color=Ink;description.textWrappingMode=TextWrappingModes.Normal;description.margin=new Vector4(0,0,10,6);
        price=Label("Item Price",body.transform,"",21,Ink);var pr=(RectTransform)price.transform;pr.anchorMin=Vector2.zero;pr.anchorMax=new Vector2(1,0);pr.offsetMin=new Vector2(22,128);pr.offsetMax=new Vector2(-22,184);
        primary=MakeButton("Merchant Primary Action",body.transform,"Purchase item",()=>Act(false));Bottom(primary.gameObject,22,72,360,48);primaryCaption=primary.GetComponentInChildren<TMP_Text>();
        recycle=MakeButton("Recycle Spare Card",body.transform,"Recycle (+3 scraps)",()=>Act(true));Bottom(recycle.gameObject,396,72,312,48);
        cancel=MakeButton("Cancel Transaction",body.transform,"Cancel",Cancel);Bottom(cancel.gameObject,580,72,128,48);
        message=Label("Market Message",body.transform,"",18,Ink);var mr=(RectTransform)message.transform;mr.anchorMin=Vector2.zero;mr.anchorMax=new Vector2(1,0);mr.offsetMin=new Vector2(22,10);mr.offsetMax=new Vector2(-22,62);
        var footer=Label("Market Rules",frame.transform,"Scroll lists and descriptions to read more. Trades keep your last copy and refinement. Purchases join the battle deck.",17,Gold);var fr=(RectTransform)footer.transform;fr.anchorMin=Vector2.zero;fr.anchorMax=new Vector2(1,0);fr.offsetMin=new Vector2(26,10);fr.offsetMax=new Vector2(-26,52);
        overlay.SetActive(false);
    }
    void ApplyFont(TMP_Text t){if(font!=null)t.font=font;if(material!=null)t.fontSharedMaterial=material;}
    TMP_Text Label(string name,Transform parent,string text,int size,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var t=go.GetComponent<TextMeshProUGUI>();ApplyFont(t);t.text=text;t.fontSize=size;t.color=color;t.textWrappingMode=TextWrappingModes.Normal;t.raycastTarget=false;return t;}
    Button MakeButton(string name,Transform parent,string text,UnityEngine.Events.UnityAction callback){var go=Paper(name,parent,false);var b=go.AddComponent<Button>();b.targetGraphic=go.GetComponent<Image>();b.onClick.AddListener(callback);var t=Label("Label",go.transform,text,20,Gold);t.alignment=TextAlignmentOptions.Center;t.enableAutoSizing=true;t.fontSizeMin=14;t.fontSizeMax=20;Stretch(t.gameObject,10,6,10,6);return b;}
    static GameObject Paper(string name,Transform parent,bool paper){var go=Panel(name,parent,Color.white);var i=go.GetComponent<Image>();i.sprite=GameControlsGuide.Frame(paper);i.type=Image.Type.Sliced;return go;}
    static ScrollRect Scroll(string name,Transform parent,bool list)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(ScrollRect));go.transform.SetParent(parent,false);var view=Panel("Viewport",go.transform,list?new Color(.12f,.065f,.025f):new Color(1,1,1,0));Stretch(view,0,0,16,0);view.AddComponent<RectMask2D>();
        var content=new GameObject("Content",typeof(RectTransform),typeof(ContentSizeFitter));content.transform.SetParent(view.transform,false);var r=(RectTransform)content.transform;r.anchorMin=new Vector2(0,1);r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,1);r.offsetMin=r.offsetMax=Vector2.zero;content.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        if(list){var layout=content.AddComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(8,8,8,8);layout.spacing=8;layout.childControlWidth=layout.childControlHeight=true;layout.childForceExpandHeight=false;}
        var scroll=go.GetComponent<ScrollRect>();scroll.viewport=(RectTransform)view.transform;scroll.content=r;scroll.horizontal=false;scroll.scrollSensitivity=32;scroll.movementType=ScrollRect.MovementType.Clamped;
        var track=Panel("Scroll Track",go.transform,new Color(.17f,.10f,.05f));var tr=(RectTransform)track.transform;tr.anchorMin=new Vector2(1,0);tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(-12,0);tr.offsetMax=Vector2.zero;
        var handle=Panel("Scroll Handle",track.transform,new Color(.72f,.51f,.23f));Stretch(handle,2,2,2,2);var bar=track.AddComponent<Scrollbar>();bar.direction=Scrollbar.Direction.BottomToTop;bar.handleRect=(RectTransform)handle.transform;bar.targetGraphic=handle.GetComponent<Image>();scroll.verticalScrollbar=bar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;return scroll;
    }
    static GameObject Panel(string name,Transform parent,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);go.GetComponent<Image>().color=color;return go;}
    static void Position(GameObject go,float x,float y,float w,float h){var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
    static void Bottom(GameObject go,float x,float y,float w,float h){var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=Vector2.zero;r.pivot=Vector2.zero;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
    static void Stretch(GameObject go,float left,float top,float right,float bottom){var r=(RectTransform)go.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(left,bottom);r.offsetMax=new Vector2(-right,-top);}
}
