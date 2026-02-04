using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nebula
{
    public sealed class GameFlowManager : MonoBehaviour
    {
        public static GameFlowManager Instance { get; private set; }

        [Header("Scene Names")]
        [Tooltip("Your battle scene name in Build Settings.")]
        public string battleSceneName = "BattleScreen";

        // Return payload (overworld restore)
        public bool HasReturnPayload { get; private set; }
        public string ReturnSceneName { get; private set; } = "NebulaMap";
        public Vector2 ReturnPosition { get; private set; }
        public Vector2 ReturnVelocity { get; private set; }
        public float ReturnRotationZ { get; private set; }
        public float ReturnAngularVelocity { get; private set; }

        // Battle payload
        public EnemyDefinition PendingEnemy { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void StartBattle(EnemyDefinition enemy, Rigidbody2D playerBody, Transform playerTransform)
        {
            if (enemy == null)
            {
                Debug.LogWarning("StartBattle called with null enemy.");
                return;
            }

            CacheReturnPayloadFromPlayer(playerBody, playerTransform);

            PendingEnemy = enemy;
            StartCoroutine(TransitionToScene(battleSceneName));
        }

        public void EnterTown(string townSceneName, Rigidbody2D playerBody, Transform playerTransform)
        {
            if (string.IsNullOrWhiteSpace(townSceneName))
            {
                Debug.LogWarning("EnterTown called with empty townSceneName.");
                return;
            }

            CacheReturnPayloadFromPlayer(playerBody, playerTransform);

            PendingEnemy = null;
            SceneManager.LoadScene(townSceneName, LoadSceneMode.Single);
        }

        public void ReturnToOverworld()
        {
            if (!HasReturnPayload)
            {
                Debug.LogWarning("ReturnToOverworld called but no payload exists. Loading ReturnSceneName anyway.");
            }

            PendingEnemy = null;
            StartCoroutine(TransitionToScene(ReturnSceneName));
            // OverworldSpawnApplier will consume payload and clear it.
        }

        private IEnumerator TransitionToScene(string sceneName)
        {
            var fx = ScreenEffects.Instance;
            if (fx != null)
            {
                yield return fx.IrisWipeOut(0.5f);
            }

            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

            // Wipe in after load (next frame the new scene is active)
            yield return null;

            fx = ScreenEffects.Instance;
            if (fx != null)
            {
                yield return fx.IrisWipeIn(0.5f);
            }
            else
            {
                var wipe = TransitionWipeController.Instance;
                if (wipe != null) wipe.ClearWipe();
            }
        }

        public void ClearReturnPayload()
        {
            HasReturnPayload = false;
        }

        private void CacheReturnPayloadFromPlayer(Rigidbody2D body, Transform tr)
        {
            ReturnSceneName = SceneManager.GetActiveScene().name;

            if (tr != null)
            {
                ReturnPosition = tr.position;
                ReturnRotationZ = tr.eulerAngles.z;
            }

            if (body != null)
            {
                ReturnVelocity = body.linearVelocity;
                ReturnAngularVelocity = body.angularVelocity;
            }

            HasReturnPayload = true;
        }
    }
}
