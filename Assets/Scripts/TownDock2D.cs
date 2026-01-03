using UnityEngine;

namespace SpaceGame
{
    [RequireComponent(typeof(Collider2D))]
    public class TownDock2D : MonoBehaviour
    {
        [Tooltip("Scene name of the town/station interior.")]
        public string townSceneName = "Town";

        [Tooltip("Optional: show prompt UI when in range (hook this up later).")]
        public bool showDebugPrompt = true;

        private PlayerShipController2D _ship;
        private Rigidbody2D _shipBody;

        private void Reset()
        {
            var c = GetComponent<Collider2D>();
            c.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
                return;

            _ship = other.GetComponent<PlayerShipController2D>();
            _shipBody = other.GetComponent<Rigidbody2D>();

            if (showDebugPrompt)
                Debug.Log($"In docking range. Press Interact (E) to enter '{townSceneName}'.");
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
                return;

            _ship = null;
            _shipBody = null;
        }

        private void Update()
        {
            if (_ship == null)
                return;

            if (_ship.interactPressedThisFrame)
            {
                var flow = GameFlowManager.Instance;
                if (flow == null)
                {
                    Debug.LogWarning("No GameFlowManager in scene.");
                    return;
                }

                flow.EnterTown(townSceneName, _shipBody, _ship.transform);
            }
        }
    }
}
