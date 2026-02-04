using UnityEngine;

namespace Nebula
{
    public class TrainerNPC : MonoBehaviour, IInteractable
    {
        [Header("Trainer")]
        public EnemyDefinition enemyDefinition;

        [Header("Dialogue")]
        [Tooltip("What the trainer says after being defeated.")]
        public string defeatedDialogue = "You've already beaten me!";

        [Header("Prompt")]
        [SerializeField] private string prompt = "Challenge";

        public void Interact(GameObject interactor)
        {
            if (enemyDefinition == null) return;

            // Already defeated?
            if (!string.IsNullOrEmpty(enemyDefinition.trainerId) &&
                Progression.IsTrainerDefeated(enemyDefinition.trainerId))
            {
                // Show post-defeat dialogue via DialogueManager if available
                var dm = DialogueManager.Instance;
                if (dm != null)
                {
                    dm.ShowSingleLine(defeatedDialogue);
                }
                else
                {
                    Debug.Log($"Trainer {enemyDefinition.trainerId}: {defeatedDialogue}");
                }
                return;
            }

            // Start battle
            if (GameFlowManager.Instance == null) return;

            var rb = interactor.GetComponent<Rigidbody2D>();
            if (rb == null) rb = interactor.GetComponentInParent<Rigidbody2D>();

            GameFlowManager.Instance.StartBattle(enemyDefinition, rb, interactor.transform);
        }

        public string GetPrompt() => prompt;
        public Transform GetTransform() => transform;
    }
}
