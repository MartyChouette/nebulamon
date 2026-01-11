using System.Collections.Generic;
using UnityEngine;

namespace Nebula
{
    [CreateAssetMenu(menuName = "Nebula/Battle/Monster Definition", fileName = "MonsterDefinition")]
    public class MonsterDefinition : ScriptableObject
    {
        [Header("Identity")]
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

        [Header("Moves")]
        public List<MoveDefinition> moves = new();
    }
}
