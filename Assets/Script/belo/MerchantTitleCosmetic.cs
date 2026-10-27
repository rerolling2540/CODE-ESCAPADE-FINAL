using TMPro;
using UnityEngine;

/// <summary>Purchased titles are visible cosmetics in exploration; they grant no stats.</summary>
public sealed class MerchantTitleCosmetic : MonoBehaviour
{
    private CharacterInventory inventory;
    private TextMeshPro label;
    void Awake()
    {
        inventory=GetComponent<CharacterInventory>();
        var go=new GameObject("Merchant Title Cosmetic",typeof(TextMeshPro));go.transform.SetParent(transform,false);
        label=go.GetComponent<TextMeshPro>();label.fontSize=2.5f;label.alignment=TextAlignmentOptions.Center;
        var font=Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");if(font!=null)label.font=font;
        label.fontStyle=FontStyles.Bold;label.fontMaterial.SetFloat("_FaceDilate",0);label.fontMaterial.SetFloat("_OutlineSoftness",0);label.fontMaterial.SetFloat("_OutlineWidth",.1f);
        label.textWrappingMode=TextWrappingModes.NoWrap;label.rectTransform.sizeDelta=new Vector2(6,.6f);
        label.richText=false;label.sortingOrder=25;
    }
    void LateUpdate()
    {
        var offer=inventory!=null?SyntaxMerchantCatalog.Load()?.Find(inventory.EquippedCosmetic):null;
        bool show=offer!=null&&offer.kind==MerchantItemKind.CosmeticTitle&&!GameControlsGuide.MapHidden;
        label.gameObject.SetActive(show);if(!show)return;
        var body=GetComponent<Collider2D>();float y=body!=null?body.bounds.max.y:transform.position.y+1.6f;
        label.transform.position=new Vector3(transform.position.x,y+.35f,transform.position.z-.1f);
        label.transform.rotation=Quaternion.identity;var scale=transform.lossyScale;label.transform.localScale=new Vector3(Mathf.Abs(scale.x)>.001f?1f/scale.x:1,Mathf.Abs(scale.y)>.001f?1f/scale.y:1,1);
        label.text=offer.cosmeticTitle;label.color=offer.titleColor;
    }
}
