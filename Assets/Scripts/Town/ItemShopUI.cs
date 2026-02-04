using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nebula
{
    public class ItemShopUI : MonoBehaviour
    {
        [Serializable]
        public class ShopSlot
        {
            public GameObject root;
            public Image icon;
            public TMP_Text nameText;
            public TMP_Text priceText;
            public TMP_Text quantityText;
            public Button actionButton;
        }

        [Header("References")]
        public ShopInventory shopInventory;

        [Header("Tabs")]
        public Button buyTab;
        public Button sellTab;

        [Header("Slots")]
        public ShopSlot[] slots;

        [Header("Footer")]
        public TMP_Text moneyText;
        public TMP_Text messageText;
        public Button closeButton;

        private bool _isBuyMode = true;

        private void OnEnable()
        {
            _isBuyMode = true;

            if (buyTab)
            {
                buyTab.onClick.RemoveAllListeners();
                buyTab.onClick.AddListener(() => { _isBuyMode = true; Refresh(); });
            }

            if (sellTab)
            {
                sellTab.onClick.RemoveAllListeners();
                sellTab.onClick.AddListener(() => { _isBuyMode = false; Refresh(); });
            }

            if (closeButton)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() => gameObject.SetActive(false));
            }

            Refresh();
        }

        private void Refresh()
        {
            if (_isBuyMode)
                RefreshBuy();
            else
                RefreshSell();

            if (moneyText) moneyText.text = $"Money: {Progression.Money}";
        }

        private void RefreshBuy()
        {
            if (buyTab) buyTab.interactable = false;
            if (sellTab) sellTab.interactable = true;

            var items = shopInventory != null ? shopInventory.itemsForSale : null;

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.root == null) continue;

                if (items == null || i >= items.Count || items[i] == null)
                {
                    slot.root.SetActive(false);
                    continue;
                }

                var item = items[i];
                slot.root.SetActive(true);

                if (slot.icon) slot.icon.sprite = item.icon;
                if (slot.nameText) slot.nameText.text = item.displayName;
                if (slot.priceText) slot.priceText.text = $"{item.buyPrice}g";
                if (slot.quantityText) slot.quantityText.text = "";

                bool canBuy = item.buyPrice > 0 && Progression.Money >= item.buyPrice;
                if (slot.actionButton)
                {
                    slot.actionButton.interactable = canBuy;
                    slot.actionButton.onClick.RemoveAllListeners();
                    var captured = item;
                    slot.actionButton.onClick.AddListener(() => BuyItem(captured));
                }
            }

            if (messageText) messageText.text = "What would you like to buy?";
        }

        private void RefreshSell()
        {
            if (buyTab) buyTab.interactable = true;
            if (sellTab) sellTab.interactable = false;

            var inventory = Progression.GetInventory();
            var catalog = ItemCatalog.Instance;

            int shown = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.root == null) continue;

                if (shown >= inventory.Count)
                {
                    slot.root.SetActive(false);
                    continue;
                }

                var invSlot = inventory[shown];
                var item = catalog != null ? catalog.GetById(invSlot.itemId) : null;

                if (item == null || item.sellPrice <= 0)
                {
                    // Skip unsellable items, try next
                    slot.root.SetActive(false);
                    shown++;
                    i--; // Re-check this UI slot with the next inventory entry
                    continue;
                }

                slot.root.SetActive(true);

                if (slot.icon) slot.icon.sprite = item.icon;
                if (slot.nameText) slot.nameText.text = item.displayName;
                if (slot.priceText) slot.priceText.text = $"{item.sellPrice}g";
                if (slot.quantityText) slot.quantityText.text = $"x{invSlot.quantity}";

                if (slot.actionButton)
                {
                    slot.actionButton.interactable = true;
                    slot.actionButton.onClick.RemoveAllListeners();
                    var captured = item;
                    slot.actionButton.onClick.AddListener(() => SellItem(captured));
                }

                shown++;
            }

            if (messageText) messageText.text = "Select an item to sell.";
        }

        private void BuyItem(ItemDefinition item)
        {
            if (item == null || item.buyPrice <= 0) return;
            if (!Progression.SpendMoney(item.buyPrice))
            {
                if (messageText) messageText.text = "Not enough money!";
                return;
            }

            Progression.AddItem(item.itemId);
            Progression.Save();

            if (messageText) messageText.text = $"Bought {item.displayName}!";
            Refresh();
        }

        private void SellItem(ItemDefinition item)
        {
            if (item == null || item.sellPrice <= 0) return;
            if (!Progression.RemoveItem(item.itemId))
            {
                if (messageText) messageText.text = "You don't have that item!";
                return;
            }

            Progression.AddMoney(item.sellPrice);
            Progression.Save();

            if (messageText) messageText.text = $"Sold {item.displayName} for {item.sellPrice}g!";
            Refresh();
        }
    }
}
