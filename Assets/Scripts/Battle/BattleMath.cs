using UnityEngine;

namespace Nebula
{
    public static class BattleMath
    {
        // Weakness ring:
        // Solar weak to Bio => Bio strong vs Solar
        // Bio weak to Time  => Time strong vs Bio
        // Time weak to Void => Void strong vs Time
        // Void weak to Solar=> Solar strong vs Void
        public static bool IsStrongAgainst(ElementType attacker, ElementType defender)
        {
            return (attacker == ElementType.Bio && defender == ElementType.Solar) ||
                   (attacker == ElementType.Time && defender == ElementType.Bio) ||
                   (attacker == ElementType.Void && defender == ElementType.Time) ||
                   (attacker == ElementType.Solar && defender == ElementType.Void);
        }

        public static float AdvantageMultiplier(ElementType attacker, ElementType defender)
        {
            var cfg = BattleConfig.Instance;
            float strong = cfg != null ? cfg.strongMultiplier : 2.0f;
            float weak = cfg != null ? cfg.weakMultiplier : 0.5f;

            if (IsStrongAgainst(attacker, defender)) return strong;
            if (IsStrongAgainst(defender, attacker)) return weak;
            return 1.0f;
        }

        public static float FinalHitChance(MonsterInstance atk, MonsterInstance def, float moveAccuracy)
        {
            var cfg = BattleConfig.Instance;
            float accBase = cfg != null ? cfg.accBase : 0.75f;
            float accScaling = cfg != null ? cfg.accScaling : 0.25f;
            float nudgePerPt = cfg != null ? cfg.luckNudgePerPoint : 0.0025f;
            int clampRange = cfg != null ? cfg.luckClampRange : 20;

            float aAcc = Mathf.Max(1f, atk.EffectiveAccuracy());
            float dEva = Mathf.Max(1f, def.EffectiveEvasion());

            float luckNudge = Mathf.Clamp(atk.EffectiveLuck() - def.EffectiveLuck(), -clampRange, clampRange) * nudgePerPt;

            float ratio = aAcc / dEva;
            float scaled = moveAccuracy * (accBase + accScaling * ratio);

            return Mathf.Clamp01(scaled + luckNudge);
        }

        public static float FinalCritChance(MonsterInstance atk, float moveCritChance)
        {
            var cfg = BattleConfig.Instance;
            float luckRate = cfg != null ? cfg.luckToCritRate : 0.005f;

            float bonus = atk.EffectiveLuck() * luckRate;
            return Mathf.Clamp01(moveCritChance + bonus);
        }

        public static int CalcDamage(MoveDefinition move, MonsterInstance atk, MonsterInstance def, bool isCrit)
        {
            var cfg = BattleConfig.Instance;
            float stabVal = cfg != null ? cfg.stabBonus : 1.25f;
            float critVal = cfg != null ? cfg.critMultiplier : 1.5f;
            float rngMin = cfg != null ? cfg.damageRngMin : 0.9f;
            float rngMax = cfg != null ? cfg.damageRngMax : 1.1f;
            int minDmg = cfg != null ? cfg.minimumDamage : 1;

            int basePower = Mathf.Max(1, move.power);

            float atkStat = move.category == MoveCategory.Physical
                ? Mathf.Max(1f, atk.EffectivePhysAttack())
                : Mathf.Max(1f, atk.EffectiveElemAttack());

            float defStat = move.category == MoveCategory.Physical
                ? Mathf.Max(1f, def.EffectivePhysDefense())
                : Mathf.Max(1f, def.EffectiveElemDefense());

            float stab = (move.element == atk.def.element) ? stabVal : 1f;
            float adv = AdvantageMultiplier(move.element, def.def.element);
            float crit = isCrit ? critVal : 1f;
            float rng = Random.Range(rngMin, rngMax);

            float raw = basePower * (atkStat / defStat) * stab * adv * crit * rng;
            return Mathf.Max(minDmg, Mathf.RoundToInt(raw));
        }

        public static float StatusApplyChance(MonsterInstance atk, MonsterInstance def, float baseChance)
        {
            var cfg = BattleConfig.Instance;
            float baseW = cfg != null ? cfg.statusBaseWeight : 0.65f;
            float scaleW = cfg != null ? cfg.statusScaling : 0.35f;

            float aRes = Mathf.Max(1f, atk.EffectiveResolve());
            float dRes = Mathf.Max(1f, def.EffectiveResolve());

            float ratio = aRes / dRes;
            float scaled = baseChance * (baseW + scaleW * ratio);

            return Mathf.Clamp01(scaled);
        }

        public static int StatusDurationAfterResolve(MonsterInstance atk, MonsterInstance def, int baseTurns)
        {
            var cfg = BattleConfig.Instance;
            float floor = cfg != null ? cfg.durationFloor : 0.7f;
            float ceil = cfg != null ? cfg.durationCeil : 1.0f;
            float offset = cfg != null ? cfg.durationOffset : 0.5f;
            float scale = cfg != null ? cfg.durationScale : 1.5f;

            int turns = Mathf.Max(1, baseTurns);

            float dRes = Mathf.Max(1f, def.EffectiveResolve());
            float aRes = Mathf.Max(1f, atk.EffectiveResolve());

            float ratio = aRes / dRes;
            float factor = Mathf.Lerp(floor, ceil, Mathf.Clamp01((ratio - offset) / scale));

            return Mathf.Max(1, Mathf.RoundToInt(turns * factor));
        }

        public static int CalcXpGain(MonsterInstance defeated, int victorLevel)
        {
            if (defeated?.def == null) return 0;
            int baseYield = Mathf.Max(1, defeated.def.baseXpYield);
            float levelFactor = defeated.level / (float)Mathf.Max(1, victorLevel);
            return Mathf.Max(1, Mathf.RoundToInt(baseYield * levelFactor));
        }

        public static float CalcCatchChance(MonsterInstance target, float deviceBonus)
        {
            if (target == null || target.def == null) return 0f;

            var cfg = BattleConfig.Instance;
            float baseRate = cfg != null ? cfg.baseCatchRate : 0.3f;
            float hpFactor = cfg != null ? cfg.hpCatchFactor : 0.5f;
            float statusBonus = cfg != null ? cfg.statusCatchBonus : 0.15f;

            float hpPercent = (float)target.hp / Mathf.Max(1, target.EffectiveMaxHP());
            float hpBonus = (1f - hpPercent) * hpFactor;
            float status = target.majorStatus.HasValue ? statusBonus : 0f;

            return Mathf.Clamp01(baseRate + deviceBonus + hpBonus + status);
        }
    }
}
