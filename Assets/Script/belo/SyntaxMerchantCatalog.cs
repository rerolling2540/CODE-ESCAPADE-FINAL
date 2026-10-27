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
        public bool AvailableFor(int character) => hero < 0 || hero == character;
    }
    public List<Offer> offers=new List<Offer>();
    public static SyntaxMerchantCatalog Load() => Resources.Load<SyntaxMerchantCatalog>("NPC/SyntaxMerchantCatalog");
    public Offer Find(string id) => offers.Find(o=>o!=null&&o.id==id);
    public static string CurrencyName(MerchantCurrency currency) => currency==MerchantCurrency.Coins?"coins":currency==MerchantCurrency.LibraryTokens?"Library Tokens":"syntax shards";
    public const int ScrapsPerCard=3;
    public const int ScrapsPerToken=3;
}
