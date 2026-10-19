using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Inventory Card Catalog")]
public class InventoryCardCatalog : ScriptableObject
{
    public List<CharacterInventory.CatalogEntry> cards = new List<CharacterInventory.CatalogEntry>();
}
