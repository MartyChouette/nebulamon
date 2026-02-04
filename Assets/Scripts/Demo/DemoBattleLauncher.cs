using UnityEngine;

namespace Nebula
{
    /// <summary>
    /// Demo scene: picks a random wild encounter from an EncounterTable
    /// and launches the BattleScreen via GameFlowManager.
    /// </summary>
    public class DemoBattleLauncher : MonoBehaviour
    {
        [SerializeField] private EncounterTable encounterTable;
        [SerializeField] private MonsterDefinition fallbackMonster;
        [SerializeField] private MonsterCatalog monsterCatalog;

        void Start()
        {
            // Ensure singletons exist
            if (GameFlowManager.Instance == null)
            {
                var go = new GameObject("GameFlowManager");
                go.AddComponent<GameFlowManager>();
            }

            if (monsterCatalog != null)
                MonsterCatalog.Instance = monsterCatalog;

            // Pick an enemy from the encounter table
            EnemyDefinition enemy = PickRandomEnemy();

            if (enemy == null)
            {
                Debug.LogWarning("[DemoBattleLauncher] No enemy found. Creating fallback.");
                enemy = CreateFallbackEnemy();
            }

            // Create a dummy rigidbody for the return payload
            var dummyGo = new GameObject("DummyPlayer");
            var rb = dummyGo.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            GameFlowManager.Instance.StartBattle(enemy, rb, dummyGo.transform);
        }

        EnemyDefinition PickRandomEnemy()
        {
            if (encounterTable == null || encounterTable.entries == null || encounterTable.entries.Count == 0)
                return null;

            // Weighted random pick
            float totalWeight = 0f;
            foreach (var e in encounterTable.entries)
                totalWeight += e.weight;

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var e in encounterTable.entries)
            {
                cumulative += e.weight;
                if (roll <= cumulative && e.enemy != null)
                    return e.enemy;
            }

            // Fallback to first valid entry
            foreach (var e in encounterTable.entries)
            {
                if (e.enemy != null) return e.enemy;
            }

            return null;
        }

        EnemyDefinition CreateFallbackEnemy()
        {
            var enemy = ScriptableObject.CreateInstance<EnemyDefinition>();
            enemy.enemyId = "demo_wild";
            enemy.displayName = "Wild Monster";
            enemy.threatLevel = 1;
            enemy.rewardMoney = 25;

            if (fallbackMonster != null)
                enemy.party.Add(fallbackMonster);

            return enemy;
        }
    }
}
