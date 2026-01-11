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
        public readonly MonsterDefinition def;
        public int hp;

        // CTB meter
        public float initiative = 0f;

        // Element quantity pool
        public ElementPool pool;

        // MVP: one major status at a time
        public StatusInstance? majorStatus;

        public bool IsDead => hp <= 0;
        public bool IsBerserk => HasStatus(StatusType.Berserk);

        public MonsterInstance(MonsterDefinition def)
        {
            this.def = def;
            hp = def != null ? def.maxHP : 1;

            // Starting stock (tune later)
            pool = new ElementPool
            {
                solar = 2,
                voids = 2,
                bio = 2,
                time = 2
            };

            if (def != null)
                pool.Add(def.element, 2);
        }

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
            int spd = Mathf.Max(1, def.speed);

            if (HasStatus(StatusType.Slow))
            {
                float p = majorStatus.Value.potency <= 0f ? 0.30f : Mathf.Clamp01(majorStatus.Value.potency);
                float mult = Mathf.Clamp(1f - p, 0.30f, 0.90f);
                spd = Mathf.Max(1, Mathf.RoundToInt(spd * mult));
            }

            return spd;
        }

        public bool CanActThisTurn(out string msg, out bool selfHit)
        {
            msg = null;
            selfHit = false;

            if (IsDead) { msg = $"{def.displayName} can’t move!"; return false; }
            if (!majorStatus.HasValue) return true;

            var s = majorStatus.Value;

            switch (s.type)
            {
                case StatusType.Sleep:
                    msg = $"{def.displayName} is asleep...";
                    return false;

                case StatusType.Dazed:
                    {
                        float p = s.potency <= 0f ? 0.50f : Mathf.Clamp01(s.potency);
                        if (Random.value < p)
                        {
                            msg = $"{def.displayName} is dazed!";
                            return false;
                        }
                        return true;
                    }

                case StatusType.Confused:
                    {
                        float p = s.potency <= 0f ? 0.33f : Mathf.Clamp01(s.potency);
                        if (Random.value < p)
                        {
                            msg = $"{def.displayName} hurt itself in confusion!";
                            selfHit = true;
                            return true; // consumes action
                        }
                        return true;
                    }

                default:
                    return true;
            }
        }
    }
}
