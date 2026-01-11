using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nebula
{
    public enum MoveCategory { Physical, Elemental, Support }
    public enum MoveKind { Damage, Heal, Status }

    [CreateAssetMenu(menuName = "Nebula/Battle/Move Definition", fileName = "MoveDefinition")]
    public class MoveDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string moveName = "Solar Beam";

        [Header("Classification")]
        public MoveCategory category = MoveCategory.Elemental;
        public MoveKind kind = MoveKind.Damage;

        [Header("Element")]
        public ElementType element = ElementType.Solar;

        [Header("Element Costs (quantity-based)")]
        public List<ElementCost> costs = new();

        [Serializable]
        public struct ElementCost
        {
            public ElementType element;
            [Min(0)] public int amount;
        }

        [Header("Reliability")]
        [Range(0f, 1f)] public float accuracy = 1f;
        [Range(0f, 1f)] public float critChance = 0.1f;

        [Header("Damage")]
        [Min(1)] public int power = 10;

        [Header("Heal")]
        [Min(0)] public int healAmount = 10;

        [Header("Status Payload (optional)")]
        public StatusPayload status;

        [Serializable]
        public struct StatusPayload
        {
            public bool enabled;
            public StatusType type;

            [Tooltip("Chance to apply on hit (before Resolve mitigation).")]
            [Range(0f, 1f)] public float applyChance;

            [Tooltip("Duration in turns (mitigated by Resolve).")]
            [Min(1)] public int durationTurns;

            [Tooltip("If true, apply to the user instead of target (e.g., Berserk self-buff).")]
            public bool applyToSelf;

            [Tooltip("Optional strength knob used by some statuses (Slow%, Dazed skip%, Confused self-hit%).")]
            [Range(0f, 1f)] public float potency;
        }
    }
}
