using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nebula
{
    public class BattleItemPickerUI : MonoBehaviour
    {
        [Serializable]
        public class ItemSlot
        {
            public GameObject root;
            public Image icon;
            public TMP_Text nameText;
            public TMP_Text quantityText;
            public Button selectButton;
        }

        public ItemSlot[] slots;
        public Button backButton;
        public GameObject panel;

        private Action<ItemDefinition> _onPicked;
        private Action _onBack;

        private void Awake()
        {
            if (panel) panel.SetActive(false);
        }

        public void Show(Action<ItemDefinition> onPicked, Action onBack)
        {
            _onPicked = onPicked;
            _onBack = onBack;

            if (panel) panel.SetActive(true);

            var inventory = Progression.GetInventory();
            var catalog = ItemCatalog.Instance;
            var usable = new List<(ItemDefinition def, int qty)>();

            if (catalog != null)
            {
                foreach (var slot in inventory)
                {
                    var def = catalog.GetById(slot.itemId);
                    if (def != null && (def.category == ItemCategory.Heal || def.category == ItemCategory.CatchDevice))
                        usable.Add((def, slot.quantity));
                }
            }

            for (int i = 0; i < slots.Length; i++)
            {
                var s = slots[i];
                if (s == null || s.root == null) continue;

                if (i >= usable.Count)
                {
                    s.root.SetActive(false);
                    continue;
                }

                var (itemDef, qty) = usable[i];
                s.root.SetActive(true);
                if (s.icon) s.icon.sprite = itemDef.icon;
                if (s.nameText) s.nameText.text = itemDef.displayName;
                if (s.quantityText) s.quantityText.text = $"x{qty}";

                var captured = itemDef;
                if (s.selectButton)
                {
                    s.selectButton.onClick.RemoveAllListeners();
                    s.selectButton.onClick.AddListener(() =>
                    {
                        Hide();
                        _onPicked?.Invoke(captured);
                    });
                }
            }

            if (backButton)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(() =>
                {
                    Hide();
                    _onBack?.Invoke();
                });
            }
        }

        public void Hide()
        {
            if (panel) panel.SetActive(false);
        }
    }
}
