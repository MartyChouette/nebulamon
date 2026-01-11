using UnityEngine;

namespace Nebula
{
    [RequireComponent(typeof(Collider2D))]
    public class AsteroidHazard2D : MonoBehaviour
    {
        [Header("Damage (Collision)")]
        public float minDamageSpeed = 3.5f;   // below this, no damage
        public float maxDamageSpeed = 16f;    // at/above, max damage
        public float maxDamage = 22f;

        [Header("Damage (Trigger)")]
        public float triggerDamage = 10f;

        private void OnCollisionEnter2D(Collision2D col)
        {
            // Works when asteroid collider is NOT a trigger
            if (!col.collider.CompareTag("Player")) return;

            var health = col.collider.GetComponentInParent<ShipHealth2D>();
            if (health == null) return;

            float speed = col.relativeVelocity.magnitude;
            if (speed < minDamageSpeed) return;

            float t = Mathf.InverseLerp(minDamageSpeed, maxDamageSpeed, speed);
            float dmg = Mathf.Lerp(0f, maxDamage, t);

            health.TakeDamage(dmg);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Works when asteroid collider IS a trigger
            if (!other.CompareTag("Player")) return;

            var health = other.GetComponentInParent<ShipHealth2D>();
            if (health == null) return;

            health.TakeDamage(triggerDamage);
        }
    }
}
