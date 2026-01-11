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
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, interactableLayer);

            IInteractable best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null) continue;

                ConsiderBehaviours(hits[i].GetComponents<MonoBehaviour>(), ref best, ref bestDist);
                ConsiderBehaviours(hits[i].GetComponentsInParent<MonoBehaviour>(), ref best, ref bestDist);
            }

            _current = best;
        }

        private void ConsiderBehaviours(MonoBehaviour[] mbs, ref IInteractable best, ref float bestDist)
        {
            if (mbs == null) return;

            for (int i = 0; i < mbs.Length; i++)
            {
                if (mbs[i] == null) continue;
                if (mbs[i] is not IInteractable interactable) continue;

                Transform t = interactable.GetTransform();
                if (t == null) continue;

                float d = Vector2.Distance(transform.position, t.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = interactable;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
