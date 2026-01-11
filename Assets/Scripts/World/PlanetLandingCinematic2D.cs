using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Cinemachine;

namespace Nebula
{
    [RequireComponent(typeof(Collider2D))]
    public class PlanetLandingCinematic2D : MonoBehaviour
    {
        [Header("Planet")]
        [SerializeField] private PlanetId planetId = PlanetId.Planet1;

        [Header("Destination")]
        [SerializeField] private string townSceneName = "Town_Planet1";
        [SerializeField] private string destinationSpawnId = "Dock";

        [Header("Input")]
        [SerializeField] private InputActionReference interactAction; // Button

        [Header("Cinemachine")]
        [Tooltip("Virtual camera that is zoomed in on the planet. It should NOT be the default cam.")]
        [SerializeField] private CinemachineVirtualCamera landingVcam;

        [Tooltip("Priority to set landing camera to during cinematic (must be higher than gameplay cam).")]
        [SerializeField] private int landingCamPriority = 50;

        [Header("Ship Visual")]
        [Tooltip("If empty, will use the Player object transform that entered the trigger.")]
        [SerializeField] private Transform shipToScale;

        [SerializeField] private float shipScaleMultiplierAtEnd = 0.15f;
        [SerializeField] private float shipScaleTime = 0.8f;

        [Header("Fade / Loading Overlay")]
        [Tooltip("CanvasGroup controlling your full-screen 'clouds/atmosphere' overlay. You create the UI.")]
        [SerializeField] private CanvasGroup loadingOverlay;

        [SerializeField] private float fadeToOverlayTime = 0.5f;
        [SerializeField] private float holdOverlayTime = 0.25f;

        [Header("Optional: Gate landing behind an upgrade")]
        [SerializeField] private bool requireUpgrade = false;
        [SerializeField] private UpgradeId requiredUpgrade = UpgradeId.AsteroidSensor;

        [Header("Optional: Prompt UI")]
        [SerializeField] private InteractPromptUI promptUI;
        [SerializeField] private string promptText = "Land";

        private bool _playerInRange;
        private bool _isLanding;
        private Transform _playerShip;
        private Vector3 _shipStartScale;
        private int _landingCamStartPriority;

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

        private void Start()
        {
            if (loadingOverlay != null)
            {
                loadingOverlay.alpha = 0f;
                loadingOverlay.blocksRaycasts = false;
                loadingOverlay.interactable = false;
                loadingOverlay.gameObject.SetActive(false);
            }

            if (landingVcam != null)
                _landingCamStartPriority = landingVcam.Priority;
        }

        private void Update()
        {
            if (!_playerInRange || _isLanding) return;

            if (promptUI != null)
                promptUI.Show(promptText, transform);

            if (interactAction != null && interactAction.action.WasPressedThisFrame())
            {
                if (requireUpgrade && !Progression.HasUpgrade(requiredUpgrade))
                {
                    // You can plug in dialogue here if you want.
                    return;
                }

                StartCoroutine(LandRoutine());
            }
        }

        private IEnumerator LandRoutine()
        {
            _isLanding = true;

            // Decide which ship to scale
            if (shipToScale == null) shipToScale = _playerShip;
            if (shipToScale != null) _shipStartScale = shipToScale.localScale;

            // Switch to landing camera (blend handled by Cinemachine Brain)
            if (landingVcam != null)
            {
                _landingCamStartPriority = landingVcam.Priority;
                landingVcam.Priority = landingCamPriority;
            }

            // Scale ship down over time
            float t = 0f;
            float dur = Mathf.Max(0.01f, shipScaleTime);
            Vector3 endScale = (_shipStartScale == Vector3.zero ? Vector3.one : _shipStartScale) * shipScaleMultiplierAtEnd;

            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                float eased = Mathf.SmoothStep(0f, 1f, t);

                if (shipToScale != null)
                    shipToScale.localScale = Vector3.Lerp(_shipStartScale, endScale, eased);

                yield return null;
            }

            // Fade to overlay (clouds/atmosphere/loading)
            yield return FadeOverlay(0f, 1f, fadeToOverlayTime);

            // Record progression + set spawn + load
            Progression.LandOnPlanet(planetId);
            SceneSpawnRouter.NextSpawnId = destinationSpawnId;

            yield return new WaitForSeconds(holdOverlayTime);

            SceneManager.LoadScene(townSceneName);
        }

        private IEnumerator FadeOverlay(float from, float to, float duration)
        {
            if (loadingOverlay == null) yield break;

            loadingOverlay.gameObject.SetActive(true);
            loadingOverlay.blocksRaycasts = true;

            float t = 0f;
            duration = Mathf.Max(0.01f, duration);

            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                loadingOverlay.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            loadingOverlay.alpha = to;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            _playerInRange = true;
            _playerShip = other.transform;

            if (promptUI != null) promptUI.Show(promptText, transform);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            _playerInRange = false;
            if (promptUI != null) promptUI.Hide();
        }
    }
}
