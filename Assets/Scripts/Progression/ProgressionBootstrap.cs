using UnityEngine;

namespace Nebula
{
    public class ProgressionBootstrap : MonoBehaviour
    {
        [SerializeField] private bool generateVillainAssignmentsOnNewGame = true;

        private void Awake()
        {
            Progression.Load();

            if (generateVillainAssignmentsOnNewGame && !Progression.HasChosenStarter)
            {
                Progression.GenerateVillainAssignmentsIfMissing();
            }
        }
    }
}
