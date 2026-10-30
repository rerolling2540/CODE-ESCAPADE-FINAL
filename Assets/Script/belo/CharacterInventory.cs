using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterInventory : MonoBehaviour
{
    private const int CurrentStarterCardsVersion = 3;
    private static CharacterInventory activeInstance;
    public static event Action InventoryChanged;
    [Serializable] public class CatalogEntry { public string id; public CardData card; }
    [Serializable] public class Stack { public string id; public int quantity; public int level = 1; }
    [Serializable] public class CloudCardStack { public string card_key; public int quantity; public int level = 1; }
    [Serializable] private class SaveData
    {
        public List<Stack> cards = new List<Stack>();
        public int coins;
        public int libraryTokens;
        public int lowLogicScraps;
        public int syntaxShards;
        public List<string> ownedCosmetics = new List<string>();
        public string equippedCosmetic;
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
    public int LowLogicScraps => data.lowLogicScraps;
    public int SyntaxShards => data.syntaxShards;
    public string EquippedCosmetic => data.equippedCosmetic;
    public bool OwnsCosmetic(string id) => data.ownedCosmetics.Contains(id);
    public bool IsCurrentSlot => loadedSlot == SaveSlots.Active;

    void Awake()
    {
        SaveSlots.ProfileChanged += HandleSaveProfileChanged;
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
        var merchant = SyntaxMerchantCatalog.Load();
        if (merchant != null)
            foreach (var offer in merchant.offers)
                if (offer != null && offer.kind == MerchantItemKind.Card && offer.card != null && !string.IsNullOrEmpty(offer.cardId) && !catalog.Exists(e => e != null && e.card == offer.card))
                    catalog.Add(new CatalogEntry { id = offer.cardId, card = offer.card });
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
                    data.lowLogicScraps = Mathf.Max(0, data.lowLogicScraps);
                    data.syntaxShards = Mathf.Max(0, data.syntaxShards);
                    if(data.ownedCosmetics==null)data.ownedCosmetics=new List<string>();
                    if(!string.IsNullOrEmpty(data.equippedCosmetic)&&!data.ownedCosmetics.Contains(data.equippedCosmetic))data.equippedCosmetic=null;
                    loaded = true;
                }
            }
            catch (ArgumentException) { Debug.LogWarning("Inventory save could not be read; restoring starter cards."); }
        }
        if (!loaded || data.starterCardsVersion < CurrentStarterCardsVersion)
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
            if (complete)
            {
                data.starterCardsVersion = CurrentStarterCardsVersion;
                Debug.Log($"Initialized {starterHero.name} starter inventory with {starterHero.Deck.Count} card(s) in save slot {loadedSlot + 1}.");
            }
            Save();
        }
        if(Application.isPlaying && GetComponent<playerMovement>() != null && GetComponent<MerchantTitleCosmetic>() == null)
            gameObject.AddComponent<MerchantTitleCosmetic>();
        if (IsCurrentSlot) activeInstance = this;
    }

    void OnDestroy()
    {
        SaveSlots.ProfileChanged -= HandleSaveProfileChanged;
        if (activeInstance == this) activeInstance = null;
    }

    void HandleSaveProfileChanged()
    {
        loadedSlot = SaveSlots.Active;
        data = new SaveData();
        if (PlayerPrefs.HasKey(SaveKey))
        {
            try
            {
                SaveData saved = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
                if (saved != null)
                {
                    data = saved;
                    if (data.cards == null) data.cards = new List<Stack>();
                    if (data.openedChests == null) data.openedChests = new List<string>();
                    if (data.ownedCosmetics == null) data.ownedCosmetics = new List<string>();
                    data.cards.RemoveAll(stack => stack == null || stack.quantity <= 0);
                    foreach (var stack in data.cards)
                        stack.level = CardRefinementRules.Level(stack.level);
                }
            }
            catch (ArgumentException)
            {
                Debug.LogWarning("Inventory save could not be read after switching save profiles.");
            }
        }

        if (IsCurrentSlot) activeInstance = this;
        InventoryChanged?.Invoke();
    }

    public CardData GetCard(string id) => catalog.Find(e => e != null && e.id == id)?.card;
    public int GetLevel(string id) => CardRefinementRules.Level(data.cards.Find(s => s.id == id)?.level ?? 1);
    public int GetQuantity(string id) => Mathf.Max(0, data.cards.Find(s => s.id == id)?.quantity ?? 0);

    static string SharedId(CardData card)
    {
        if (card == null) return null;
        if (Array.Exists(Resources.LoadAll<CardData>("ChestCards"), c => c == card)) return "chest." + card.name;
        string id=Resources.Load<InventoryCardCatalog>("InventoryCardCatalog")?.cards.Find(e => e != null && e.card == card)?.id;
        return id ?? SyntaxMerchantCatalog.Load()?.offers.Find(o=>o!=null&&o.kind==MerchantItemKind.Card&&o.card==card)?.cardId;
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

    public int Balance(MerchantCurrency currency) => currency==MerchantCurrency.Coins?Coins:currency==MerchantCurrency.LibraryTokens?LibraryTokens:SyntaxShards;

    public bool CanPurchase(string offerId, out string reason)
    {
        if(!IsCurrentSlot){reason="The save slot changed. Reopen the merchant.";return false;}
        var offer=SyntaxMerchantCatalog.Load()?.Find(offerId);
        if(offer==null||offer.price<=0){reason="This item is unavailable.";return false;}
        if(!offer.AvailableFor(PlayerPrefs.GetInt("SelectedCharacter",0))){reason="This card belongs to another hero class.";return false;}
        if(offer.kind==MerchantItemKind.Card&&(offer.card==null||GetCard(offer.cardId)!=offer.card)){reason="This card is not in the inventory catalog.";return false;}
        if(offer.kind==MerchantItemKind.Card&&GetQuantity(offer.cardId)==int.MaxValue){reason="This card stack is full.";return false;}
        if(offer.kind==MerchantItemKind.SyntaxShard&&SyntaxShards==int.MaxValue){reason="Your syntax shard pouch is full.";return false;}
        if(offer.kind==MerchantItemKind.CosmeticTitle&&OwnsCosmetic(offer.id)){reason="You already own this title.";return false;}
        if(Balance(offer.currency)<offer.price){reason="Requires "+offer.price+" "+SyntaxMerchantCatalog.CurrencyName(offer.currency)+".";return false;}
        reason="Ready to purchase.";return true;
    }

    public bool TryPurchase(string offerId,out string message)
    {
        if(!CanPurchase(offerId,out message))return false;
        var offer=SyntaxMerchantCatalog.Load().Find(offerId);
        if(offer.kind==MerchantItemKind.Card&&!AddCardInternal(offer.card,1)){message="The card could not be added.";return false;}
        if(offer.kind==MerchantItemKind.SyntaxShard)data.syntaxShards++;
        if(offer.kind==MerchantItemKind.CosmeticTitle){data.ownedCosmetics.Add(offer.id);data.equippedCosmetic=offer.id;}
        if(offer.currency==MerchantCurrency.Coins)data.coins-=offer.price;
        else if(offer.currency==MerchantCurrency.LibraryTokens)data.libraryTokens-=offer.price;
        else data.syntaxShards-=offer.price;
        Save();message="Purchased "+offer.title+".";return true;
    }

    public bool CanTradeSpare(string id,bool recycle,out string reason)
    {
        if(!IsCurrentSlot){reason="The save slot changed. Reopen the merchant.";return false;}
        if(GetCard(id)==null||GetQuantity(id)<2){reason="Keep your last copy. Only spare copies can be traded.";return false;}
        if((recycle&&(long)LowLogicScraps+SyntaxMerchantCatalog.ScrapsPerCard>int.MaxValue)||(!recycle&&LibraryTokens==int.MaxValue)){reason="Your resource pouch is full.";return false;}
        reason=recycle?"Recycle one spare copy into 3 low-logic scraps.":"Trade one spare copy for 1 Library Token.";return true;
    }

    public bool TryTradeSpare(string id,bool recycle,out string message)
    {
        if(!CanTradeSpare(id,recycle,out message))return false;
        data.cards.Find(s=>s.id==id).quantity--;
        if(recycle)data.lowLogicScraps+=SyntaxMerchantCatalog.ScrapsPerCard;else data.libraryTokens++;
        Save();message=recycle?"Recycled one spare card into 3 low-logic scraps.":"Traded one spare card for 1 Library Token.";return true;
    }

    public bool CanExchangeScraps => IsCurrentSlot&&LowLogicScraps>=SyntaxMerchantCatalog.ScrapsPerToken&&LibraryTokens<int.MaxValue;
    public bool TryExchangeScraps(out string message)
    {
        if(!CanExchangeScraps){message="Requires 3 low-logic scraps and space for another token.";return false;}
        data.lowLogicScraps-=SyntaxMerchantCatalog.ScrapsPerToken;data.libraryTokens++;Save();message="Exchanged 3 scraps for 1 Library Token.";return true;
    }

    public bool TryEquipCosmetic(string id,out string message)
    {
        if(!IsCurrentSlot||(!string.IsNullOrEmpty(id)&&!OwnsCosmetic(id))){message="Purchase this title before equipping it.";return false;}
        if(!string.IsNullOrEmpty(id)&&SyntaxMerchantCatalog.Load()?.Find(id)?.kind!=MerchantItemKind.CosmeticTitle){message="This title is unavailable.";return false;}
        data.equippedCosmetic=id;Save();message=string.IsNullOrEmpty(id)?"Title hidden.":"Title equipped.";return true;
    }

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
        SaveData saved = null;
        bool hasSavedInventory = PlayerPrefs.HasKey(SaveKey);
        if (hasSavedInventory)
        {
            try
            {
                saved = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
                if (saved == null)
                {
                    Debug.LogError("The saved card inventory is empty or invalid. Using this hero's starter deck for this battle without overwriting the save.");
                    return starterDeck != null ? new List<CardData>(starterDeck.FindAll(card => card != null)) : new List<CardData>();
                }
            }
            catch (ArgumentException)
            {
                Debug.LogError("The saved card inventory could not be read. Using this hero's starter deck for this battle without overwriting the save.");
                return starterDeck != null ? new List<CardData>(starterDeck.FindAll(card => card != null)) : new List<CardData>();
            }
        }

        // A fresh profile can enter a battle before the exploration scene has created
        // CharacterInventory. Seed only this hero's five starter cards, never the full catalog.
        saved ??= new SaveData();
        saved.cards ??= new List<Stack>();
        if (saved.starterCardsVersion < CurrentStarterCardsVersion)
        {
            bool complete = starterDeck != null;
            if (starterDeck != null)
            {
                foreach (CardData card in starterDeck)
                {
                    if (card == null) { complete = false; continue; }
                    string id = SharedId(card);
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        complete = false;
                        Debug.LogWarning($"Starter card '{card.name}' has no inventory catalog ID; it is available for this battle but could not be saved to inventory.");
                        continue;
                    }

                    Stack owned = saved.cards.Find(stack => stack != null && stack.id == id);
                    if (owned == null)
                        saved.cards.Add(new Stack { id = id, quantity = 1, level = 1 });
                }
            }

            if (complete) saved.starterCardsVersion = CurrentStarterCardsVersion;
            // Do not overwrite a malformed existing save. A genuinely new save is safe to initialize here.
            if (!hasSavedInventory || saved.starterCardsVersion == CurrentStarterCardsVersion)
            {
                PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(saved));
                PlayerPrefs.Save();
                InventoryChanged?.Invoke();
            }
        }

        if (saved.cards == null) return new List<CardData>();
        var deck = new List<CardData>();
        // Ownership controls playable copies, including purchased cards and consumed duplicates.
        var available=new Dictionary<string,CardData>();
        if (starterDeck != null)
            foreach(var card in starterDeck)if(card!=null){string id=SharedId(card);if(!string.IsNullOrEmpty(id))available[id]=card;}
        var catalog = Resources.Load<InventoryCardCatalog>("InventoryCardCatalog");
        if (catalog != null && catalog.cards != null)
            foreach (var entry in catalog.cards)
                if (entry != null && entry.card != null && !string.IsNullOrWhiteSpace(entry.id)) available[entry.id] = entry.card;
        foreach(var card in Resources.LoadAll<CardData>("ChestCards"))available["chest."+card.name]=card;
        var merchant=SyntaxMerchantCatalog.Load();
        if(merchant!=null)foreach(var offer in merchant.offers)
            if(offer!=null&&offer.kind==MerchantItemKind.Card&&offer.card!=null&&offer.AvailableFor(PlayerPrefs.GetInt("SelectedCharacter",0)))available[offer.cardId]=offer.card;
        foreach(var pair in available)
        {
            var stack=saved.cards.Find(s=>s!=null&&s.id==pair.Key);
            int count=stack!=null?Mathf.Max(0,stack.quantity):0;
            for(int i=0;i<count;i++)deck.Add(pair.Value);
        }
        return deck;
    }

    public static List<CloudCardStack> ReadSavedCards(int slot)
    {
        string key = "CodeEscapade.Inventory.v1.Slot." + slot;
        if (slot < 0 || slot >= SaveSlots.Count || !PlayerPrefs.HasKey(key)) return new List<CloudCardStack>();
        try
        {
            var saved = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(key));
            var result = new List<CloudCardStack>();
            if (saved?.cards == null) return result;
            foreach (var stack in saved.cards)
                if (stack != null && !string.IsNullOrWhiteSpace(stack.id) && stack.quantity > 0)
                    result.Add(new CloudCardStack { card_key = stack.id, quantity = stack.quantity, level = CardRefinementRules.Level(stack.level) });
            return result;
        }
        catch (ArgumentException) { return new List<CloudCardStack>(); }
    }

    public static void ApplyCloudCards(int slot, List<CloudCardStack> cards)
    {
        if (slot < 0 || slot >= SaveSlots.Count) return;
        string key = "CodeEscapade.Inventory.v1.Slot." + slot;
        SaveData saved = new SaveData();
        if (PlayerPrefs.HasKey(key))
        {
            try { saved = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(key)) ?? new SaveData(); }
            catch (ArgumentException) { saved = new SaveData(); }
        }
        saved.cards = new List<Stack>();
        if (cards != null)
            foreach (var card in cards)
                if (card != null && !string.IsNullOrWhiteSpace(card.card_key) && card.quantity > 0)
                    saved.cards.Add(new Stack { id = card.card_key, quantity = card.quantity, level = CardRefinementRules.Level(card.level) });
        PlayerPrefs.SetString(key, JsonUtility.ToJson(saved));
        PlayerPrefs.Save();
        if (activeInstance != null && activeInstance.loadedSlot == slot)
        {
            try { activeInstance.data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(key)) ?? new SaveData(); }
            catch (ArgumentException) { activeInstance.data = new SaveData(); }
        }
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
        InventoryChanged?.Invoke();
    }
}
