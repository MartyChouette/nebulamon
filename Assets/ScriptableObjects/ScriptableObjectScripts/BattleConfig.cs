using UnityEngine;

namespace Nebula
{
    [CreateAssetMenu(menuName = "Nebula/Battle/Battle Config", fileName = "BattleConfig")]
    public class BattleConfig : ScriptableObject
    {
        public static BattleConfig Instance { get; set; }

        // ── Type Advantage ──────────────────────────────────────────
        [Header("Type Advantage")]
        [Tooltip("Damage multiplier when attacker has type advantage.")]
        public float strongMultiplier = 2.0f;

        [Tooltip("Damage multiplier when attacker has type disadvantage.")]
        public float weakMultiplier = 0.5f;

        // ── STAB & Crits ───────────────────────────────────────────
        [Header("STAB & Crits")]
        [Tooltip("Same-Type Attack Bonus multiplier.")]
        public float stabBonus = 1.25f;

        [Tooltip("Damage multiplier on critical hit.")]
        public float critMultiplier = 1.5f;

        // ── Damage ─────────────────────────────────────────────────
        [Header("Damage")]
        [Tooltip("Minimum random damage variance.")]
        public float damageRngMin = 0.9f;

        [Tooltip("Maximum random damage variance.")]
        public float damageRngMax = 1.1f;

        [Tooltip("Minimum damage any hit can deal.")]
        public int minimumDamage = 1;

        // ── Accuracy ───────────────────────────────────────────────
        [Header("Accuracy")]
        [Tooltip("Base accuracy weight (before stat ratio scaling).")]
        public float accBase = 0.75f;

        [Tooltip("Stat ratio scaling weight for accuracy.")]
        public float accScaling = 0.25f;

        [Tooltip("Hit chance nudge per point of luck difference.")]
        public float luckNudgePerPoint = 0.0025f;

        [Tooltip("Max luck difference clamped for hit nudge.")]
        public int luckClampRange = 20;

        // ── Crit from Luck ─────────────────────────────────────────
        [Header("Crit from Luck")]
        [Tooltip("Crit chance bonus per point of luck.")]
        public float luckToCritRate = 0.005f;

        // ── Status Apply ───────────────────────────────────────────
        [Header("Status Apply")]
        [Tooltip("Base weight for status apply chance formula.")]
        public float statusBaseWeight = 0.65f;

        [Tooltip("Stat ratio scaling weight for status apply chance.")]
        public float statusScaling = 0.35f;

        // ── Status Duration ────────────────────────────────────────
        [Header("Status Duration")]
        [Tooltip("Lerp floor for duration factor.")]
        public float durationFloor = 0.7f;

        [Tooltip("Lerp ceiling for duration factor.")]
        public float durationCeil = 1.0f;

        [Tooltip("Ratio offset subtracted before scaling.")]
        public float durationOffset = 0.5f;

        [Tooltip("Ratio divisor for Clamp01 in duration lerp.")]
        public float durationScale = 1.5f;

        // ── Element Pool ───────────────────────────────────────────
        [Header("Element Pool")]
        [Tooltip("Starting stock for each element type.")]
        public int baseElementStock = 2;

        [Tooltip("Extra stock added for the monster's native element.")]
        public int nativeElementBonus = 2;

        // ── Status Defaults ────────────────────────────────────────
        [Header("Status Defaults")]
        [Tooltip("Default Slow potency when none specified.")]
        [Range(0f, 1f)] public float defaultSlowPotency = 0.30f;

        [Tooltip("Minimum speed multiplier under Slow.")]
        [Range(0f, 1f)] public float slowFloor = 0.30f;

        [Tooltip("Maximum speed multiplier under Slow.")]
        [Range(0f, 1f)] public float slowCeiling = 0.90f;

        [Tooltip("Default Daze skip chance when no potency specified.")]
        [Range(0f, 1f)] public float defaultDazePotency = 0.50f;

        [Tooltip("Default Confuse self-hit chance when no potency specified.")]
        [Range(0f, 1f)] public float defaultConfusePotency = 0.33f;

        // ── Self-Hit ───────────────────────────────────────────────
        [Header("Self-Hit")]
        [Tooltip("Fraction of max HP dealt as self-hit damage from Confusion.")]
        [Range(0f, 1f)] public float confuseSelfHitPercent = 0.12f;

        // ── Battle Timing ──────────────────────────────────────────
        [Header("Battle Timing")]
        [Tooltip("Delay after drawing energy.")]
        public float drawDelay = 0.25f;

        [Tooltip("Delay showing draw result.")]
        public float drawResultDelay = 0.45f;

        [Tooltip("Delay when action is blocked by status.")]
        public float actionBlockedDelay = 0.45f;

        [Tooltip("Delay after announcing a move.")]
        public float moveAnnounceDelay = 0.35f;

        [Tooltip("Delay for 'not enough energy' message.")]
        public float notEnoughEnergyDelay = 0.45f;

        [Tooltip("Delay after a miss.")]
        public float missDelay = 0.45f;

        [Tooltip("Delay after crit message.")]
        public float critDelay = 0.35f;

        [Tooltip("Delay after 'super effective' message.")]
        public float superEffectiveDelay = 0.35f;

        [Tooltip("Delay after 'not very effective' message.")]
        public float notEffectiveDelay = 0.35f;

        [Tooltip("Delay after status apply message.")]
        public float statusApplyDelay = 0.45f;

        [Tooltip("Delay after status tick message.")]
        public float statusTickDelay = 0.35f;

        [Tooltip("Delay at battle end.")]
        public float battleEndDelay = 0.7f;

        [Tooltip("Delay after running away.")]
        public float runAwayDelay = 0.5f;

        [Tooltip("Delay after heal.")]
        public float healDelay = 0.25f;

        // ── AI ─────────────────────────────────────────────────────
        [Header("AI")]
        [Tooltip("Max random attempts for enemy move selection.")]
        [Min(1)] public int enemyAiRetries = 16;

        [Tooltip("Safety guard for initiative advance loop.")]
        [Min(100)] public int initiativeLoopGuard = 10000;

        // ── XP & Leveling ────────────────────────────────────────
        [Header("XP & Leveling")]
        [Tooltip("Base XP needed for level 2.")]
        public int baseXpThreshold = 20;

        [Tooltip("Additional XP needed per level.")]
        public int xpPerLevel = 8;

        [Tooltip("Maximum monster level.")]
        [Min(2)] public int maxLevel = 50;

        // ── Catching ─────────────────────────────────────────────
        [Header("Catching")]
        [Tooltip("Base catch rate before modifiers.")]
        [Range(0f, 1f)] public float baseCatchRate = 0.3f;

        [Tooltip("How much low HP increases catch chance.")]
        public float hpCatchFactor = 0.5f;

        [Tooltip("Bonus catch chance when target has a status.")]
        [Range(0f, 1f)] public float statusCatchBonus = 0.15f;

        [Tooltip("Maximum monsters in the roster.")]
        [Min(3)] public int maxRosterSize = 30;
    }
}
