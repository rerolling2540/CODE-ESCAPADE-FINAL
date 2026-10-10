using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterInventory : MonoBehaviour
{
    [Serializable] public class CatalogEntry { public string id; public CardData card; }
    [Serializable] public class Stack { public string id; public int quantity; }
    [Serializable] private class SaveData
    {
        public List<Stack> cards = new List<Stack>();
        public int coins;
        public int libraryTokens;
    }

    public List<CatalogEntry> catalog = new List<CatalogEntry>();
    public HeroData starterHero;
    public const string SaveKey = "CodeEscapade.Inventory.v1";
    private SaveData data = new SaveData();
    public IReadOnlyList<Stack> Cards => data.cards;
    public int Coins => data.coins;
    public int LibraryTokens => data.libraryTokens;

    void Awake()
    {
        bool loaded = false;
        if (PlayerPrefs.HasKey(SaveKey))
        {
            try
            {
                var saved = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
                if (saved != null && saved.cards != null)
                {
                    data = saved;
                    data.cards.RemoveAll(s => s == null || s.quantity <= 0);
                    data.coins = Mathf.Max(0, data.coins);
                    data.libraryTokens = Mathf.Max(0, data.libraryTokens);
                    loaded = true;
                }
            }
            catch (ArgumentException) { Debug.LogWarning("Inventory save could not be read; restoring starter cards."); }
        }
        if (!loaded)
        {
            if (starterHero != null && starterHero.Deck != null)
                foreach (var card in starterHero.Deck) AddCardInternal(card, 1);
            Save();
        }
    }

    public CardData GetCard(string id) => catalog.Find(e => e != null && e.id == id)?.card;

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
    void Save() { PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data)); PlayerPrefs.Save(); }
}
