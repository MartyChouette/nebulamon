using UnityEngine;

namespace SpaceGame
{
    [RequireComponent(typeof(Collider2D))]
    public class BattleTrigger2D : MonoBehaviour
    {
        public EnemyDefinition enemy;

        [Tooltip("If true, triggers as soon as the player enters.")]
        public bool instant = true;

        [Tooltip("If false, requires Interact (E) while inside.")]
        public bool requireInteract = false;

        private bool _playerInside;

        private void Reset()
        {
            var c = GetComponent<Collider2D>();
            c.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
                return;

            _playerInside = true;

            if (instant && !requireInteract)
                TryStartBattle(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
                return;

            _playerInside = false;
        }

        private void Update()
        {
            if (!_playerInside || !requireInteract)
                return;

            // Look for ship controller to detect interact
            var ship = FindObjectOfType<PlayerShipController2D>();
            if (ship != null && ship.interactPressedThisFrame)
            {
                var playerCollider = ship.GetComponent<Collider2D>();
                if (playerCollider != null)
                    TryStartBattle(playerCollider);
            }
        }

        private void TryStartBattle(Component playerComponent)
        {
            var flow = GameFlowManager.Instance;
            if (flow == null)
            {
                Debug.LogWarning("No GameFlowManager in scene.");
                return;
            }

            var rb = playerComponent.GetComponent<Rigidbody2D>();
            var tr = playerComponent.transform;

            flow.StartBattle(enemy, rb, tr);
        }
    }
}
