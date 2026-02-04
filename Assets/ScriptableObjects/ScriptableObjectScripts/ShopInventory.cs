using System.Collections.Generic;
using UnityEngine;

namespace Nebula
{
    [CreateAssetMenu(menuName = "Nebula/Data/Shop Inventory", fileName = "ShopInventory")]
    public class ShopInventory : ScriptableObject
    {
        public List<ItemDefinition> itemsForSale = new();
    }
}
