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
        string runtimeKey = GameContentSync.RuntimeKey(source);
        if (runtimeKey != null) return GameContentSync.Find(runtimeKey);
        var catalog = Load();
        if (catalog == null) { Debug.LogError("GameplayCardCatalog is missing; refusing legacy card data."); return null; }
        var bundled = catalog.cards.Exists(e => e != null && e.card == source) ? source
            : catalog.legacyAliases.Find(e => e != null && e.source == source)?.replacement;
        string id = catalog.cards.Find(e => e != null && e.card == bundled)?.id;
        return GameContentSync.Find(id) ?? bundled;
    }
    public static string PrimaryId(CardData card)
    {
        string runtimeKey = GameContentSync.RuntimeKey(card);
        if (runtimeKey != null) return runtimeKey;
        var catalog = Load();
        var bundled = catalog?.cards.Exists(e => e != null && e.card == card) == true ? card
            : catalog?.legacyAliases.Find(e => e != null && e.source == card)?.replacement;
        return catalog?.cards.Find(e => e != null && e.card == bundled)?.id;
    }
    public static CardData FindById(string id) => GameContentSync.Find(id);
    public static bool CanObtain(CardData card) => card != null && GameContentSync.CanObtain(PrimaryId(card));
    public static bool ForCurrentHero(CardData card)
    {
        var resolved = Resolve(card);
        if (resolved == null) return false;
        int hero = GameContentSync.HeroFor(PrimaryId(resolved));
        return hero < 0 || hero == PlayerPrefs.GetInt("SelectedCharacter", 0);
    }
}
