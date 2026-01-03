using System;
using UnityEngine;

namespace SpaceGame
{
    [CreateAssetMenu(menuName = "SpaceGame/Encounter Table", fileName = "EncounterTable")]
    public class EncounterTable : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public EnemyDefinition enemy;
            [Min(0f)] public float weight;
        }

        public Entry[] entries;

        public EnemyDefinition PickRandom()
        {
            if (entries == null || entries.Length == 0)
                return null;

            float total = 0f;
            for (int i = 0; i < entries.Length; i++)
                total += Mathf.Max(0f, entries[i].weight);

            if (total <= 0f)
                return entries[0].enemy;

            float r = UnityEngine.Random.value * total;
            float acc = 0f;

            for (int i = 0; i < entries.Length; i++)
            {
                acc += Mathf.Max(0f, entries[i].weight);
                if (r <= acc)
                    return entries[i].enemy;
            }

            return entries[entries.Length - 1].enemy;
        }
    }
}
