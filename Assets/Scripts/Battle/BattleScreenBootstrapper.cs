// Assets/Scripts/Battle/BattleScreenBootstrapper.cs
using UnityEngine;

namespace Nebula
{
    public class BattleScreenBootstrapper : MonoBehaviour
    {
        [Header("Refs")]
        public BattleController battle;

        [Header("Player Party Setup")]
        public MonsterDefinition fallbackPlayerMonster;
        public Sprite playerPilotSprite;
        public Sprite playerShipSprite;

        [Header("Catalog (optional)")]
        public MonsterCatalog monsterCatalog;

        private void Start()
        {
            if (monsterCatalog != null)
                MonsterCatalog.Instance = monsterCatalog;

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

            // Build player side from roster (or fallback)
            var playerSide = BuildPlayerSide();
            if (playerSide == null || playerSide.party.Count == 0)
            {
                Debug.LogError("No player monsters available. Assign fallbackPlayerMonster.");
                flow.ReturnToOverworld();
                return;
            }

            // Build enemy side
            var enemySide = BuildEnemySide(enemyDef);
            if (enemySide.party.Count == 0)
            {
                Debug.LogError("EnemyDefinition has no monsters in party.");
                flow.ReturnToOverworld();
                return;
            }

            // Pass reward/trainer info to controller
            battle.rewardMoney = enemyDef.rewardMoney;
            battle.trainerId = enemyDef.trainerId;
            battle.isTrainerBattle = enemyDef.isTrainer;

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

        private BattleSide BuildPlayerSide()
        {
            var side = new BattleSide();

            if (!Progression.IsLoaded)
            {
                try { Progression.Load(); } catch { /* fallback below */ }
            }

            if (Progression.IsLoaded)
            {
                var partyEntries = Progression.GetParty();
                var catalog = MonsterCatalog.Instance;

                if (partyEntries.Count > 0 && catalog != null)
                {
                    foreach (var entry in partyEntries)
                    {
                        var def = catalog.GetByMonsterId(entry.monsterId);
                        if (def == null) continue;

                        var inst = new MonsterInstance(def, entry.level);
                        inst.xp = entry.xp;

                        if (entry.currentHp > 0 && entry.currentHp <= inst.EffectiveMaxHP())
                            inst.hp = entry.currentHp;

                        side.party.Add(inst);
                    }

                    if (side.party.Count > 0) return side;
                }
            }

            // Fallback: use inspector-assigned monster
            if (fallbackPlayerMonster != null)
                side.party.Add(new MonsterInstance(fallbackPlayerMonster));

            return side;
        }

        private BattleSide BuildEnemySide(EnemyDefinition enemyDef)
        {
            var side = new BattleSide();

            int enemyLevel = Mathf.Max(1, enemyDef.threatLevel);

            if (enemyDef.party != null)
            {
                for (int i = 0; i < enemyDef.party.Count && side.party.Count < 3; i++)
                {
                    if (enemyDef.party[i] != null)
                        side.party.Add(new MonsterInstance(enemyDef.party[i], enemyLevel));
                }
            }

            if (side.party.Count == 0)
            {
                var first = enemyDef.GetFirstValidMonster();
                if (first != null)
                    side.party.Add(new MonsterInstance(first, enemyLevel));
            }

            return side;
        }
    }
}
