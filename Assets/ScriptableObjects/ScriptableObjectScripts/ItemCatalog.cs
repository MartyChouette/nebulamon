using System.Collections.Generic;
using UnityEngine;

namespace Nebula
{
    [CreateAssetMenu(menuName = "Nebula/Data/Item Catalog", fileName = "ItemCatalog")]
    public class ItemCatalog : ScriptableObject
    {
        public static ItemCatalog Instance { get; set; }

        public List<ItemDefinition> allItems = new();

        public ItemDefinition GetById(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            for (int i = 0; i < allItems.Count; i++)
            {
                if (allItems[i] != null && allItems[i].itemId == itemId)
                    return allItems[i];
            }
            return null;
        }
    }
}
