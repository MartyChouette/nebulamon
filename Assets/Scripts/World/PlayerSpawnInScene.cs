using UnityEngine;

namespace Nebula
{
    public class PlayerSpawnInScene : MonoBehaviour
    {
        [SerializeField] private Transform player;

        private void Start()
        {
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }
            if (player == null) return;

            string id = SceneSpawnRouter.NextSpawnId;
            var points = FindObjectsByType<SpawnPoint2D>(FindObjectsSortMode.None);

            for (int i = 0; i < points.Length; i++)
            {
                if (points[i].spawnId == id)
                {
                    player.position = points[i].transform.position;
                    return;
                }
            }
        }
    }
}
