using System;
using System.Collections;
using UnityEngine;

/// <summary>Turns a complete uGUI sheet, including its text, about the centre binding.</summary>
public sealed class WorkshopBookTurn : MonoBehaviour
{
    public bool Busy { get; private set; }
    public float Angle { get; private set; }
    private RectTransform leaf;
    private GameObject front,back,stationary;
    private UnityEngine.UI.Image shadow;
    private Transform left,right;
    private Action change;
    private Action completed;
    private bool forward,changed;
    private CanvasGroup input;
    private Coroutine routine;
    public void Begin(Transform leftPage,Transform rightPage,CanvasGroup controls,bool next,Action renderDestination,Action afterTurn=null)
    {
        if(Busy)return;
        left=leftPage;right=rightPage;input=controls;forward=next;change=renderDestination;completed=afterTurn;changed=false;Busy=true;
        input.interactable=false;
        var go=new GameObject("Turning paper sheet",typeof(RectTransform),typeof(CanvasGroup));go.transform.SetParent(transform,false);
        leaf=(RectTransform)go.transform;leaf.anchorMin=leaf.anchorMax=new Vector2(.5f,.5f);leaf.pivot=new Vector2(next?0:1,.5f);leaf.sizeDelta=((RectTransform)rightPage).rect.size;leaf.anchoredPosition=Vector2.zero;
        go.GetComponent<CanvasGroup>().blocksRaycasts=false;
        front=Face(next?rightPage:leftPage,false);
        stationary=Instantiate((next?leftPage:rightPage).gameObject,transform,false);
        stationary.name="Stationary old paper";
        change?.Invoke();back=Face(next?leftPage:rightPage,true);back.SetActive(false);
        var shade=new GameObject("Moving fold shadow",typeof(RectTransform),typeof(UnityEngine.UI.Image));shade.transform.SetParent(transform,false);
        shadow=shade.GetComponent<UnityEngine.UI.Image>();shadow.color=Color.clear;shadow.raycastTarget=false;
        var sr=(RectTransform)shade.transform;sr.anchorMin=sr.anchorMax=new Vector2(.5f,.5f);sr.pivot=new Vector2(.5f,.5f);sr.sizeDelta=new Vector2(18,leaf.rect.height);sr.anchoredPosition=Vector2.zero;
        leaf.SetAsLastSibling();Sample(0);
        if(Application.isPlaying)routine=StartCoroutine(Play());
    }
    GameObject Face(Transform source,bool reverse)
    {
        var face=Instantiate(source.gameObject,leaf,false);face.name=reverse?"New page reverse face":"Old page front face";
        var rect=(RectTransform)face.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;rect.localScale=Vector3.one;rect.localRotation=Quaternion.Euler(0,reverse?180:0,0);
        return face;
    }
    IEnumerator Play()
    {
        float elapsed=0;
        while(Busy&&elapsed<.6f)
        {
            if(!GameControlsGuide.SettingsOpen)elapsed+=Time.unscaledDeltaTime;
            Sample(elapsed/.6f);yield return null;
        }
        routine=null;
    }
    // Also permits exact phase inspection in the Editor without touching the player's save.
    public void Sample(float phase)
    {
        if(!Busy)return;phase=Mathf.Clamp01(phase);
        if(phase>=.5f&&!changed)
        {
            changed=true;back.SetActive(true);front.SetActive(false);
        }
        float eased=phase*phase*(3-2*phase);Angle=(forward?-1:1)*180*eased;
        leaf.localRotation=Quaternion.Euler(0,Angle,Mathf.Sin(phase*Mathf.PI)*(forward?1:-1)*1.5f);
        float fold=Mathf.Sin(phase*Mathf.PI);shadow.color=new Color(.08f,.035f,.01f,fold*.35f);
        ((RectTransform)shadow.transform).sizeDelta=new Vector2(12+fold*60,leaf.rect.height);
        if(phase>=1)Finish(true);
    }
    public void Cancel()
    {
        if(routine!=null)StopCoroutine(routine);routine=null;Finish(false);
    }
    void Finish(bool notify)
    {
        if(leaf!=null){leaf.gameObject.SetActive(false);Remove(leaf.gameObject);}
        if(shadow!=null){shadow.gameObject.SetActive(false);Remove(shadow.gameObject);}
        if(stationary!=null){stationary.SetActive(false);Remove(stationary);stationary=null;}
        if(input!=null)input.interactable=true;
        Busy=false;leaf=null;shadow=null;change=null;var callback=completed;completed=null;if(notify)callback?.Invoke();
    }
    static void Remove(GameObject go){if(Application.isPlaying)Destroy(go);else DestroyImmediate(go);}
    void OnDisable()=>Cancel();
}
