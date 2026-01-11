using UnityEngine;

namespace Nebula
{
    [CreateAssetMenu(menuName = "Nebula/Dialogue/Chirp Profile")]
    public class NPCChirpProfile : ScriptableObject
    {
        public AudioClip[] chirps;
        public float minPitch = 0.95f;
        public float maxPitch = 1.05f;

        public AudioClip Pick()
        {
            if (chirps == null || chirps.Length == 0) return null;
            return chirps[Random.Range(0, chirps.Length)];
        }

        public float PickPitch() => Random.Range(minPitch, maxPitch);
    }
}
