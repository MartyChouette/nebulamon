using UnityEngine;

namespace SpaceGame
{
    public enum RegionType { SafeRoute, AsteroidField, PirateTerritory, Nebula, DerelictGraveyard }

    [RequireComponent(typeof(Collider2D))]
    public class EncounterRegion2D : MonoBehaviour
    {
        public RegionType regionType = RegionType.AsteroidField;

        [Header("Encounters")]
        public EncounterTable encounterTable;

        [Tooltip("Chance per step check. Example: 0.10 = 10% each step.")]
        [Range(0f, 1f)] public float chancePerStep = 0.10f;

        [Tooltip("How many world units count as one 'step'. Smaller = more checks.")]
        [Min(0.1f)] public float stepDistance = 12f;

        [Tooltip("After a battle triggers, block new ones for this many seconds.")]
        [Min(0f)] public float encounterCooldownSeconds = 6f;

        private void Reset()
        {
            var c = GetComponent<Collider2D>();
            c.isTrigger = true;
        }
    }
}
