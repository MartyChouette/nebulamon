using UnityEngine;

namespace Nebula
{
    /// <summary>
    /// Assigns catalog singletons in non-battle scenes (town, overworld).
    /// Add this component to a GameObject in any scene that needs
    /// MonsterCatalog or ItemCatalog access outside of BattleScreen.
    /// </summary>
    public class CatalogBootstrap : MonoBehaviour
    {
        [Tooltip("The project-wide MonsterCatalog asset.")]
        public MonsterCatalog monsterCatalog;

        [Tooltip("The project-wide ItemCatalog asset.")]
        public ItemCatalog itemCatalog;

        private void Awake()
        {
            if (monsterCatalog != null)
                MonsterCatalog.Instance = monsterCatalog;

            if (itemCatalog != null)
                ItemCatalog.Instance = itemCatalog;
        }
    }
}
