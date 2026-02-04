using System.Collections.Generic;
using UnityEngine;

namespace Nebula
{
    [CreateAssetMenu(menuName = "Nebula/Data/Monster Catalog", fileName = "MonsterCatalog")]
    public class MonsterCatalog : ScriptableObject
    {
        public static MonsterCatalog Instance { get; set; }

        public List<MonsterDefinition> allMonsters = new();

        public MonsterDefinition GetByMonsterId(MonsterId id)
        {
            for (int i = 0; i < allMonsters.Count; i++)
            {
                if (allMonsters[i] != null && allMonsters[i].monsterId == id)
                    return allMonsters[i];
            }
            return null;
        }
    }
}
