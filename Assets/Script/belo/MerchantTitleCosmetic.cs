using TMPro;
using UnityEngine;

/// <summary>Purchased titles are visible cosmetics in exploration; they grant no stats.</summary>
public sealed class MerchantTitleCosmetic : MonoBehaviour
{
    private CharacterInventory inventory;
    private TextMeshPro label;
    private SpriteRenderer effect,heroSprite;
    private Collider2D body;
    private string lastCosmetic;
    private float effectTime;
    void Awake()
    {
        inventory=GetComponent<CharacterInventory>()??FindFirstObjectByType<CharacterInventory>();
        var go=new GameObject("Merchant Title Cosmetic",typeof(TextMeshPro));go.transform.SetParent(transform,false);
        label=go.GetComponent<TextMeshPro>();label.fontSize=2.5f;label.alignment=TextAlignmentOptions.Center;
        var font=Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");if(font!=null)label.font=font;
        label.fontStyle=FontStyles.Bold;label.fontMaterial.SetFloat("_FaceDilate",0);label.fontMaterial.SetFloat("_OutlineSoftness",0);label.fontMaterial.SetFloat("_OutlineWidth",.1f);
        label.textWrappingMode=TextWrappingModes.NoWrap;label.rectTransform.sizeDelta=new Vector2(6,.6f);
        label.richText=false;label.sortingOrder=25;
        body=GetComponent<Collider2D>();
        var vfx=new GameObject("Scholar Cosmetic Effect",typeof(SpriteRenderer));vfx.transform.SetParent(transform,false);
        effect=vfx.GetComponent<SpriteRenderer>();effect.enabled=false;
    }
    void LateUpdate()
    {
        if(inventory==null)inventory=FindFirstObjectByType<CharacterInventory>();
        var offer=inventory!=null&&inventory.IsCurrentSlot&&inventory.OwnsCosmetic(inventory.EquippedCosmetic)?SyntaxMerchantCatalog.Load()?.Find(inventory.EquippedCosmetic):null;
        if(offer!=null&&offer.kind!=MerchantItemKind.CosmeticTitle)offer=null;
        bool warden=WardenQuest.Chapter(SaveSlots.Read(SaveSlots.Active))>=6;
        bool show=CharacterAlmanac.IsExplorationScene(gameObject.scene.name)&&inventory!=null&&inventory.IsCurrentSlot&&
            (warden||offer!=null)&&!GameControlsGuide.MapHidden&&!GameControlsGuide.GameplayOverlayHidden;
        label.gameObject.SetActive(show);effect.enabled=false;if(!show)return;
        float y=body!=null?body.bounds.max.y:transform.position.y+1.6f;
        label.transform.position=new Vector3(transform.position.x,y+.35f,transform.position.z-.1f);
        label.transform.rotation=Quaternion.identity;var scale=transform.lossyScale;label.transform.localScale=new Vector3(Mathf.Abs(scale.x)>.001f?1f/scale.x:1,Mathf.Abs(scale.y)>.001f?1f/scale.y:1,1);
        label.text=offer!=null?offer.cosmeticTitle:"Warden of the Hollow";label.color=offer!=null?offer.titleColor:new Color(1,.84f,.35f);
        if(heroSprite==null||!heroSprite.enabled)
            foreach(var candidate in GetComponentsInChildren<SpriteRenderer>())
                if(candidate!=effect&&candidate.enabled){heroSprite=candidate;break;}
        if(heroSprite!=null)
        {
            label.GetComponent<MeshRenderer>().sortingLayerID=heroSprite.sortingLayerID;
            label.sortingOrder=heroSprite.sortingOrder+3;
            effect.sortingLayerID=heroSprite.sortingLayerID;effect.sortingOrder=heroSprite.sortingOrder+1;
            effect.sharedMaterial=heroSprite.sharedMaterial;
        }
        string next=offer?.id;
        if(lastCosmetic!=next){lastCosmetic=next;effectTime=0;}
        var frames=offer?.cosmeticFrames;
        if(frames==null||frames.Length==0)return;
        effectTime+=Time.deltaTime;
        var frame=frames[Mathf.FloorToInt(effectTime*Mathf.Max(1,offer.cosmeticFramesPerSecond))%frames.Length];
        if(frame==null)return;
        frame.texture.filterMode=FilterMode.Point;
        effect.sprite=frame;effect.enabled=true;effect.color=new Color(1,1,1,.8f);
        var bounds=body!=null?body.bounds:new Bounds(transform.position+Vector3.up*.8f,new Vector3(1,1.6f,0));
        effect.transform.position=new Vector3(bounds.center.x,bounds.center.y,transform.position.z-.05f);
        effect.transform.rotation=Quaternion.identity;
        float size=bounds.size.y*1.3f/Mathf.Max(.01f,frame.bounds.size.y);
        effect.transform.localScale=new Vector3(Mathf.Abs(scale.x)>.001f?size/scale.x:size,Mathf.Abs(scale.y)>.001f?size/scale.y:size,1);
    }
    void OnDisable(){if(label!=null)label.gameObject.SetActive(false);if(effect!=null)effect.enabled=false;}
}
