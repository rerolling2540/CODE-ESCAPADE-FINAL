

using System.Collections.Generic;
using UnityEditor.Scripting;
using UnityEngine;

public class Card
{
    // Ensure Data is public getter
    public CardData Data { get; private set; }

    // Helper property to satisfy CardData references
    public CardData CardData => Data;

    public Card(CardData cardData)
    {
        Data = cardData;
    }

    public string Title => Data.name;
    public string Description => Data.Description;
    public int Mana => Data.Mana;
    public Sprite Image => Data.Image;
    public Effect ManualTargetEffect => Data.ManualTargetEffect;
    public List<AutoTargetEffect> OtherEffects => Data.OtherEffects;
}