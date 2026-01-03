using UnityEngine;

namespace SpaceGame
{
    public class OverworldSpawnApplier : MonoBehaviour
    {
        [Tooltip("If null, will look for Rigidbody2D on this object.")]
        public Rigidbody2D playerBody;

        private void Start()
        {
            var flow = GameFlowManager.Instance;
            if (flow == null || !flow.HasReturnPayload)
                return;

            if (playerBody == null) playerBody = GetComponent<Rigidbody2D>();

            transform.position = flow.ReturnPosition;
            transform.rotation = Quaternion.Euler(0f, 0f, flow.ReturnRotationZ);

            if (playerBody != null)
            {
                playerBody.linearVelocity = flow.ReturnVelocity;
                playerBody.angularVelocity = flow.ReturnAngularVelocity;
            }

            flow.ClearReturnPayload();
        }
    }
}
