using UnityEngine;

namespace Nebula
{
    public class ShipUpgradeStation : MonoBehaviour
    {
        // Call these from UI buttons

        public bool CanBuyAsteroidSensor(int cost)
            => !Progression.HasUpgrade(UpgradeId.AsteroidSensor) && Progression.Money >= cost;

        public void BuyAsteroidSensor(int cost)
        {
            if (Progression.HasUpgrade(UpgradeId.AsteroidSensor)) return;

            if (!Progression.SpendMoney(cost)) return;
            Progression.GrantUpgrade(UpgradeId.AsteroidSensor);
        }

        public bool CanBuyDeepSpaceEngine(int cost)
            => !Progression.HasUpgrade(UpgradeId.DeepSpaceEngine) && Progression.Money >= cost;

        public void BuyDeepSpaceEngine(int cost)
        {
            if (Progression.HasUpgrade(UpgradeId.DeepSpaceEngine)) return;

            if (!Progression.SpendMoney(cost)) return;
            Progression.GrantUpgrade(UpgradeId.DeepSpaceEngine);
        }
    }
}
