using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>Plays the Humble Gift cutout reveal using its original exported frames.</summary>
[RequireComponent(typeof(UnityEngine.UI.Image))]
public sealed class WorkshopPaperAnimation : MonoBehaviour
{
    private static Sprite[] frames;
    private Coroutine playing;
    private UnityEngine.UI.Image image;
    private CanvasGroup[] content;
    void Awake(){image=GetComponent<UnityEngine.UI.Image>();}
    void OnEnable()=>Reveal();
    public void Reveal()
    {
        if(!Application.isPlaying)return;
        if(image==null)image=GetComponent<UnityEngine.UI.Image>();
        if(frames==null)frames=Resources.LoadAll<Texture2D>("UI/WorkshopReveal").OrderBy(t=>t.name).Select(t=>{
            t.filterMode=FilterMode.Point;t.wrapMode=TextureWrapMode.Clamp;
            return Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(14,14,14,14));}).ToArray();
        if(playing!=null)StopCoroutine(playing);
        if(frames.Length>0)playing=StartCoroutine(Animate());
    }
    IEnumerator Animate()
    {
        image.type=UnityEngine.UI.Image.Type.Simple;
        float elapsed=0;
        while(elapsed<.65f)
        {
            if(content==null||content.Length!=transform.childCount)content=transform.Cast<Transform>().Select(t=>{
                var group=t.GetComponent<CanvasGroup>();if(group==null)group=t.gameObject.AddComponent<CanvasGroup>();return group;}).ToArray();
            if(!GameControlsGuide.SettingsOpen)elapsed+=Time.unscaledDeltaTime;
            image.sprite=frames[Mathf.Min(frames.Length-1,(int)(elapsed/.65f*frames.Length))];
            foreach(var group in content)if(group!=null){group.alpha=Mathf.Clamp01((elapsed-.4f)/.25f);group.interactable=elapsed>=.65f;group.blocksRaycasts=elapsed>=.65f;}
            yield return null;
        }
        image.sprite=frames[frames.Length-1];image.type=UnityEngine.UI.Image.Type.Sliced;playing=null;
    }
    void OnDisable(){if(playing!=null)StopCoroutine(playing);playing=null;if(content!=null)foreach(var group in content)if(group!=null){group.alpha=1;group.interactable=true;group.blocksRaycasts=true;}}
}
