// Assets/Scripts/Battle/EncounterTable.cs
using Nebula;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nebula
{
    [CreateAssetMenu(menuName = "Nebula/Encounter/Encounter Table")]
    public class EncounterTable : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public EnemyDefinition enemy;
            [Min(0f)] public float weight;
        }

        public List<Entry> entries = new();

        public EnemyDefinition PickRandom()
        {
            float total = 0f;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].enemy != null && entries[i].weight > 0f)
                    total += entries[i].weight;

            if (total <= 0f) return null;

            float r = UnityEngine.Random.value * total;
            float acc = 0f;

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e.enemy == null || e.weight <= 0f) continue;

                acc += e.weight;
                if (r <= acc) return e.enemy;
            }

            return null;
        }
    }
}
