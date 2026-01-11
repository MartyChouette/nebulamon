// Assets/Scripts/Battle/BattleElementRules.cs
using Nebula;
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
            if (IsStrongAgainst(attacker, defender)) return 2.0f;
            if (IsStrongAgainst(defender, attacker)) return 0.5f;
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
            float atk = Mathf.Max(1f, attackerAtk);
            float def = Mathf.Max(1f, defenderDef);

            float stab = (moveElement == attackerElement) ? 1.25f : 1f;
            float adv = AdvantageMultiplier(moveElement, defenderElement);
            float crit = isCrit ? 1.5f : 1f;

            // mild randomness 0.9..1.1
            float rng = Mathf.Lerp(0.9f, 1.1f, Mathf.Clamp01(random01));

            float raw = basePower * (atk / def) * stab * adv * crit * rng;
            return Mathf.Max(1f, raw);
        }
    }
}
