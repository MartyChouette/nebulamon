using System.Collections.Generic;
using UnityEngine;

namespace Nebula
{
    [System.Serializable]
    public struct StatusInstance
    {
        public StatusType type;
        public int turnsRemaining;
        public float potency;

        public StatusInstance(StatusType type, int turns, float potency)
        {
            this.type = type;
            this.turnsRemaining = Mathf.Max(1, turns);
            this.potency = potency;
        }
    }

    public sealed class MonsterInstance
    {
        public MonsterDefinition def { get; private set; }
        public int hp;
        public int level;
        public int xp;

        // CTB meter
        public float initiative = 0f;

        // Element quantity pool
        public ElementPool pool;

        // Known moves (initialized from def.moves, can be modified)
        public List<MoveDefinition> knownMoves = new();

        // MVP: one major status at a time
        public StatusInstance? majorStatus;

        public bool IsDead => hp <= 0;
        public bool IsBerserk => HasStatus(StatusType.Berserk);

        public MonsterInstance(MonsterDefinition def, int level = 1)
        {
            this.def = def;
            this.level = Mathf.Max(1, level);
            this.xp = 0;

            hp = EffectiveMaxHP();

            var cfg = BattleConfig.Instance;
            int baseStock = cfg != null ? cfg.baseElementStock : 2;
            int nativeBonus = cfg != null ? cfg.nativeElementBonus : 2;

            pool = new ElementPool
            {
                solar = baseStock,
                voids = baseStock,
                bio = baseStock,
                time = baseStock
            };

            if (def != null)
            {
                pool.Add(def.element, nativeBonus);

                if (def.moves != null)
                {
                    foreach (var m in def.moves)
                    {
                        if (m != null)
                            knownMoves.Add(m);
                    }
                }
            }
        }

        // ── Effective Stats ──────────────────────────────────────

        public int EffectiveMaxHP()
        {
            if (def == null) return 1;
            return Mathf.Max(1, Mathf.RoundToInt(def.maxHP + def.hpGrowth * (level - 1)));
        }

        public int EffectivePhysAttack()
        {
            if (def == null) return 1;
            return Mathf.Max(1, Mathf.RoundToInt(def.physAttack + def.physAttackGrowth * (level - 1)));
        }

        public int EffectivePhysDefense()
        {
            if (def == null) return 1;
            return Mathf.Max(1, Mathf.RoundToInt(def.physDefense + def.physDefenseGrowth * (level - 1)));
        }

        public int EffectiveElemAttack()
        {
            if (def == null) return 1;
            return Mathf.Max(1, Mathf.RoundToInt(def.elemAttack + def.elemAttackGrowth * (level - 1)));
        }

        public int EffectiveElemDefense()
        {
            if (def == null) return 1;
            return Mathf.Max(1, Mathf.RoundToInt(def.elemDefense + def.elemDefenseGrowth * (level - 1)));
        }

        public int EffectiveAccuracy()
        {
            if (def == null) return 1;
            return Mathf.Max(1, Mathf.RoundToInt(def.accuracy + def.accuracyGrowth * (level - 1)));
        }

        public int EffectiveEvasion()
        {
            if (def == null) return 1;
            return Mathf.Max(1, Mathf.RoundToInt(def.evasion + def.evasionGrowth * (level - 1)));
        }

        public int EffectiveResolve()
        {
            if (def == null) return 1;
            return Mathf.Max(1, Mathf.RoundToInt(def.resolve + def.resolveGrowth * (level - 1)));
        }

        public int EffectiveLuck()
        {
            if (def == null) return 0;
            return Mathf.Max(0, Mathf.RoundToInt(def.luck + def.luckGrowth * (level - 1)));
        }

        // ── Status ───────────────────────────────────────────────

        public bool HasStatus(StatusType t) => majorStatus.HasValue && majorStatus.Value.type == t;

        public bool TryApplyStatus(StatusType t, int turns, float potency, out string msg)
        {
            msg = null;
            if (t == StatusType.None) return false;
            if (IsDead) return false;

            if (majorStatus.HasValue)
            {
                msg = $"{def.displayName} is already affected!";
                return false;
            }

            majorStatus = new StatusInstance(t, turns, potency);
            msg = t switch
            {
                StatusType.Sleep => $"{def.displayName} fell asleep!",
                StatusType.Dazed => $"{def.displayName} is dazed!",
                StatusType.Confused => $"{def.displayName} became confused!",
                StatusType.Berserk => $"{def.displayName} went berserk!",
                StatusType.Slow => $"{def.displayName} was slowed!",
                _ => $"{def.displayName} is affected!"
            };
            return true;
        }

        public void TickEndOfTurn(out string msg)
        {
            msg = null;
            if (!majorStatus.HasValue) return;

            var s = majorStatus.Value;
            s.turnsRemaining -= 1;

            if (s.turnsRemaining <= 0)
            {
                msg = s.type switch
                {
                    StatusType.Sleep => $"{def.displayName} woke up!",
                    StatusType.Dazed => $"{def.displayName} recovered!",
                    StatusType.Confused => $"{def.displayName} snapped out of it!",
                    StatusType.Berserk => $"{def.displayName} calmed down!",
                    StatusType.Slow => $"{def.displayName} is no longer slowed!",
                    _ => $"{def.displayName} recovered!"
                };
                majorStatus = null;
            }
            else
            {
                majorStatus = s;
            }
        }

        public int EffectiveSpeed()
        {
            int spd = def != null
                ? Mathf.Max(1, Mathf.RoundToInt(def.speed + def.speedGrowth * (level - 1)))
                : 1;

            if (HasStatus(StatusType.Slow))
            {
                var cfg = BattleConfig.Instance;
                float defaultPot = cfg != null ? cfg.defaultSlowPotency : 0.30f;
                float floor = cfg != null ? cfg.slowFloor : 0.30f;
                float ceiling = cfg != null ? cfg.slowCeiling : 0.90f;

                float p = majorStatus.Value.potency <= 0f ? defaultPot : Mathf.Clamp01(majorStatus.Value.potency);
                float mult = Mathf.Clamp(1f - p, floor, ceiling);
                spd = Mathf.Max(1, Mathf.RoundToInt(spd * mult));
            }

            return spd;
        }

        public bool CanActThisTurn(out string msg, out bool selfHit)
        {
            msg = null;
            selfHit = false;

            if (IsDead) { msg = $"{def.displayName} can't move!"; return false; }
            if (!majorStatus.HasValue) return true;

            var s = majorStatus.Value;
            var cfg = BattleConfig.Instance;

            switch (s.type)
            {
                case StatusType.Sleep:
                    msg = $"{def.displayName} is asleep...";
                    return false;

                case StatusType.Dazed:
                    {
                        float defaultPot = cfg != null ? cfg.defaultDazePotency : 0.50f;
                        float p = s.potency <= 0f ? defaultPot : Mathf.Clamp01(s.potency);
                        if (Random.value < p)
                        {
                            msg = $"{def.displayName} is dazed!";
                            return false;
                        }
                        return true;
                    }

                case StatusType.Confused:
                    {
                        float defaultPot = cfg != null ? cfg.defaultConfusePotency : 0.33f;
                        float p = s.potency <= 0f ? defaultPot : Mathf.Clamp01(s.potency);
                        if (Random.value < p)
                        {
                            msg = $"{def.displayName} hurt itself in confusion!";
                            selfHit = true;
                            return true;
                        }
                        return true;
                    }

                default:
                    return true;
            }
        }

        // ── XP & Leveling ────────────────────────────────────────

        public bool TryGainXP(int amount, out int levelsGained)
        {
            levelsGained = 0;
            if (amount <= 0) return false;

            var cfg = BattleConfig.Instance;
            int maxLvl = cfg != null ? cfg.maxLevel : 50;
            if (level >= maxLvl) return false;

            xp += amount;

            int baseThreshold = cfg != null ? cfg.baseXpThreshold : 20;
            int perLevel = cfg != null ? cfg.xpPerLevel : 8;

            while (level < maxLvl)
            {
                int needed = baseThreshold + perLevel * level;
                if (xp < needed) break;
                xp -= needed;
                level++;
                levelsGained++;
                hp = EffectiveMaxHP();
            }

            if (level >= maxLvl) xp = 0;

            return levelsGained > 0;
        }

        // ── Evolution ────────────────────────────────────────────

        public void Evolve(MonsterDefinition newDef)
        {
            if (newDef == null) return;

            int oldMaxHp = EffectiveMaxHP();
            float hpRatio = oldMaxHp > 0 ? (float)hp / oldMaxHp : 1f;

            def = newDef;

            int newMaxHp = EffectiveMaxHP();
            hp = Mathf.Max(1, Mathf.RoundToInt(newMaxHp * hpRatio));

            // Merge new moves from evolved form
            if (newDef.moves != null)
            {
                foreach (var move in newDef.moves)
                {
                    if (move != null && !knownMoves.Contains(move))
                        knownMoves.Add(move);
                }
            }
        }
    }
}
