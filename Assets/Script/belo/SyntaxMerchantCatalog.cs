using System;
using System.Collections.Generic;
using UnityEngine;

public enum MerchantCurrency { Coins, LibraryTokens, SyntaxShards }
public enum MerchantItemKind { Card, SyntaxShard, CosmeticTitle }

[CreateAssetMenu(menuName="Data/Syntax Merchant Catalog")]
public sealed class SyntaxMerchantCatalog : ScriptableObject
{
    [Serializable] public sealed class Offer
    {
        public string id, title;
        [TextArea] public string description;
        public MerchantItemKind kind;
        public MerchantCurrency currency;
        [Min(1)] public int price=1;
        [Tooltip("-1 means all heroes. Warrior 0, Archer 1, Mage 2.")] public int hero=-1;
        public CardData card;
        public string cardId;
        public string cosmeticTitle;
        public Color titleColor=Color.white;
        public Sprite[] cosmeticFrames;
        [Min(1f)] public float cosmeticFramesPerSecond=12f;
        public bool AvailableFor(int character) => (hero < 0 || hero == character) &&
            (kind != MerchantItemKind.Card || GameplayCardCatalog.CanObtain(card));
    }
    public List<Offer> offers=new List<Offer>();
    private static SyntaxMerchantCatalog runtimeCatalog;
    private static int contentVersion = -1;
    private static List<Offer> authoredOffers;
    public static SyntaxMerchantCatalog Load()
    {
        if (runtimeCatalog == null)
        {
            var authored = Resources.Load<SyntaxMerchantCatalog>("NPC/SyntaxMerchantCatalog");
            if (authored == null) return null;
            runtimeCatalog = Instantiate(authored);
            authoredOffers = new List<Offer>(runtimeCatalog.offers);
        }
        var catalog = runtimeCatalog;
        if (contentVersion != GameContentSync.Version)
        {
            catalog.offers = authoredOffers.FindAll(o => o == null || o.kind != MerchantItemKind.Card || GameContentSync.Metadata(GameplayCardCatalog.PrimaryId(o.card)) == null);
            foreach (var row in GameContentSync.ShopCards)
                if (!catalog.offers.Exists(o => o != null && o.kind == MerchantItemKind.Card &&
                    GameplayCardCatalog.PrimaryId(o.card) == row.game_card_key))
                    catalog.offers.Add(new Offer { id = "remote.offer." + row.game_card_key, cardId = row.game_card_key,
                        card = GameplayCardCatalog.FindById(row.game_card_key), hero = GameContentSync.HeroFor(row.game_card_key),
                        kind = MerchantItemKind.Card, currency = authoredOffers.Find(o => o != null && o.kind == MerchantItemKind.Card && GameplayCardCatalog.PrimaryId(o.card) == row.game_card_key)?.currency ?? MerchantCurrency.Coins, price = row.shop_price });
            contentVersion = GameContentSync.Version;
        }
        if (catalog != null)
            foreach (var offer in catalog.offers)
                if (offer != null && offer.kind == MerchantItemKind.Card)
                {
                    offer.card = GameplayCardCatalog.Resolve(offer.card);
                    if (offer.card != null)
                    {
                        offer.title = offer.card.name;
                        offer.description = offer.card.Description;
                    }
                }
        return catalog;
    }
    public Offer Find(string id) => offers.Find(o=>o!=null&&o.id==id);
    public static string CurrencyName(MerchantCurrency currency) => currency==MerchantCurrency.Coins?"coins":currency==MerchantCurrency.LibraryTokens?"Library Tokens":"syntax shards";
    public const int ScrapsPerCard=3;
    public const int ScrapsPerToken=3;
}
