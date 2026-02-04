using System.Collections.Generic;
using UnityEngine;

namespace Nebula
{
    [CreateAssetMenu(menuName = "Nebula/Battle/Monster Definition", fileName = "MonsterDefinition")]
    public class MonsterDefinition : ScriptableObject, ICardData
    {
        // ICardData implementation
        public string DisplayName => displayName;
        public Sprite CardSprite => battleSprite;
        public Sprite BackgroundSprite => null; // Monsters use default battle background

        [Header("Identity")]
        public MonsterId monsterId;
        public string displayName = "Nebula Beast";
        public ElementType element = ElementType.Solar;

        [Header("Sprites")]
        public Sprite battleSprite;
        public List<Sprite> animFrames;
        [Min(1)] public int animFps = 6;

        [Header("Core Stats")]
        [Min(1)] public int maxHP = 40;
        [Min(1)] public int speed = 10;

        [Header("Physical")]
        [Min(1)] public int physAttack = 10;
        [Min(1)] public int physDefense = 10;

        [Header("Elemental")]
        [Min(1)] public int elemAttack = 10;
        [Min(1)] public int elemDefense = 10;

        [Header("Reliability")]
        [Min(1)] public int accuracy = 10;
        [Min(1)] public int evasion = 10;

        [Header("Mind / Fortune")]
        [Min(1)] public int resolve = 10;
        [Min(0)] public int luck = 0;

        [Header("Growth Rates (per level)")]
        public float hpGrowth = 2f;
        public float speedGrowth = 1f;
        public float physAttackGrowth = 1f;
        public float physDefenseGrowth = 1f;
        public float elemAttackGrowth = 1f;
        public float elemDefenseGrowth = 1f;
        public float accuracyGrowth = 1f;
        public float evasionGrowth = 1f;
        public float resolveGrowth = 1f;
        public float luckGrowth = 0.5f;

        [Header("Moves")]
        public List<MoveDefinition> moves = new();

        [Header("Evolution")]
        [Tooltip("The monster this evolves into. Null if no evolution.")]
        public MonsterDefinition evolvedForm;

        [Header("XP & Bestiary")]
        [Min(1)] public int baseXpYield = 30;
        [TextArea] public string bestiaryDescription;
    }
}
