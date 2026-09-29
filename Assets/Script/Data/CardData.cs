using System.Collections.Generic;
using System.Runtime.InteropServices;
using SerializeReferenceEditor;
using UnityEngine;
 
[CreateAssetMenu(menuName = "Data/Card")]
public class CardData : ScriptableObject
{
    [field: SerializeField] public string Description {get; private set;}
    [field: SerializeField] public int Mana {get; private set;}
    [field: SerializeField] public Sprite Image {get; private set;}
    [field: SerializeReference, SR] public Effect ManualTargetEffect {get; private set;} = null;
    [field: SerializeField] public List<AutoTargetEffect> OtherEffects {get; private set;}

}
