using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterInventory : MonoBehaviour
{
    [Serializable] public class CatalogEntry { public string id; public CardData card; }
    [Serializable] public class Stack { public string id; public int quantity; public int level = 1; }
    [Serializable] private class SaveData
    {
        public List<Stack> cards = new List<Stack>();
        public int coins;
        public int libraryTokens;
        public List<string> openedChests = new List<string>();
        public int starterCardsVersion;
    }

    public List<CatalogEntry> catalog = new List<CatalogEntry>();
    public HeroData starterHero;
    public static string SaveKey => SaveSlots.InventoryKey;
    private SaveData data = new SaveData();
    private int loadedSlot;
    public IReadOnlyList<Stack> Cards => data.cards;
    public int Coins => data.coins;
    public int LibraryTokens => data.libraryTokens;

    void Awake()
    {
        loadedSlot = SaveSlots.Active;
        starterHero = SelectedHero.Resolve(starterHero);
        var sharedCatalog = Resources.Load<InventoryCardCatalog>("InventoryCardCatalog");
        if (sharedCatalog != null)
            foreach (var entry in sharedCatalog.cards)
                if (entry != null && entry.card != null && !string.IsNullOrEmpty(entry.id) &&
                    !catalog.Exists(e => e != null && e.card == entry.card))
                    catalog.Add(new CatalogEntry { id = entry.id, card = entry.card });
        foreach (var card in Resources.LoadAll<CardData>("ChestCards"))
            if (!catalog.Exists(e => e != null && e.card == card))
                catalog.Add(new CatalogEntry { id = "chest." + card.name, card = card });
        SaveSlots.MigrateLegacy();
        bool loaded = false;
        if (PlayerPrefs.HasKey(SaveKey))
        {
            try
            {
                var saved = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
                if (saved != null && saved.cards != null)
                {
                    data = saved;
                    if (data.openedChests == null) data.openedChests = new List<string>();
                    data.cards.RemoveAll(s => s == null || s.quantity <= 0);
                    foreach (var stack in data.cards) stack.level = CardRefinementRules.Level(stack.level);
                    data.coins = Mathf.Max(0, data.coins);
                    data.libraryTokens = Mathf.Max(0, data.libraryTokens);
                    loaded = true;
                }
            }
            catch (ArgumentException) { Debug.LogWarning("Inventory save could not be read; restoring starter cards."); }
        }
        if (!loaded || data.starterCardsVersion < 1)
        {
            bool complete = starterHero != null && starterHero.Deck != null;
            if (starterHero != null && starterHero.Deck != null)
                foreach (var card in starterHero.Deck)
                {
                    var entry = catalog.Find(e => e != null && e.card == card && !string.IsNullOrEmpty(e.id));
                    if (card == null || entry == null) { complete = false; continue; }
                    // Repair older saves once, preserving owned copies and all earned rewards.
                    if ((!loaded || !data.cards.Exists(s => s.id == entry.id)) && !AddCardInternal(card, 1))
                        complete = false;
                }
            if (complete) data.starterCardsVersion = 1;
            Save();
        }
    }

    public CardData GetCard(string id) => catalog.Find(e => e != null && e.id == id)?.card;
    public int GetLevel(string id) => CardRefinementRules.Level(data.cards.Find(s => s.id == id)?.level ?? 1);
    public int GetQuantity(string id) => Mathf.Max(0, data.cards.Find(s => s.id == id)?.quantity ?? 0);

    static string SharedId(CardData card)
    {
        if (card == null) return null;
        if (Array.Exists(Resources.LoadAll<CardData>("ChestCards"), c => c == card)) return "chest." + card.name;
        return Resources.Load<InventoryCardCatalog>("InventoryCardCatalog")?.cards.Find(e => e != null && e.card == card)?.id;
    }

    public static int SavedCardLevel(CardData card)
    {
        string id = SharedId(card);
        if (string.IsNullOrEmpty(id) || !PlayerPrefs.HasKey(SaveKey)) return 1;
        try
        {
            var saved = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
            return CardRefinementRules.Level(saved?.cards?.Find(s => s != null && s.id == id)?.level ?? 1);
        }
        catch (ArgumentException) { return 1; }
    }

    public bool CanRefine(string id, out string reason)
    {
        if (loadedSlot != SaveSlots.Active) { reason = "The active save slot changed. Reopen this character's inventory."; return false; }
        return CardRefinementRules.CanAfford(GetCard(id), GetLevel(id), GetQuantity(id), Coins, out reason);
    }

    public bool TryRefine(string id, out string message)
    {
        if (!CanRefine(id, out message)) return false;
        var stack = data.cards.Find(s => s.id == id);
        int level = GetLevel(id);
        // Consume duplicates and coins together, then save once. Never modify shared CardData.
        stack.quantity -= CardRefinementRules.DuplicateCost(level);
        data.coins -= CardRefinementRules.CoinCost(level);
        stack.level = level + 1;
        Save();
        message = GetCard(id).name + " refined to level " + stack.level + ".";
        return true;
    }

    public bool IsChestOpened(string id) => data.openedChests.Contains(id);

    public bool ClaimChest(string id, CardData card)
    {
        if (string.IsNullOrEmpty(id) || IsChestOpened(id) || !AddCardInternal(card, 1)) return false;
        // Persist the reward and chest together so a reload cannot duplicate the card.
        data.openedChests.Add(id);
        Save();
        return true;
    }

    public static List<CardData> WithChestRewards(List<CardData> starterDeck)
    {
        var deck = starterDeck != null ? new List<CardData>(starterDeck) : new List<CardData>();
        if (!PlayerPrefs.HasKey(SaveKey)) return deck;
        SaveData saved;
        try { saved = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey)); }
        catch (ArgumentException) { return deck; }
        if (saved?.cards == null) return deck;
        // A consumed duplicate must not remain as a free extra copy in the starter deck.
        var used = new Dictionary<CardData, int>();
        deck.RemoveAll(card =>
        {
            if (card == null) return true;
            string id = SharedId(card);
            var stack = saved.cards.Find(s => s != null && s.id == id);
            if (stack == null) return false; // Preserve legacy saves with incomplete starter metadata.
            used.TryGetValue(card, out int count);
            used[card] = count + 1;
            return count >= stack.quantity;
        });
        foreach (var card in Resources.LoadAll<CardData>("ChestCards"))
            if (saved.cards.Exists(s => s != null && s.id == "chest." + card.name && s.quantity > 0) && !deck.Contains(card))
                deck.Add(card);
        return deck;
    }

    bool AddCardInternal(CardData card, int amount)
    {
        if (card == null || amount <= 0) return false;
        var entry = catalog.Find(e => e != null && e.card == card);
        if (entry == null || string.IsNullOrEmpty(entry.id)) return false;
        var stack = data.cards.Find(s => s.id == entry.id);
        if (stack == null) data.cards.Add(new Stack { id = entry.id, quantity = amount });
        else stack.quantity = (int)Math.Min(int.MaxValue, (long)stack.quantity + amount);
        return true;
    }

    public bool AddCard(CardData card, int amount = 1)
    {
        if (!AddCardInternal(card, amount)) return false;
        Save();
        return true;
    }

    public bool RemoveCard(CardData card, int amount = 1)
    {
        var entry = catalog.Find(e => e != null && e.card == card);
        var stack = entry == null ? null : data.cards.Find(s => s.id == entry.id);
        if (amount <= 0 || stack == null || stack.quantity < amount) return false;
        stack.quantity -= amount;
        if (stack.quantity == 0) data.cards.Remove(stack);
        Save();
        return true;
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        data.coins = (int)Math.Min(int.MaxValue, (long)data.coins + amount);
        Save();
    }

    public void AddLibraryTokens(int amount)
    {
        if (amount <= 0) return;
        data.libraryTokens = (int)Math.Min(int.MaxValue, (long)data.libraryTokens + amount);
        Save();
    }

    public static void ResetSavedInventory() { PlayerPrefs.DeleteKey(SaveKey); }
    void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        SaveSlots.NotifyLocalSaveChanged();
    }
}
