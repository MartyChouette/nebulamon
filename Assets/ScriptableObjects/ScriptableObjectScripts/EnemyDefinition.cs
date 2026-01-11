using System.Collections.Generic;
using UnityEngine;

namespace Nebula
{
    [CreateAssetMenu(menuName = "Nebula/Encounter/Enemy Definition", fileName = "EnemyDefinition")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string enemyId = "pirate_drone";
        public string displayName = "Pirate Drone";

        [Header("Battle Content")]
        [Tooltip("Optional: prefab to spawn in the battle scene.")]
        public GameObject battlePrefab;

        [Tooltip("Optional: determines battle difficulty/scaling.")]
        [Min(1)] public int threatLevel = 1;

        [Header("Battle Presentation (slide-in)")]
        public Sprite enemyPilotSprite;
        public Sprite enemyShipSprite;

        [Header("Enemy Party (1..3)")]
        public List<MonsterDefinition> party = new();

        public MonsterDefinition GetFirstValidMonster()
        {
            for (int i = 0; i < party.Count; i++)
                if (party[i] != null) return party[i];
            return null;
        }
    }
}
