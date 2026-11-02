using UnityEngine;

/// <summary>Authored routes preserve earlier-teacher access; only the final trial route requires preparation.</summary>
public sealed class WardenTravelGate : MonoBehaviour
{
    public string destinationScene,arrivalId;
    public bool finalRoute;
    void Start()
    {
        var portal=PortalInteraction.For(gameObject);
        portal.entryPrompt="[E] Travel to "+destinationScene;
        portal.AddRoute(()=>!finalRoute||WardenQuest.Chapter(SaveSlots.Read(SaveSlots.Active))>=5,player=>{PortalArrivalPoint.Prepare(destinationScene,arrivalId);SceneTransitionFader.Load(destinationScene);});
    }
    void Update(){var p=GetComponent<PortalInteraction>();if(p!=null)p.unavailablePrompt=finalRoute&&WardenQuest.Chapter(SaveSlots.Read(SaveSlots.Active))<5?"Complete the elf's Map 4 chapter to enter the final trial.":"";}
}
