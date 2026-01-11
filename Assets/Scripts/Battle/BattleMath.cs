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
            if (IsStrongAgainst(attacker, defender)) return 2.0f;
            if (IsStrongAgainst(defender, attacker)) return 0.5f;
            return 1.0f;
        }

        public static float FinalHitChance(MonsterDefinition atk, MonsterDefinition def, float moveAccuracy)
        {
            float aAcc = Mathf.Max(1f, atk.accuracy);
            float dEva = Mathf.Max(1f, def.evasion);

            float luckNudge = Mathf.Clamp(atk.luck - def.luck, -20, 20) * 0.0025f; // -0.05..+0.05

            float ratio = aAcc / dEva;
            float scaled = moveAccuracy * (0.75f + 0.25f * ratio);

            return Mathf.Clamp01(scaled + luckNudge);
        }

        public static float FinalCritChance(MonsterDefinition atk, float moveCritChance)
        {
            float bonus = atk.luck * 0.005f; // +0.5% per luck point
            return Mathf.Clamp01(moveCritChance + bonus);
        }

        public static int CalcDamage(MoveDefinition move, MonsterDefinition atk, MonsterDefinition def, bool isCrit)
        {
            int basePower = Mathf.Max(1, move.power);

            float atkStat = move.category == MoveCategory.Physical ? Mathf.Max(1f, atk.physAttack) : Mathf.Max(1f, atk.elemAttack);
            float defStat = move.category == MoveCategory.Physical ? Mathf.Max(1f, def.physDefense) : Mathf.Max(1f, def.elemDefense);

            float stab = (move.element == atk.element) ? 1.25f : 1f;
            float adv = AdvantageMultiplier(move.element, def.element);
            float crit = isCrit ? 1.5f : 1f;
            float rng = Random.Range(0.9f, 1.1f);

            float raw = basePower * (atkStat / defStat) * stab * adv * crit * rng;
            return Mathf.Max(1, Mathf.RoundToInt(raw));
        }

        public static float StatusApplyChance(MonsterDefinition atk, MonsterDefinition def, float baseChance)
        {
            float aRes = Mathf.Max(1f, atk.resolve);
            float dRes = Mathf.Max(1f, def.resolve);

            float ratio = aRes / dRes;
            float scaled = baseChance * (0.65f + 0.35f * ratio);

            return Mathf.Clamp01(scaled);
        }

        public static int StatusDurationAfterResolve(MonsterDefinition atk, MonsterDefinition def, int baseTurns)
        {
            int turns = Mathf.Max(1, baseTurns);

            float dRes = Mathf.Max(1f, def.resolve);
            float aRes = Mathf.Max(1f, atk.resolve);

            float ratio = aRes / dRes;
            float factor = Mathf.Lerp(0.7f, 1.0f, Mathf.Clamp01((ratio - 0.5f) / 1.5f));

            return Mathf.Max(1, Mathf.RoundToInt(turns * factor));
        }
    }
}
