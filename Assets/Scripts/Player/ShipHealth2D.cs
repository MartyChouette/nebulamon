using UnityEngine;

namespace Nebula
{
    public class ShipHealth2D : MonoBehaviour
    {
        [Header("Health")]
        public float maxHP = 100f;
        public float currentHP = 100f;

        [Header("Invincibility")]
        public float invulnSecondsAfterHit = 0.25f;

        private float _invulnTimer;

        public bool IsDead => currentHP <= 0f;

        private void Awake()
        {
            currentHP = Mathf.Clamp(currentHP, 0f, maxHP);
            if (currentHP <= 0f) currentHP = maxHP;
        }

        private void Update()
        {
            if (_invulnTimer > 0f) _invulnTimer -= Time.deltaTime;
        }

        public bool CanTakeDamage() => _invulnTimer <= 0f && !IsDead;

        public void TakeDamage(float amount)
        {
            if (!CanTakeDamage()) return;

            currentHP = Mathf.Max(0f, currentHP - Mathf.Max(0f, amount));
            _invulnTimer = invulnSecondsAfterHit;

            if (IsDead)
                Debug.Log("Ship destroyed!");
        }

        public void HealToFull()
        {
            currentHP = maxHP;
        }
    }
}
