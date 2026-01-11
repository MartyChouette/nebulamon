// Assets/Scripts/Battle/BattleScreenBootstrapper.cs
using Nebula;
using UnityEngine;

namespace Nebula
{
    public class BattleScreenBootstrapper : MonoBehaviour
    {
        [Header("Refs")]
        public BattleController battle;

        [Header("Player Party Setup")]
        public MonsterDefinition fallbackPlayerMonster; // used if no starter chosen
        public Sprite playerPilotSprite;
        public Sprite playerShipSprite;

        private void Start()
        {
            var flow = GameFlowManager.Instance;
            if (flow == null)
            {
                Debug.LogError("No GameFlowManager found. Battle cannot start.");
                return;
            }

            var enemyDef = flow.PendingEnemy;
            if (enemyDef == null)
            {
                Debug.LogWarning("No PendingEnemy. Using fallback enemy (none). Returning.");
                flow.ReturnToOverworld();
                return;
            }

            // Build player side (MVP = just 1 monster now)
            MonsterDefinition playerMonster = ResolvePlayerMonster();
            if (playerMonster == null)
            {
                Debug.LogError("No player monster available. Assign fallbackPlayerMonster.");
                flow.ReturnToOverworld();
                return;
            }

            var playerSide = new BattleSide();
            playerSide.party.Add(new MonsterInstance(playerMonster));

            // Build enemy side (supports up to 3 in the ScriptableObject list)
            var enemySide = new BattleSide();
            if (enemyDef.party != null)
            {
                for (int i = 0; i < enemyDef.party.Count && enemySide.party.Count < 3; i++)
                {
                    if (enemyDef.party[i] != null)
                        enemySide.party.Add(new MonsterInstance(enemyDef.party[i]));
                }
            }

            // ensure at least one
            if (enemySide.party.Count == 0)
            {
                var first = enemyDef.GetFirstValidMonster();
                if (first != null) enemySide.party.Add(new MonsterInstance(first));
            }

            if (enemySide.party.Count == 0)
            {
                Debug.LogError("EnemyDefinition has no monsters in party.");
                flow.ReturnToOverworld();
                return;
            }

            battle.defaultPlayerPilot = playerPilotSprite;
            battle.defaultPlayerShip = playerShipSprite;

            battle.StartBattle(
                playerSide,
                enemySide,
                playerPilotSprite,
                playerShipSprite,
                enemyDef.enemyPilotSprite,
                enemyDef.enemyShipSprite
            );
        }

        private MonsterDefinition ResolvePlayerMonster()
        {
            // MVP: use your Progression starter monster if you later wire a catalog.
            // For now: just return fallback.
            return fallbackPlayerMonster;
        }
    }
}
