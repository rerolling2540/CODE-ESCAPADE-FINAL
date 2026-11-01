using System.Collections.Generic;
using UnityEngine;

/// <summary>Class card library shown in the Satchel. Cards are playable after acquisition.</summary>
[CreateAssetMenu(menuName = "Data/Class Card Library")]
public sealed class CardDraftCatalog : ScriptableObject
{
    public List<CardData> warriorJava = new List<CardData>();
    public List<CardData> archerCSharp = new List<CardData>();
    public List<CardData> magePython = new List<CardData>();

    public IReadOnlyList<CardData> ForCharacter(int selectedCharacter)
    {
        switch (Mathf.Clamp(selectedCharacter, 0, 2))
        {
            case 0: return warriorJava;
            case 1: return archerCSharp;
            default: return magePython;
        }
    }
}
