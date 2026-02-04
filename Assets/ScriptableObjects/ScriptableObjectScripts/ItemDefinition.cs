using System.Collections.Generic;
using UnityEngine;

namespace Nebula
{
    public enum ItemCategory
    {
        Heal,
        CatchDevice,
        Evolution,
        MoveTutor,
        KeyItem,
        Misc
    }

    [CreateAssetMenu(menuName = "Nebula/Items/Item Definition", fileName = "ItemDefinition")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string itemId;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Classification")]
        public ItemCategory category = ItemCategory.Misc;

        [Header("Economy")]
        public int buyPrice;
        public int sellPrice;
        public bool consumable = true;

        [Header("Heal")]
        public int healAmount;
        public bool healStatus;
        public bool healAllParty;

        [Header("Catch")]
        [Range(0f, 2f)] public float catchRateBonus;

        [Header("Evolution")]
        public MonsterDefinition targetMonster;
        public MonsterDefinition evolvedInto;

        [Header("Move Tutor")]
        public MoveDefinition taughtMove;
        public List<MonsterId> compatibleMonsters = new();
    }
}
