using UnityEngine;

namespace Nebula
{
    public class TownServiceHub : MonoBehaviour
    {
        [Header("Panels (hook these up)")]
        [SerializeField] private GameObject shipUpgradePanel;
        [SerializeField] private GameObject healPanel;
        [SerializeField] private GameObject skillShopPanel;
        [SerializeField] private GameObject npcHousePanel;
        [SerializeField] private GameObject genericShopPanel;

        public void Open(TownBuildingDoor.DoorKind kind)
        {
            CloseAll();

            switch (kind)
            {
                case TownBuildingDoor.DoorKind.ShipUpgrade:
                    if (shipUpgradePanel != null) shipUpgradePanel.SetActive(true);
                    break;
                case TownBuildingDoor.DoorKind.HealCenter:
                    if (healPanel != null) healPanel.SetActive(true);
                    break;
                case TownBuildingDoor.DoorKind.SkillShop:
                    if (skillShopPanel != null) skillShopPanel.SetActive(true);
                    break;
                case TownBuildingDoor.DoorKind.NPCHouse:
                    if (npcHousePanel != null) npcHousePanel.SetActive(true);
                    break;
                case TownBuildingDoor.DoorKind.GenericShop:
                    if (genericShopPanel != null) genericShopPanel.SetActive(true);
                    break;
                default:
                    break;
            }
        }

        public void CloseAll()
        {
            if (shipUpgradePanel != null) shipUpgradePanel.SetActive(false);
            if (healPanel != null) healPanel.SetActive(false);
            if (skillShopPanel != null) skillShopPanel.SetActive(false);
            if (npcHousePanel != null) npcHousePanel.SetActive(false);
            if (genericShopPanel != null) genericShopPanel.SetActive(false);
        }
    }
}
