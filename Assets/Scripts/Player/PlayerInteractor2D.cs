// Assets/Scripts/Player/PlayerInteractor2D.cs
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nebula
{
    public class PlayerInteractor2D : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionReference interactAction;

        [Header("Scan")]
        [SerializeField] private float radius = 0.8f;
        [SerializeField] private LayerMask interactableLayer;

        [Header("Optional UI")]
        [SerializeField] private InteractPromptUI promptUI;

        private IInteractable _current;
        private readonly Collider2D[] _hitBuffer = new Collider2D[16];

        private void OnEnable()
        {
            if (interactAction != null) interactAction.action.Enable();
        }

        private void OnDisable()
        {
            if (interactAction != null) interactAction.action.Disable();
        }

        private void Update()
        {
            FindBestInteractable();

            if (promptUI != null)
            {
                if (_current != null) promptUI.Show(_current.GetPrompt(), _current.GetTransform());
                else promptUI.Hide();
            }

            if (_current != null && interactAction != null && interactAction.action.WasPressedThisFrame())
            {
                _current.Interact(gameObject);
            }
        }

        private void FindBestInteractable()
        {
            int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, radius, _hitBuffer, interactableLayer);

            IInteractable best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                if (_hitBuffer[i] == null) continue;

                IInteractable interactable = _hitBuffer[i].GetComponent<IInteractable>();
                if (interactable == null)
                    interactable = _hitBuffer[i].GetComponentInParent<IInteractable>();
                if (interactable == null) continue;

                Transform t = interactable.GetTransform();
                if (t == null) continue;

                float d = Vector2.Distance(transform.position, t.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = interactable;
                }
            }

            _current = best;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
