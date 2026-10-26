

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

    public string Title => Data != null ? Data.name : "Missing Card";
    public string Description => Data != null ? Data.Description : "This card asset is missing.";
    public int Mana => Data != null ? Data.Mana : 0;
    public Sprite Image => Data != null ? Data.Image : null;
    public Effect ManualTargetEffect => Data != null ? Data.ManualTargetEffect : null;
    public List<AutoTargetEffect> OtherEffects => Data != null && Data.OtherEffects != null
        ? Data.OtherEffects
        : new List<AutoTargetEffect>();

    public bool RequiresManualTarget
    {
        get { return ManualTargetEffect != null; }
    }
}
