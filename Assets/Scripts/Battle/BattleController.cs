
// Assets/Scripts/Battle/BattleController.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nebula
{
    public class BattleController : MonoBehaviour
    {
        [Header("Refs")]
        public BattleUI ui;
        public BattleVfxController vfx;

        [Header("ATB HUD")]
        public BattleTurnHudUI turnHud;

        [Header("Balance Config")]
        [Tooltip("Assign a BattleConfig asset. Sets BattleConfig.Instance on Awake.")]
        [SerializeField] private BattleConfig battleConfig;

        [Header("Player Presentation (fallbacks)")]
        public Sprite defaultPlayerPilot;
        public Sprite defaultPlayerShip;

        [Header("CTB / Initiative")]
        [Min(10f)] public float initiativeThreshold = 100f;
        [Range(0f, 1f)] public float leftoverClamp01 = 0.25f;

        [Header("Turn Start Rules")]
        public bool playerAlwaysStarts = true;

        [Header("Element Economy")]
        public bool gainOwnElementEachTurn = true;
        public Vector2Int drawGainRange = new Vector2Int(1, 3);
        [Range(0f, 1f)] public float drawBonusChance = 0.35f;

        [Header("Switch / Item Pickers (optional)")]
        public BattlePartyPickerUI partyPicker;
        public BattleItemPickerUI itemPicker;

        // Battle reward info (set by bootstrapper)
        [HideInInspector] public int rewardMoney;
        [HideInInspector] public string trainerId;
        [HideInInspector] public bool isTrainerBattle;

        private BattleSide _player = new();
        private BattleSide _enemy = new();

        private bool _battleEnded = false;

        private enum PlayerAction { None, Move, Draw, Run, Switch, Item }
        private PlayerAction _playerAction = PlayerAction.None;
        private MoveDefinition _playerMove;
        private int _switchIndex = -1;
        private ItemDefinition _playerItem;

        private MonsterInstance _lastPlayerActive;
        private MonsterInstance _lastEnemyActive;

        private void Awake()
        {
            if (battleConfig != null)
                BattleConfig.Instance = battleConfig;
        }

        public void StartBattle(
            BattleSide playerSide,
            BattleSide enemySide,
            Sprite playerPilot,
            Sprite playerShip,
            Sprite enemyPilot,
            Sprite enemyShip)
        {
            _player = playerSide ?? new BattleSide();
            _enemy = enemySide ?? new BattleSide();

            _battleEnded = false;

            ui?.ShowRootMenu();
            ui?.HideMovesMenu();
            ui?.SetPlayerInputEnabled(false);

            if (vfx != null)
            {
                vfx.SetupSprites(
                    playerPilot != null ? playerPilot : defaultPlayerPilot,
                    playerShip != null ? playerShip : defaultPlayerShip,
                    enemyPilot,
                    enemyShip,
                    _player.Active?.def?.battleSprite,
                    _enemy.Active?.def?.battleSprite
                );
                RestartAnimIfNeeded(force: true);
                StartCoroutine(vfx.PlayIntro());
            }

            ResetInitiative(_player.Active, _enemy.Active);
            if (playerAlwaysStarts && _player.Active != null)
            {
                _player.Active.initiative = Mathf.Max(1f, initiativeThreshold);
                if (_enemy.Active != null) _enemy.Active.initiative = 0f;
            }

            turnHud?.Configure(initiativeThreshold, leftoverClamp01);
            turnHud?.Refresh(_player.Active, _enemy.Active);

            StartCoroutine(BattleLoop());
        }

        private IEnumerator BattleLoop()
        {
            while (!_battleEnded)
            {
                if (_player.AllDead) { yield return EndBattle(false); yield break; }
                if (_enemy.AllDead) { yield return EndBattle(true); yield break; }

                if (_player.Active == null || _player.Active.IsDead) _player.TryAdvanceToNextAlive();
                if (_enemy.Active == null || _enemy.Active.IsDead) _enemy.TryAdvanceToNextAlive();

                RestartAnimIfNeeded(force: false);

                ui?.SetHP(_player.Active, _enemy.Active);
                ui?.SetResources(_player.Active);

                AdvanceMetersUntilSomeoneActs(_player.Active, _enemy.Active);
                turnHud?.Refresh(_player.Active, _enemy.Active);

                bool playerActs = PlayerActsNext(_player.Active, _enemy.Active);

                if (playerActs) yield return HandlePlayerTurn();
                else yield return HandleEnemyTurn();
            }
        }

        private IEnumerator HandlePlayerTurn()
        {
            var player = _player.Active;
            var enemy = _enemy.Active;
            if (player == null || enemy == null) yield break;

            if (gainOwnElementEachTurn)
                player.pool.Add(player.def.element, 1);

            ui?.SetResources(player);
            turnHud?.Refresh(_player.Active, _enemy.Active);

            _playerAction = PlayerAction.None;
            _playerMove = null;
            _switchIndex = -1;
            _playerItem = null;

            ui?.SetTop("Your turn.");
            ui?.ShowRootMenu();
            ui?.HideMovesMenu();
            ui?.SetPlayerInputEnabled(true);

            // Determine if switch/item are available
            bool canSwitch = _player.AliveCount > 1;
            bool canUseItems = true;

            ui?.WireRootButtons(
                onFight: () =>
                {
                    ui.ShowMoves4(
                        player,
                        onPicked: (m) =>
                        {
                            ui.HideMovesMenu();
                            ui.ShowRootMenu();
                            ui.SetPlayerInputEnabled(false);
                            _playerAction = PlayerAction.Move;
                            _playerMove = m;
                        },
                        onBack: () =>
                        {
                            ui.HideMovesMenu();
                            ui.ShowRootMenu();
                            ui.SetPlayerInputEnabled(true);
                        }
                    );
                },
                onDraw: () =>
                {
                    ui?.HideMovesMenu();
                    ui?.ShowRootMenu();
                    ui?.SetPlayerInputEnabled(false);
                    _playerAction = PlayerAction.Draw;
                },
                onRun: () =>
                {
                    ui?.HideMovesMenu();
                    ui?.ShowRootMenu();
                    ui?.SetPlayerInputEnabled(false);
                    _playerAction = PlayerAction.Run;
                },
                onSwitch: canSwitch ? () =>
                {
                    ui?.SetPlayerInputEnabled(false);
                    if (partyPicker != null)
                    {
                        partyPicker.Show(_player, _player.activeIndex,
                            onPicked: (idx) =>
                            {
                                _switchIndex = idx;
                                _playerAction = PlayerAction.Switch;
                            },
                            onBack: () =>
                            {
                                ui?.SetPlayerInputEnabled(true);
                            }
                        );
                    }
                } : null,
                onItem: canUseItems ? () =>
                {
                    ui?.SetPlayerInputEnabled(false);
                    if (itemPicker != null)
                    {
                        itemPicker.Show(
                            onPicked: (item) =>
                            {
                                _playerItem = item;
                                _playerAction = PlayerAction.Item;
                            },
                            onBack: () =>
                            {
                                ui?.SetPlayerInputEnabled(true);
                            }
                        );
                    }
                } : null
            );

            while (_playerAction == PlayerAction.None && !_battleEnded)
                yield return null;

            if (_battleEnded) yield break;

            var cfg = BattleConfig.Instance;

            switch (_playerAction)
            {
                case PlayerAction.Move:
                    yield return TakeTurn(attackerIsPlayer: true, _playerMove);
                    ConsumeTurn(player);
                    yield return TickActorStatusEndOfTurn(player);
                    break;

                case PlayerAction.Draw:
                    yield return DoDrawAction(player, enemy);
                    ConsumeTurn(player);
                    yield return TickActorStatusEndOfTurn(player);
                    break;

                case PlayerAction.Run:
                    yield return RunAndExit();
                    _battleEnded = true;
                    break;

                case PlayerAction.Switch:
                    _player.SwitchTo(_switchIndex);
                    RestartAnimIfNeeded(force: true);
                    ui?.SetHP(_player.Active, _enemy.Active);
                    ui?.SetResources(_player.Active);
                    ui?.SetTop($"Go, {_player.Active.def.displayName}!");
                    yield return new WaitForSeconds(cfg != null ? cfg.moveAnnounceDelay : 0.35f);
                    ConsumeTurn(player);
                    break;

                case PlayerAction.Item:
                    yield return HandleItemUse();
                    if (!_battleEnded)
                        ConsumeTurn(player);
                    break;
            }
        }

        private IEnumerator HandleItemUse()
        {
            if (_playerItem == null) yield break;

            var cfg = BattleConfig.Instance;
            float delay = cfg != null ? cfg.moveAnnounceDelay : 0.35f;

            if (_playerItem.category == ItemCategory.Heal)
            {
                var target = _player.Active;
                target.hp = Mathf.Min(target.EffectiveMaxHP(), target.hp + _playerItem.healAmount);

                if (_playerItem.healStatus && target.majorStatus.HasValue)
                    target.majorStatus = null;

                Progression.RemoveItem(_playerItem.itemId);

                ui?.SetHP(_player.Active, _enemy.Active);
                ui?.SetTop($"Used {_playerItem.displayName}! Restored HP!");
                yield return new WaitForSeconds(delay);
            }
            else if (_playerItem.category == ItemCategory.CatchDevice)
            {
                yield return HandleCatchAttempt();
            }
        }

        private IEnumerator HandleCatchAttempt()
        {
            if (_playerItem == null) yield break;

            var cfg = BattleConfig.Instance;
            float delay = cfg != null ? cfg.moveAnnounceDelay : 0.35f;

            if (isTrainerBattle)
            {
                ui?.SetTop("Can't catch a trainer's monster!");
                yield return new WaitForSeconds(delay);
                yield break;
            }

            int maxRoster = cfg != null ? cfg.maxRosterSize : 30;
            if (Progression.RosterCount >= maxRoster)
            {
                ui?.SetTop("Roster is full!");
                yield return new WaitForSeconds(delay);
                yield break;
            }

            Progression.RemoveItem(_playerItem.itemId);

            var target = _enemy.Active;
            float chance = BattleMath.CalcCatchChance(target, _playerItem.catchRateBonus);

            ui?.SetTop($"Threw {_playerItem.displayName}!");
            yield return new WaitForSeconds(delay);

            if (Random.value <= chance)
            {
                ui?.SetTop($"Caught {target.def.displayName}!");
                yield return new WaitForSeconds(delay);

                var owned = new ProgressionData.OwnedMonster
                {
                    monsterId = target.def.monsterId,
                    level = target.level,
                    xp = target.xp,
                    currentHp = target.hp
                };
                if (target.knownMoves != null)
                {
                    foreach (var m in target.knownMoves)
                    {
                        if (m != null) owned.knownMoveNames.Add(m.moveName);
                    }
                }

                Progression.AddToRoster(owned);
                Progression.MarkCaught(target.def.monsterId);
                PersistPlayerPartyToRoster();
                Progression.Save();

                _battleEnded = true;
                if (vfx != null) yield return vfx.PlayOutro();
                GameFlowManager.Instance.ReturnToOverworld();
            }
            else
            {
                ui?.SetTop("It broke free!");
                yield return new WaitForSeconds(delay);
            }
        }

        private IEnumerator HandleEnemyTurn()
        {
            var enemy = _enemy.Active;
            var player = _player.Active;
            if (enemy == null || player == null) yield break;

            ui?.ShowRootMenu();
            ui?.HideMovesMenu();
            ui?.SetPlayerInputEnabled(false);
            turnHud?.Refresh(_player.Active, _enemy.Active);

            if (gainOwnElementEachTurn)
                enemy.pool.Add(enemy.def.element, 1);

            MoveDefinition move = PickEnemyMove(enemy);

            if (move == null)
            {
                yield return DoDrawAction(enemy, player);
                ConsumeTurn(enemy);
                yield return TickActorStatusEndOfTurn(enemy);
            }
            else
            {
                yield return TakeTurn(attackerIsPlayer: false, move);
                ConsumeTurn(enemy);
                yield return TickActorStatusEndOfTurn(enemy);
            }
        }

        private IEnumerator DoDrawAction(MonsterInstance drawer, MonsterInstance opponent)
        {
            if (drawer == null || opponent == null) yield break;

            var cfg = BattleConfig.Instance;

            ui?.SetTop($"{drawer.def.displayName} drew energy!");
            yield return new WaitForSeconds(cfg != null ? cfg.drawDelay : 0.25f);

            int min = Mathf.Min(drawGainRange.x, drawGainRange.y);
            int max = Mathf.Max(drawGainRange.x, drawGainRange.y);
            int gain = Random.Range(min, max + 1);

            drawer.pool.Add(opponent.def.element, gain);

            if (Random.value < drawBonusChance)
            {
                var bonus = (ElementType)Random.Range(0, 4);
                drawer.pool.Add(bonus, 1);
            }

            if (drawer == _player.Active)
                ui?.SetResources(drawer);

            ui?.SetTop($"Gained {gain} {opponent.def.element}.");
            yield return new WaitForSeconds(cfg != null ? cfg.drawResultDelay : 0.45f);

            turnHud?.Refresh(_player.Active, _enemy.Active);
        }

        private IEnumerator TakeTurn(bool attackerIsPlayer, MoveDefinition chosenMove)
        {
            var atk = attackerIsPlayer ? _player.Active : _enemy.Active;
            var def = attackerIsPlayer ? _enemy.Active : _player.Active;

            if (atk == null || def == null || atk.IsDead) yield break;

            var cfg = BattleConfig.Instance;

            if (!atk.CanActThisTurn(out string gateMsg, out bool selfHit))
            {
                ui?.SetTop(gateMsg);
                yield return new WaitForSeconds(cfg != null ? cfg.actionBlockedDelay : 0.45f);
                yield break;
            }

            if (selfHit)
            {
                float selfPct = cfg != null ? cfg.confuseSelfHitPercent : 0.12f;
                int selfDmg = Mathf.Max(1, Mathf.RoundToInt(atk.EffectiveMaxHP() * selfPct));
                atk.hp = Mathf.Max(0, atk.hp - selfDmg);

                ui?.SetHP(_player.Active, _enemy.Active);

                if (vfx != null)
                {
                    if (attackerIsPlayer) yield return vfx.PlayHit(vfx.playerMonster, false, false, atk.def.element);
                    else yield return vfx.PlayHit(vfx.enemyMonster, false, false, atk.def.element);
                }

                turnHud?.Refresh(_player.Active, _enemy.Active);
                yield break;
            }

            yield return ExecuteMove(attackerIsPlayer, chosenMove);
        }

        private IEnumerator ExecuteMove(bool attackerIsPlayer, MoveDefinition move)
        {
            if (move == null) yield break;

            var atk = attackerIsPlayer ? _player.Active : _enemy.Active;
            var def = attackerIsPlayer ? _enemy.Active : _player.Active;
            if (atk == null || def == null) yield break;

            var cfg = BattleConfig.Instance;

            ui?.ShowRootMenu();
            ui?.HideMovesMenu();
            ui?.SetPlayerInputEnabled(false);

            if (!atk.pool.CanAfford(move))
            {
                ui?.SetTop("Not enough element energy!");
                yield return new WaitForSeconds(cfg != null ? cfg.notEnoughEnergyDelay : 0.45f);
                yield break;
            }

            atk.pool.Spend(move);
            if (attackerIsPlayer) ui?.SetResources(atk);

            ui?.SetTop($"{atk.def.displayName} used {move.moveName}!");
            yield return new WaitForSeconds(cfg != null ? cfg.moveAnnounceDelay : 0.35f);

            if (move.kind == MoveKind.Heal || (move.category == MoveCategory.Support && move.healAmount > 0))
            {
                atk.hp = Mathf.Min(atk.EffectiveMaxHP(), atk.hp + Mathf.Max(0, move.healAmount));
                ui?.SetHP(_player.Active, _enemy.Active);

                ui?.SetTop($"{atk.def.displayName} restored HP!");
                if (vfx != null)
                {
                    yield return attackerIsPlayer
                        ? vfx.PlayHeal(vfx.playerMonster)
                        : vfx.PlayHeal(vfx.enemyMonster);
                }

                yield return new WaitForSeconds(cfg != null ? cfg.healDelay : 0.25f);
                yield return TryApplyMoveStatus(atk, def, move);

                turnHud?.Refresh(_player.Active, _enemy.Active);
                yield break;
            }

            if (move.kind == MoveKind.Status && move.status.enabled)
            {
                yield return TryApplyMoveStatus(atk, def, move);
                turnHud?.Refresh(_player.Active, _enemy.Active);
                yield break;
            }

            float hitChance = BattleMath.FinalHitChance(atk, def, move.accuracy);
            if (Random.value > hitChance)
            {
                ui?.SetTop("It missed!");
                yield return new WaitForSeconds(cfg != null ? cfg.missDelay : 0.45f);
                yield break;
            }

            bool crit = Random.value < BattleMath.FinalCritChance(atk, move.critChance);
            int dmg = BattleMath.CalcDamage(move, atk, def, crit);

            def.hp = Mathf.Max(0, def.hp - dmg);

            float adv = BattleMath.AdvantageMultiplier(move.element, def.def.element);
            bool weakHit = adv < 1f;
            bool strongHit = adv > 1f;

            ui?.SetHP(_player.Active, _enemy.Active);

            if (vfx != null)
            {
                if (attackerIsPlayer)
                    yield return vfx.PlayHit(vfx.enemyMonster, crit, weakHit, move.element);
                else
                    yield return vfx.PlayHit(vfx.playerMonster, crit, weakHit, move.element);
            }

            if (crit) { ui?.SetTop("Critical hit!"); yield return new WaitForSeconds(cfg != null ? cfg.critDelay : 0.35f); }
            if (strongHit) { ui?.SetTop("It's super effective!"); yield return new WaitForSeconds(cfg != null ? cfg.superEffectiveDelay : 0.35f); }
            if (weakHit) { ui?.SetTop("It's not very effective..."); yield return new WaitForSeconds(cfg != null ? cfg.notEffectiveDelay : 0.35f); }

            yield return TryApplyMoveStatus(atk, def, move);

            turnHud?.Refresh(_player.Active, _enemy.Active);
        }

        private IEnumerator TryApplyMoveStatus(MonsterInstance atk, MonsterInstance def, MoveDefinition move)
        {
            if (move == null || !move.status.enabled) yield break;

            var target = move.status.applyToSelf ? atk : def;
            float baseChance = Mathf.Clamp01(move.status.applyChance);
            if (baseChance <= 0f) yield break;

            float chance = BattleMath.StatusApplyChance(atk, target, baseChance);
            if (Random.value > chance) yield break;

            int turns = BattleMath.StatusDurationAfterResolve(atk, target, move.status.durationTurns);

            var cfg = BattleConfig.Instance;

            if (target.TryApplyStatus(move.status.type, turns, move.status.potency, out string msg))
            {
                ui?.SetTop(msg);
                yield return new WaitForSeconds(cfg != null ? cfg.statusApplyDelay : 0.45f);
            }
        }

        private IEnumerator TickActorStatusEndOfTurn(MonsterInstance actor)
        {
            if (actor == null || actor.IsDead) yield break;

            var cfg = BattleConfig.Instance;

            actor.TickEndOfTurn(out string msg);
            if (!string.IsNullOrEmpty(msg))
            {
                ui?.SetTop(msg);
                yield return new WaitForSeconds(cfg != null ? cfg.statusTickDelay : 0.35f);
            }

            turnHud?.Refresh(_player.Active, _enemy.Active);
        }

        private IEnumerator EndBattle(bool won)
        {
            var cfg = BattleConfig.Instance;
            float delay = cfg != null ? cfg.battleEndDelay : 0.7f;

            ui?.SetPlayerInputEnabled(false);
            ui?.SetTop(won ? "You won!" : "You lost...");
            yield return new WaitForSeconds(delay);

            if (won)
            {
                // Mark enemy monsters as seen
                for (int i = 0; i < _enemy.party.Count; i++)
                {
                    var e = _enemy.party[i];
                    if (e?.def != null)
                        Progression.MarkSeen(e.def.monsterId);
                }

                // Award XP to alive party members
                for (int pi = 0; pi < _player.party.Count; pi++)
                {
                    var ally = _player.party[pi];
                    if (ally == null || ally.IsDead) continue;

                    int totalXp = 0;
                    for (int ei = 0; ei < _enemy.party.Count; ei++)
                    {
                        var e = _enemy.party[ei];
                        if (e?.def != null)
                            totalXp += BattleMath.CalcXpGain(e, ally.level);
                    }

                    if (totalXp > 0)
                    {
                        ui?.SetTop($"{ally.def.displayName} gained {totalXp} XP!");
                        yield return new WaitForSeconds(delay);

                        if (ally.TryGainXP(totalXp, out int levelsGained))
                        {
                            ui?.SetTop($"{ally.def.displayName} grew to Lv. {ally.level}!");
                            yield return new WaitForSeconds(delay);
                        }
                    }
                }

                // Award money
                if (rewardMoney > 0)
                {
                    Progression.AddMoney(rewardMoney);
                    ui?.SetTop($"Earned {rewardMoney} credits!");
                    yield return new WaitForSeconds(delay);
                }

                // Mark trainer defeated
                if (isTrainerBattle && !string.IsNullOrEmpty(trainerId))
                    Progression.MarkTrainerDefeated(trainerId);

                // Persist roster state
                PersistPlayerPartyToRoster();
            }

            if (vfx != null) yield return vfx.PlayOutro();

            _battleEnded = true;
            GameFlowManager.Instance.ReturnToOverworld();
        }

        private void PersistPlayerPartyToRoster()
        {
            if (!Progression.IsLoaded) return;
            var data = Progression.Data;
            if (data.partyIndices == null || data.roster == null) return;

            for (int pi = 0; pi < _player.party.Count && pi < data.partyIndices.Count; pi++)
            {
                int rosterIdx = data.partyIndices[pi];
                if (rosterIdx < 0 || rosterIdx >= data.roster.Count) continue;

                var entry = data.roster[rosterIdx];
                var inst = _player.party[pi];
                if (inst?.def == null) continue;

                entry.monsterId = inst.def.monsterId;
                entry.level = inst.level;
                entry.xp = inst.xp;
                entry.currentHp = inst.hp;
                entry.knownMoveNames.Clear();
                foreach (var m in inst.knownMoves)
                {
                    if (m != null) entry.knownMoveNames.Add(m.moveName);
                }
            }

            Progression.Save();
        }

        private IEnumerator RunAndExit()
        {
            var cfg = BattleConfig.Instance;

            ui?.SetPlayerInputEnabled(false);
            ui?.SetTop("Got away safely.");
            yield return new WaitForSeconds(cfg != null ? cfg.runAwayDelay : 0.5f);

            PersistPlayerPartyToRoster();

            if (vfx != null) yield return vfx.PlayOutro();

            GameFlowManager.Instance.ReturnToOverworld();
        }

        // ---------------- Enemy AI ----------------
        private MoveDefinition PickEnemyMove(MonsterInstance enemy)
        {
            if (enemy?.knownMoves == null || enemy.knownMoves.Count == 0) return null;

            var cfg = BattleConfig.Instance;
            int retries = cfg != null ? cfg.enemyAiRetries : 16;

            for (int tries = 0; tries < retries; tries++)
            {
                var m = enemy.knownMoves[Random.Range(0, enemy.knownMoves.Count)];
                if (m != null && enemy.pool.CanAfford(m)) return m;
            }

            for (int i = 0; i < enemy.knownMoves.Count; i++)
            {
                var m = enemy.knownMoves[i];
                if (m != null && enemy.pool.CanAfford(m)) return m;
            }

            return null;
        }

        // ---------------- CTB Helpers ----------------
        private void ResetInitiative(MonsterInstance a, MonsterInstance b)
        {
            if (a != null) a.initiative = 0f;
            if (b != null) b.initiative = 0f;
        }

        private void AdvanceMetersUntilSomeoneActs(MonsterInstance player, MonsterInstance enemy)
        {
            if (player == null || enemy == null) return;

            float th = Mathf.Max(1f, initiativeThreshold);

            var cfg = BattleConfig.Instance;
            int guard = cfg != null ? cfg.initiativeLoopGuard : 10000;

            int count = 0;
            while (count++ < guard && player.initiative < th && enemy.initiative < th)
            {
                player.initiative += Mathf.Max(1f, player.EffectiveSpeed());
                enemy.initiative += Mathf.Max(1f, enemy.EffectiveSpeed());
            }
        }

        private bool PlayerActsNext(MonsterInstance player, MonsterInstance enemy)
        {
            if (player == null || enemy == null) return true;

            if (player.initiative > enemy.initiative) return true;
            if (enemy.initiative > player.initiative) return false;

            int ps = player.EffectiveSpeed();
            int es = enemy.EffectiveSpeed();
            if (ps != es) return ps > es;

            return true;
        }

        private void ConsumeTurn(MonsterInstance actor)
        {
            if (actor == null) return;

            float th = Mathf.Max(1f, initiativeThreshold);
            actor.initiative -= th;

            float clamp = th * Mathf.Clamp01(leftoverClamp01);
            actor.initiative = Mathf.Clamp(actor.initiative, 0f, clamp);

            turnHud?.Refresh(_player.Active, _enemy.Active);
        }

        private void RestartAnimIfNeeded(bool force)
        {
            var p = _player.Active;
            var e = _enemy.Active;

            bool playerChanged = (p != _lastPlayerActive);
            bool enemyChanged = (e != _lastEnemyActive);

            if (force || playerChanged)
            {
                if (vfx != null) vfx.StartMonsterAnim(vfx.playerMonster, p?.def, true);
                _lastPlayerActive = p;
            }

            if (force || enemyChanged)
            {
                if (vfx != null) vfx.StartMonsterAnim(vfx.enemyMonster, e?.def, false);
                _lastEnemyActive = e;
            }
        }
    }
}
