using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nebula
{
    public class TownBuildingDoor : MonoBehaviour, IInteractable
    {
        public enum DoorKind
        {
            ShipUpgrade,
            HealCenter,
            SkillShop,
            NPCHouse,
            GenericShop,
            SceneOnly
        }

        [Header("Door")]
        [SerializeField] private DoorKind kind = DoorKind.SceneOnly;
        [SerializeField] private string prompt = "Enter";

        [Header("Scene Transition (optional)")]
        [Tooltip("If set, loads this scene when entering.")]
        [SerializeField] private string sceneToLoad;
        [Tooltip("Spawn ID inside the destination scene.")]
        [SerializeField] private string destinationSpawnId = "Entrance";

        [Header("Service (optional)")]
        [SerializeField] private TownServiceHub serviceHub; // drag your hub in the scene

        public void Interact(GameObject interactor)
        {
            // 1) Scene transition if configured
            if (!string.IsNullOrWhiteSpace(sceneToLoad))
            {
                SceneSpawnRouter.NextSpawnId = destinationSpawnId;
                SceneManager.LoadScene(sceneToLoad);
                return;
            }

            // 2) Otherwise open a service via hub
            if (serviceHub != null)
                serviceHub.Open(kind);
            else
                Debug.LogWarning($"{name}: No sceneToLoad and no serviceHub assigned.");
        }

        public string GetPrompt() => prompt;
        public Transform GetTransform() => transform;
    }
}
