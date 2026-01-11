using UnityEngine;

namespace Nebula
{
    [System.Serializable]
    public struct ElementPool
    {
        public int solar;
        public int voids;
        public int bio;
        public int time;

        public int Get(ElementType t) => t switch
        {
            ElementType.Solar => solar,
            ElementType.Void => voids,
            ElementType.Bio => bio,
            ElementType.Time => time,
            _ => 0
        };

        public void Add(ElementType t, int amount)
        {
            amount = Mathf.Max(0, amount);
            switch (t)
            {
                case ElementType.Solar: solar += amount; break;
                case ElementType.Void: voids += amount; break;
                case ElementType.Bio: bio += amount; break;
                case ElementType.Time: time += amount; break;
            }
        }

        public bool CanAfford(MoveDefinition move)
        {
            if (move == null) return false;
            if (move.costs == null || move.costs.Count == 0) return true;

            for (int i = 0; i < move.costs.Count; i++)
            {
                var c = move.costs[i];
                if (Get(c.element) < c.amount) return false;
            }
            return true;
        }

        public void Spend(MoveDefinition move)
        {
            if (move == null || move.costs == null) return;

            for (int i = 0; i < move.costs.Count; i++)
            {
                var c = move.costs[i];
                int amt = Mathf.Max(0, c.amount);

                switch (c.element)
                {
                    case ElementType.Solar: solar -= amt; break;
                    case ElementType.Void: voids -= amt; break;
                    case ElementType.Bio: bio -= amt; break;
                    case ElementType.Time: time -= amt; break;
                }
            }

            solar = Mathf.Max(0, solar);
            voids = Mathf.Max(0, voids);
            bio = Mathf.Max(0, bio);
            time = Mathf.Max(0, time);
        }
    }
}
