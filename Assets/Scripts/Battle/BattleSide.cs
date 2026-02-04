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
                    if (party[i] != null && !party[i].IsDead) return false;
                return true;
            }
        }

        public int AliveCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < party.Count; i++)
                    if (party[i] != null && !party[i].IsDead) count++;
                return count;
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

        public bool SwitchTo(int index)
        {
            if (index < 0 || index >= party.Count) return false;
            if (party[index].IsDead) return false;
            activeIndex = index;
            return true;
        }

        public List<int> GetAliveIndicesExcept(int exclude)
        {
            var list = new List<int>();
            for (int i = 0; i < party.Count; i++)
            {
                if (i != exclude && !party[i].IsDead)
                    list.Add(i);
            }
            return list;
        }
    }
}
