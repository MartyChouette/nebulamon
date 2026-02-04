using System.Collections.Generic;
using UnityEngine;

namespace Nebula
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EncounterDirector2D : MonoBehaviour
    {
        private Rigidbody2D _rb;

        private readonly List<EncounterRegion2D> _regionsInside = new();
        private Vector2 _lastPos;
        private float _distanceAccumulator;
        private float _cooldownTimer;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _lastPos = _rb.position;
        }

        private void Update()
        {
            if (_cooldownTimer > 0f)
                _cooldownTimer -= Time.deltaTime;
        }

        private void FixedUpdate()
        {
            if (_regionsInside.Count == 0)
            {
                _lastPos = _rb.position;
                _distanceAccumulator = 0f;
                return;
            }

            // Pick "highest priority" region if overlapping (last entered wins)
            EncounterRegion2D active = _regionsInside[_regionsInside.Count - 1];
            if (active == null || active.encounterTable == null)
            {
                _lastPos = _rb.position;
                return;
            }

            // Don�t roll during cooldown or if we are transitioning scenes
            if (_cooldownTimer > 0f || GameFlowManager.Instance == null)
            {
                _lastPos = _rb.position;
                return;
            }

            // Accumulate distance traveled inside region
            Vector2 p = _rb.position;
            float d = Vector2.Distance(p, _lastPos);
            _lastPos = p;

            // (Optional) ignore tiny micro-jitter
            if (d < 0.01f) return;

            _distanceAccumulator += d;

            // Roll once per stepDistance
            while (_distanceAccumulator >= active.stepDistance)
            {
                _distanceAccumulator -= active.stepDistance;

                if (UnityEngine.Random.value <= active.chancePerStep)
                {
                    TriggerBattle(active);
                    break;
                }
            }
        }

        private void TriggerBattle(EncounterRegion2D region)
        {
            var enemy = region.encounterTable.PickRandom();
            if (enemy == null)
                return;

            // Skip defeated trainers
            if (enemy.isTrainer && !string.IsNullOrEmpty(enemy.trainerId)
                && Progression.IsTrainerDefeated(enemy.trainerId))
                return;

            _cooldownTimer = region.encounterCooldownSeconds;

            // Start battle through your persistent flow manager
            GameFlowManager.Instance.StartBattle(enemy, _rb, transform);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var region = other.GetComponent<EncounterRegion2D>();
            if (region != null && !_regionsInside.Contains(region))
                _regionsInside.Add(region);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var region = other.GetComponent<EncounterRegion2D>();
            if (region != null)
                _regionsInside.Remove(region);
        }
    }
}
