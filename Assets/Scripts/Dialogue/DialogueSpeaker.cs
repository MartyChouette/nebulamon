using UnityEngine;

namespace Nebula
{
    public class DialogueSpeaker : MonoBehaviour, IInteractable
    {
        [Header("Dialogue")]
        public string conversationId = "npc_default";
        public string prompt = "Talk";
        public NPCChirpProfile defaultChirpProfile;

        [Header("References")]
        public DialogueManager dialogueManager;

        public void Interact(GameObject interactor)
        {
            Debug.Log($"[DialogueSpeaker] Interact() on '{name}' convoId='{conversationId}'");

            if (dialogueManager == null)
            {
                dialogueManager = FindFirstObjectByType<DialogueManager>();
                if (dialogueManager == null)
                {
                    Debug.LogError("[DialogueSpeaker] No DialogueManager found in scene.");
                    return;
                }
            }

            if (string.IsNullOrEmpty(conversationId))
            {
                Debug.LogError("[DialogueSpeaker] conversationId is empty.");
                return;
            }

            dialogueManager.StartConversation(this, conversationId);
        }

        public string GetPrompt() => prompt;
        public Transform GetTransform() => transform;

        public void StartConversation(string convoId)
        {
            if (dialogueManager == null) dialogueManager = FindFirstObjectByType<DialogueManager>();
            if (dialogueManager == null)
            {
                Debug.LogError("[DialogueSpeaker] StartConversation() but no DialogueManager found.");
                return;
            }

            Debug.Log($"[DialogueSpeaker] StartConversation('{convoId}')");
            dialogueManager.StartConversation(this, convoId);
        }
    }
}
