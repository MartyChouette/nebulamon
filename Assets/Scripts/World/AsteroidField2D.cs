using System.Collections.Generic;
using UnityEngine;

namespace Nebula
{
    public class AsteroidField2D : MonoBehaviour
    {
        [Header("Bounds")]
        [Tooltip("Use a BoxCollider2D (isTrigger ok) as the spawn area.")]
        public CircleCollider2D bounds;

        [Header("Spawn")]
        public GameObject[] asteroidPrefabs;
        public int count = 180;

        [Tooltip("Minimum distance from player spawn to avoid immediate collisions.")]
        public float keepAwayRadius = 4f;

        [Header("Placement")]
        public float minAsteroidRadius = 0.35f; // for overlap testing
        public int maxPlacementTriesPerAsteroid = 18;
        public LayerMask obstacleLayer; // if you want to avoid stations/walls etc.

        [Header("Optional drift")]
        public bool giveRandomDrift = true;
        public float maxDriftSpeed = 0.8f;
        public float maxSpinDegPerSec = 50f;

        private readonly List<GameObject> _spawned = new List<GameObject>();

        private void Reset()
        {
            bounds = GetComponent<CircleCollider2D>();
        }

        private void Start()
        {
            Spawn();
        }

        [ContextMenu("Spawn Asteroid Field")]
        public void Spawn()
        {
            if (bounds == null)
            {
                Debug.LogError("AsteroidField2D: bounds is null. Add/assign a BoxCollider2D.");
                return;
            }
            if (asteroidPrefabs == null || asteroidPrefabs.Length == 0)
            {
                Debug.LogError("AsteroidField2D: no asteroidPrefabs assigned.");
                return;
            }

            Clear();

            Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
            Vector2 playerPos = player != null ? (Vector2)player.position : Vector2.positiveInfinity;

            var b = bounds.bounds;
            Vector2 min = b.min;
            Vector2 max = b.max;

            int spawned = 0;

            for (int i = 0; i < count; i++)
            {
                bool placed = false;

                for (int t = 0; t < maxPlacementTriesPerAsteroid; t++)
                {
                    Vector2 p = new Vector2(
                        Random.Range(min.x, max.x),
                        Random.Range(min.y, max.y)
                    );

                    if (player != null && Vector2.Distance(p, playerPos) < keepAwayRadius)
                        continue;

                    // overlap test so we don't clump too hard
                    if (Physics2D.OverlapCircle(p, minAsteroidRadius, obstacleLayer) != null)
                        continue;

                    GameObject prefab = asteroidPrefabs[Random.Range(0, asteroidPrefabs.Length)];
                    if (prefab == null) continue;
                    GameObject a = Instantiate(prefab, p, Quaternion.Euler(0, 0, Random.Range(0f, 360f)), transform);

                    // Optional drift/spin
                    var rb = a.GetComponent<Rigidbody2D>();
                    if (giveRandomDrift)
                    {
                        if (rb != null)
                        {
                            rb.bodyType = RigidbodyType2D.Kinematic; // stable obstacles with drift
                            rb.linearVelocity = Random.insideUnitCircle * maxDriftSpeed;
                            rb.angularVelocity = Random.Range(-maxSpinDegPerSec, maxSpinDegPerSec);
                        }
                        else
                        {
                            // if no rigidbody, you can add a simple drift script later
                        }
                    }

                    _spawned.Add(a);
                    spawned++;
                    placed = true;
                    break;
                }

                if (!placed)
                {
                    // skip; prevents infinite loops
                }
            }

            Debug.Log($"AsteroidField2D: spawned {spawned}/{count} asteroids.");
        }

        [ContextMenu("Clear Asteroid Field")]
        public void Clear()
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                if (_spawned[i] != null) Destroy(_spawned[i]);
            }
            _spawned.Clear();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (bounds == null) return;
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.25f);
            Gizmos.DrawCube(bounds.bounds.center, bounds.bounds.size);
        }
#endif
    }
}
