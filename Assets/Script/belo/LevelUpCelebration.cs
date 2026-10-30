using UnityEngine;
using TMPro;
public sealed class LevelUpCelebration : MonoBehaviour
{
    private Transform target;
    private SpriteRenderer targetSprite,effect;
    private TextMeshPro label;
    private Sprite[] frames;
    private float elapsed;
    private LevelUpEffectData data;
    private static int pendingSlot=-1;
    public static void Request()
    {
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="Battle Scene")pendingSlot=SaveSlots.Active;
        else Play();
    }
    public static void PlayPending()
    {if(pendingSlot!=SaveSlots.Active)return;pendingSlot=-1;Play();}
    public static float BusyUntil {get;private set;}
    public static void Play()
    {
        var data=Resources.Load<LevelUpEffectData>("UI/LevelUpEffect");if(data==null||data.frames==null||data.frames.Length==0)return;
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="Battle Scene")return;
        HeroView hero=null;
        var player=Object.FindFirstObjectByType<playerMovement>();
        Transform target=hero!=null?hero.transform:player!=null?player.transform:null;if(target==null)return;
        var go=new GameObject("Level Up - HeavensFury");var celebration=go.AddComponent<LevelUpCelebration>();celebration.target=target;
        celebration.data=data;
        celebration.targetSprite=hero!=null?hero.MainSpriteRenderer:player.GetComponentsInChildren<SpriteRenderer>().FirstEnabled();
        celebration.frames=data.frames;celebration.effect=go.AddComponent<SpriteRenderer>();
        if(celebration.targetSprite!=null){celebration.effect.sortingLayerID=celebration.targetSprite.sortingLayerID;celebration.effect.sortingOrder=celebration.targetSprite.sortingOrder+5;}
        celebration.effect.sprite=data.frames[0];go.transform.localScale=Vector3.one*(5f/Mathf.Max(.01f,data.frames[0].bounds.size.y));
        var text=new GameObject("Leveled up",typeof(TextMeshPro));celebration.label=text.GetComponent<TextMeshPro>();text.transform.SetParent(go.transform,false);text.transform.localScale=Vector3.one/go.transform.localScale.x;
        celebration.label.text="Leveled up";celebration.label.fontSize=5;celebration.label.fontStyle=FontStyles.Bold;celebration.label.color=new Color(1,.85f,.3f);celebration.label.outlineWidth=.25f;celebration.label.outlineColor=Color.black;celebration.label.alignment=TextAlignmentOptions.Center;celebration.label.rectTransform.sizeDelta=new Vector2(7,1);celebration.label.GetComponent<MeshRenderer>().sortingOrder=celebration.effect.sortingOrder+1;
        BusyUntil=Time.unscaledTime+2f;
    }
    void LateUpdate()
    {
        if(target==null||PlayerDeathFlow.IsActive){Destroy(gameObject);return;}
        bool hidden=GameControlsGuide.SettingsOpen||CharacterAlmanac.IsOpen;
        effect.enabled=!hidden;label.enabled=!hidden;if(hidden)return;
        elapsed+=Time.deltaTime;
        if(elapsed>=2f){Destroy(gameObject);return;}
        effect.sprite=frames[Mathf.Min(frames.Length-1,Mathf.FloorToInt(elapsed*8))];
        var bounds=targetSprite!=null?targetSprite.bounds:new Bounds(target.position,Vector3.one*2);
        var body=target.GetComponent<Collider2D>();
        var center=body!=null?body.bounds.center:bounds.center;
        float height=body!=null?body.bounds.size.y:bounds.size.y;
        float scale=height*1.6f/Mathf.Max(.01f,frames[0].bounds.size.y*data.diameterFraction);
        transform.localScale=Vector3.one*scale;
        transform.position=new Vector3(center.x-data.centerOffset.x*frames[0].bounds.size.x*scale,center.y-data.centerOffset.y*frames[0].bounds.size.y*scale,target.position.z);
        label.transform.localScale=Vector3.one/scale;
        label.transform.position=new Vector3(bounds.center.x,bounds.max.y+.8f+elapsed*.15f,target.position.z);
    }
}
