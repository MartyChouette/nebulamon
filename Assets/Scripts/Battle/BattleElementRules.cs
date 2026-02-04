// Assets/Scripts/Battle/BattleElementRules.cs
using UnityEngine;

namespace Nebula
{
    public static class BattleElementRules
    {
        // "Solar weak to Bio" means Bio > Solar
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

        public static float CalcDamage(
            int basePower,
            int attackerAtk,
            int defenderDef,
            ElementType moveElement,
            ElementType attackerElement,
            ElementType defenderElement,
            bool isCrit,
            float random01)
        {
            var cfg = BattleConfig.Instance;
            float stabVal = cfg != null ? cfg.stabBonus : 1.25f;
            float critVal = cfg != null ? cfg.critMultiplier : 1.5f;
            float rngMin = cfg != null ? cfg.damageRngMin : 0.9f;
            float rngMax = cfg != null ? cfg.damageRngMax : 1.1f;

            float atk = Mathf.Max(1f, attackerAtk);
            float def = Mathf.Max(1f, defenderDef);

            float stab = (moveElement == attackerElement) ? stabVal : 1f;
            float adv = AdvantageMultiplier(moveElement, defenderElement);
            float crit = isCrit ? critVal : 1f;

            float rng = Mathf.Lerp(rngMin, rngMax, Mathf.Clamp01(random01));

            float raw = basePower * (atk / def) * stab * adv * crit * rng;
            return Mathf.Max(1f, raw);
        }
    }
}
