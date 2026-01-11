using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Nebula
{
    [RequireComponent(typeof(Collider2D))]
    public class PlanetLandingTrigger2D : MonoBehaviour
    {
        [Header("Planet")]
        [SerializeField] private PlanetId planetId = PlanetId.Planet1;

        [Header("Destination")]
        [SerializeField] private string townSceneName = "Town_Planet1";
        [SerializeField] private string destinationSpawnId = "Dock";

        [Header("Input")]
        [SerializeField] private InputActionReference interactAction; // Button

        [Header("Landing Gate (optional)")]
        [SerializeField] private bool requireUpgrade = false;
        [SerializeField] private UpgradeId requiredUpgrade = UpgradeId.AsteroidSensor;

        [Header("Blocked Feedback (optional)")]
        [SerializeField] private DialogueSpeaker blockedSpeaker; // optional
        [SerializeField] private string blockedConversationId = "gate_blocked";

        [Header("Prompt (optional)")]
        [SerializeField] private InteractPromptUI promptUI; // optional
        [SerializeField] private string promptText = "Land";

        private bool _playerInRange;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col) col.isTrigger = true;
        }

        private void OnEnable()
        {
            if (interactAction != null) interactAction.action.Enable();
        }

        private void OnDisable()
        {
            if (interactAction != null) interactAction.action.Disable();
            if (promptUI != null) promptUI.Hide();
        }

        private void Update()
        {
            if (!_playerInRange) return;

            if (promptUI != null)
                promptUI.Show(promptText, transform);

            if (interactAction != null && interactAction.action.WasPressedThisFrame())
            {
                TryLand();
            }
        }

        private void TryLand()
        {
            if (requireUpgrade && !Progression.HasUpgrade(requiredUpgrade))
            {
                if (blockedSpeaker != null)
                    blockedSpeaker.StartConversation(blockedConversationId);
                return;
            }

            // Track you got here
            Progression.LandOnPlanet(planetId);

            // Set spawn point for the destination scene
            SceneSpawnRouter.NextSpawnId = destinationSpawnId;

            // Load town/spaceport scene
            SceneManager.LoadScene(townSceneName);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _playerInRange = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _playerInRange = false;
            if (promptUI != null) promptUI.Hide();
        }
    }
}
