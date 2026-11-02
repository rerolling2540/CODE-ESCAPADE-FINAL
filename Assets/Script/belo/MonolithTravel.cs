using UnityEngine;

/// <summary>Scene-authored portal. Uses the shared interaction rules and never spawns NPCs.</summary>
[RequireComponent(typeof(PortalInteraction))]
public sealed class MonolithTravel : MonoBehaviour
{
    public SpriteRenderer portalVisual;
    public Sprite[] frames;
    [Min(1)]public float framesPerSecond=12;
    void Start()
    {
        var interaction=GetComponent<PortalInteraction>();interaction.entryPrompt="[E] Open monolith travel";
        interaction.AddRoute(()=>isActiveAndEnabled&&!MonolithTravelPanel.IsOpen,player=>MonolithTravelPanel.Open(this));
    }
    void Update()
    {
        if(portalVisual!=null&&frames!=null&&frames.Length>0)portalVisual.sprite=frames[Mathf.FloorToInt(Time.time*framesPerSecond)%frames.Length];
    }
}
