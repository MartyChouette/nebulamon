using UnityEngine;

namespace Nebula
{
    public static class EvolutionManager
    {
        /// <summary>
        /// Attempt to evolve a roster monster using an evolution item.
        /// Returns true on success. On failure, msg explains why.
        /// </summary>
        public static bool TryEvolve(int rosterIndex, ItemDefinition item, out string msg)
        {
            msg = null;

            if (item == null || item.category != ItemCategory.Evolution)
            {
                msg = "That item can't trigger evolution.";
                return false;
            }

            if (item.evolvedInto == null)
            {
                msg = "This evolution item has no target form configured.";
                return false;
            }

            var data = Progression.Data;
            if (data == null || rosterIndex < 0 || rosterIndex >= data.roster.Count)
            {
                msg = "Invalid roster entry.";
                return false;
            }

            var owned = data.roster[rosterIndex];

            // Check target monster compatibility
            if (item.targetMonster != null && owned.monsterId != item.targetMonster.monsterId)
            {
                msg = "This item isn't compatible with that monster.";
                return false;
            }

            // Consume item
            if (item.consumable && !Progression.RemoveItem(item.itemId))
            {
                msg = "You don't have that item!";
                return false;
            }

            // Get old definition for display
            var catalog = MonsterCatalog.Instance;
            var oldDef = catalog != null ? catalog.GetByMonsterId(owned.monsterId) : null;
            string oldName = oldDef != null ? oldDef.displayName : owned.monsterId.ToString();

            // Build a temporary MonsterInstance to perform the evolution logic
            var tempInstance = new MonsterInstance(oldDef, owned.level);
            tempInstance.hp = owned.currentHp;
            tempInstance.xp = owned.xp;

            // Restore known moves
            tempInstance.knownMoves.Clear();
            if (catalog != null)
            {
                // Rebuild known moves from names (MoveDefs aren't saved, only names)
                foreach (var moveName in owned.knownMoveNames)
                {
                    // We don't have a move catalog lookup by name, so keep names as-is
                }
            }

            // Perform evolution
            var newDef = item.evolvedInto;
            tempInstance.Evolve(newDef);

            // Write back to roster
            owned.monsterId = newDef.monsterId;
            owned.currentHp = tempInstance.hp;

            // Merge new moves from evolved form into known move names
            if (newDef.moves != null)
            {
                foreach (var move in newDef.moves)
                {
                    if (move != null && !owned.knownMoveNames.Contains(move.moveName))
                        owned.knownMoveNames.Add(move.moveName);
                }
            }

            // Mark caught
            Progression.MarkCaught(newDef.monsterId);
            Progression.Save();

            msg = $"{oldName} evolved into {newDef.displayName}!";
            return true;
        }
    }
}
