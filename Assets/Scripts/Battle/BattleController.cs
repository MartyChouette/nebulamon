
// Assets/Scripts/Battle/BattleController.cs
using System.Collections;
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

        private BattleSide _player = new();
        private BattleSide _enemy = new();

        private bool _battleEnded = false;

        private enum PlayerAction { None, Move, Draw, Run }
        private PlayerAction _playerAction = PlayerAction.None;
        private MoveDefinition _playerMove;

        private MonsterInstance _lastPlayerActive;
        private MonsterInstance _lastEnemyActive;

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

            // Keep menus visible; start disabled until first player turn actually begins
            ui?.ShowRootMenu();
            ui?.HideMovesMenu();
            ui?.SetPlayerInputEnabled(false);

            // VFX setup (optional)
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

            // Force who acts first (so you SEE buttons immediately if desired)
            ResetInitiative(_player.Active, _enemy.Active);
            if (playerAlwaysStarts && _player.Active != null)
            {
                _player.Active.initiative = Mathf.Max(1f, initiativeThreshold);
                if (_enemy.Active != null) _enemy.Active.initiative = 0f;
            }

            // ATB HUD
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

                // If neither is ready, fill meters
                AdvanceMetersUntilSomeoneActs(_player.Active, _enemy.Active);

                // Update ATB HUD meters + projected next turns
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

            // Player can act now: enable buttons
            ui?.SetTop("Your turn.");
            ui?.ShowRootMenu();
            ui?.HideMovesMenu();
            ui?.SetPlayerInputEnabled(true);

            ui?.WireRootButtons(
                onFight: () =>
                {
                    ui.ShowMoves4(
                        player,
                        onPicked: (m) =>
                        {
                            // After choosing, close moves menu and grey out during resolution
                            ui.HideMovesMenu();
                            ui.ShowRootMenu();
                            ui.SetPlayerInputEnabled(false);

                            _playerAction = PlayerAction.Move;
                            _playerMove = m;
                        },
                        onBack: () =>
                        {
                            // Back: moves menu OFF, root ON (still enabled because player turn)
                            ui.HideMovesMenu();
                            ui.ShowRootMenu();
                            ui.SetPlayerInputEnabled(true);
                        }
                    );
                },
                onDraw: () =>
                {
                    // Grey out during resolution
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
                }
            );

            while (_playerAction == PlayerAction.None && !_battleEnded)
                yield return null;

            if (_battleEnded) yield break;

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
            }
        }

        private IEnumerator HandleEnemyTurn()
        {
            var enemy = _enemy.Active;
            var player = _player.Active;
            if (enemy == null || player == null) yield break;

            // Enemy turn: KEEP menus visible, just grey them out
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

            ui?.SetTop($"{drawer.def.displayName} drew energy!");
            yield return new WaitForSeconds(0.25f);

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
            yield return new WaitForSeconds(0.45f);

            turnHud?.Refresh(_player.Active, _enemy.Active);
        }

        private IEnumerator TakeTurn(bool attackerIsPlayer, MoveDefinition chosenMove)
        {
            var atk = attackerIsPlayer ? _player.Active : _enemy.Active;
            var def = attackerIsPlayer ? _enemy.Active : _player.Active;

            if (atk == null || def == null || atk.IsDead) yield break;

            if (!atk.CanActThisTurn(out string gateMsg, out bool selfHit))
            {
                ui?.SetTop(gateMsg);
                yield return new WaitForSeconds(0.45f);
                yield break;
            }

            if (selfHit)
            {
                int selfDmg = Mathf.Max(1, Mathf.RoundToInt(atk.def.maxHP * 0.12f));
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

            // Resolution: keep menus visible but disabled
            ui?.ShowRootMenu();
            ui?.HideMovesMenu();
            ui?.SetPlayerInputEnabled(false);

            if (!atk.pool.CanAfford(move))
            {
                ui?.SetTop("Not enough element energy!");
                yield return new WaitForSeconds(0.45f);
                yield break;
            }

            atk.pool.Spend(move);
            if (attackerIsPlayer) ui?.SetResources(atk);

            ui?.SetTop($"{atk.def.displayName} used {move.moveName}!");
            yield return new WaitForSeconds(0.35f);

            float hitChance = BattleMath.FinalHitChance(atk.def, def.def, move.accuracy);
            if (Random.value > hitChance)
            {
                ui?.SetTop("It missed!");
                yield return new WaitForSeconds(0.45f);
                yield break;
            }

            if (move.kind == MoveKind.Heal || (move.category == MoveCategory.Support && move.healAmount > 0))
            {
                atk.hp = Mathf.Min(atk.def.maxHP, atk.hp + Mathf.Max(0, move.healAmount));
                ui?.SetHP(_player.Active, _enemy.Active);

                ui?.SetTop($"{atk.def.displayName} restored HP!");
                if (vfx != null)
                {
                    yield return attackerIsPlayer
                        ? vfx.PlayHeal(vfx.playerMonster)
                        : vfx.PlayHeal(vfx.enemyMonster);
                }

                yield return new WaitForSeconds(0.25f);
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

            bool crit = Random.value < BattleMath.FinalCritChance(atk.def, move.critChance);
            int dmg = BattleMath.CalcDamage(move, atk.def, def.def, crit);

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

            if (crit) { ui?.SetTop("Critical hit!"); yield return new WaitForSeconds(0.35f); }
            if (strongHit) { ui?.SetTop("It's super effective!"); yield return new WaitForSeconds(0.35f); }
            if (weakHit) { ui?.SetTop("It's not very effective..."); yield return new WaitForSeconds(0.35f); }

            yield return TryApplyMoveStatus(atk, def, move);

            turnHud?.Refresh(_player.Active, _enemy.Active);
        }

        private IEnumerator TryApplyMoveStatus(MonsterInstance atk, MonsterInstance def, MoveDefinition move)
        {
            if (move == null || !move.status.enabled) yield break;

            var target = move.status.applyToSelf ? atk : def;
            float baseChance = Mathf.Clamp01(move.status.applyChance);
            if (baseChance <= 0f) yield break;

            float chance = BattleMath.StatusApplyChance(atk.def, target.def, baseChance);
            if (Random.value > chance) yield break;

            int turns = BattleMath.StatusDurationAfterResolve(atk.def, target.def, move.status.durationTurns);

            if (target.TryApplyStatus(move.status.type, turns, move.status.potency, out string msg))
            {
                ui?.SetTop(msg);
                yield return new WaitForSeconds(0.45f);
            }
        }

        private IEnumerator TickActorStatusEndOfTurn(MonsterInstance actor)
        {
            if (actor == null || actor.IsDead) yield break;

            actor.TickEndOfTurn(out string msg);
            if (!string.IsNullOrEmpty(msg))
            {
                ui?.SetTop(msg);
                yield return new WaitForSeconds(0.35f);
            }

            turnHud?.Refresh(_player.Active, _enemy.Active);
        }

        private IEnumerator EndBattle(bool won)
        {
            ui?.SetPlayerInputEnabled(false);
            ui?.SetTop(won ? "You won!" : "You lost...");
            yield return new WaitForSeconds(0.7f);

            if (vfx != null) yield return vfx.PlayOutro();

            _battleEnded = true;
            GameFlowManager.Instance.ReturnToOverworld();
        }

        private IEnumerator RunAndExit()
        {
            ui?.SetPlayerInputEnabled(false);
            ui?.SetTop("Got away safely.");
            yield return new WaitForSeconds(0.5f);

            if (vfx != null) yield return vfx.PlayOutro();

            GameFlowManager.Instance.ReturnToOverworld();
        }

        // ---------------- Enemy AI ----------------
        private MoveDefinition PickEnemyMove(MonsterInstance enemy)
        {
            if (enemy?.def?.moves == null || enemy.def.moves.Count == 0) return null;

            for (int tries = 0; tries < 16; tries++)
            {
                var m = enemy.def.moves[Random.Range(0, enemy.def.moves.Count)];
                if (m != null && enemy.pool.CanAfford(m)) return m;
            }

            for (int i = 0; i < enemy.def.moves.Count; i++)
            {
                var m = enemy.def.moves[i];
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

            int guard = 0;
            while (guard++ < 10000 && player.initiative < th && enemy.initiative < th)
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

            // Update ATB HUD after spending initiative
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

