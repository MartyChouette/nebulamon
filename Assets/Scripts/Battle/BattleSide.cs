using System.Collections.Generic;

namespace Nebula
{
    public sealed class BattleSide
    {
        public readonly List<MonsterInstance> party = new();
        public int activeIndex = 0;

        public MonsterInstance Active =>
            (party.Count > 0 && activeIndex >= 0 && activeIndex < party.Count) ? party[activeIndex] : null;

        public bool AllDead
        {
            get
            {
                for (int i = 0; i < party.Count; i++)
                    if (!party[i].IsDead) return false;
                return true;
            }
        }

        public bool TryAdvanceToNextAlive()
        {
            for (int i = 0; i < party.Count; i++)
            {
                if (!party[i].IsDead)
                {
                    activeIndex = i;
                    return true;
                }
            }
            return false;
        }
    }
}
