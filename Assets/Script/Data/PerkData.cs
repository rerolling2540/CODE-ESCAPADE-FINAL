using System.Runtime.InteropServices;
using SerializeReferenceEditor;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Data/Perk")]
public class PerkData : ScriptableObject
{
    [field: SerializeField] public Sprite Image {get; private set;}
    [field: SerializeReference, SR] public PerkCondition PerkCondition {get; private set; }
    [field: SerializeReference, SR] public AutoTargetEffect AutoTargetEffect {get; private set;}
    [field: SerializeField] public bool UseAutoTarget {get; private set;}
    [field: SerializeField] public bool UseActionCasterAsTarget {get; private set; } = false;
    
}
