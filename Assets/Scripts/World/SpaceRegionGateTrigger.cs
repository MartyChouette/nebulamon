using UnityEngine;

namespace Nebula
{
    public class SpaceRegionGateTrigger : MonoBehaviour
    {
        [Header("Gate requirement (pick one)")]
        [SerializeField] private bool useUpgradeRequirement = true;

        [SerializeField] private UpgradeId requiredUpgrade = UpgradeId.AsteroidSensor;

        [Tooltip("If not using upgrade requirement, set a flag key here (e.g. seen_intro_gate)")]
        [SerializeField] private string requiredFlagKey = "";

        [Header("Blocked feedback")]
        [SerializeField] private string blockedMessageConversation = "gate_blocked";
        [SerializeField] private DialogueSpeaker gateSpeaker; // optional

        [SerializeField] private float pushBackDistance = 0.35f;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            bool allowed = true;

            if (useUpgradeRequirement)
            {
                allowed = Progression.HasUpgrade(requiredUpgrade);
            }
            else if (!string.IsNullOrWhiteSpace(requiredFlagKey))
            {
                allowed = Progression.GetFlag(requiredFlagKey);
            }

            if (allowed) return;

            // simple push-back
            Vector3 dir = (other.transform.position - transform.position).normalized;
            other.transform.position += dir * pushBackDistance;

            if (gateSpeaker != null)
                gateSpeaker.StartConversation(blockedMessageConversation);
        }
    }
}
