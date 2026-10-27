

using System.Collections.Generic;
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
        Level = CharacterInventory.SavedCardLevel(cardData);
    }

    public int Level { get; }

    public string Title => Data != null ? Data.name + (Level > 1 ? " [Lv " + Level + "]" : "") : "Missing Card";
    public string Description => Data != null ? Data.Description + (Level > 1 ? "\nRefined effects:\n" + CardRefinementRules.Describe(Data, Level) : "") : "This card asset is missing.";
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
