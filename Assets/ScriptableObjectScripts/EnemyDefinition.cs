using UnityEngine;

namespace SpaceGame
{
    [CreateAssetMenu(menuName = "SpaceGame/Enemy Definition", fileName = "EnemyDefinition")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string enemyId = "pirate_drone";
        public string displayName = "Pirate Drone";

        [Header("Battle Content")]
        [Tooltip("Optional: prefab to spawn in the battle scene.")]
        public GameObject battlePrefab;

        [Tooltip("Optional: determines battle UI / scaling.")]
        public int threatLevel = 1;
    }
}
