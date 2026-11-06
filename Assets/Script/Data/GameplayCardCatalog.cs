using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Only cards referenced here can enter gameplay. References point to Assets/Data/Cards/CARDS.</summary>
[CreateAssetMenu(menuName = "Data/Gameplay Card Catalog")]
public sealed class GameplayCardCatalog : ScriptableObject
{
    [Serializable] public sealed class Entry { public string id; public int hero; public CardData card; }
    [Serializable] public sealed class LegacyAlias { public CardData source; public CardData replacement; }
    public List<Entry> cards = new List<Entry>();
    public List<LegacyAlias> legacyAliases = new List<LegacyAlias>();
    private static GameplayCardCatalog cached;
    public static GameplayCardCatalog Load() => cached != null ? cached : cached = Resources.Load<GameplayCardCatalog>("GameplayCardCatalog");
    public static CardData Resolve(CardData source)
    {
        if (source == null) return null;
        var catalog = Load();
        if (catalog == null) { Debug.LogError("GameplayCardCatalog is missing; refusing legacy card data."); return null; }
        if (catalog.cards.Exists(e => e != null && e.card == source)) return source;
        return catalog.legacyAliases.Find(e => e != null && e.source == source)?.replacement;
    }
    public static string PrimaryId(CardData card)
    {
        var resolved = Resolve(card);
        return resolved != null ? Load()?.cards.Find(e => e != null && e.card == resolved)?.id : null;
    }
    public static bool ForCurrentHero(CardData card)
    {
        var resolved = Resolve(card);
        var entry = resolved != null ? Load()?.cards.Find(e => e != null && e.card == resolved) : null;
        return entry != null && (entry.hero < 0 || entry.hero == PlayerPrefs.GetInt("SelectedCharacter", 0));
    }
}
