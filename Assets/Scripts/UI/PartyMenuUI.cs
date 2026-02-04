using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nebula
{
    public class PartyMenuUI : MonoBehaviour
    {
        [Serializable]
        public class PartySlot
        {
            public GameObject root;
            public Image portrait;
            public TMP_Text nameText;
            public TMP_Text levelText;
            public Slider hpBar;
            public TMP_Text hpText;
            public Button slotButton;
        }

        public PartySlot[] slots;
        public GameObject panel;

        public event Action<int> OnSlotSelected;
        public event Action<int, int> OnSwapRequested;

        private int _selectedIndex = -1;

        public void Show(List<MonsterInstance> party)
        {
            if (panel) panel.SetActive(true);
            _selectedIndex = -1;

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.root == null) continue;

                if (i >= party.Count || party[i] == null)
                {
                    slot.root.SetActive(false);
                    continue;
                }

                var mon = party[i];
                slot.root.SetActive(true);

                if (slot.portrait) slot.portrait.sprite = mon.def?.battleSprite;
                if (slot.nameText) slot.nameText.text = mon.def?.displayName ?? "???";
                if (slot.levelText) slot.levelText.text = $"Lv. {mon.level}";

                int maxHp = mon.EffectiveMaxHP();
                if (slot.hpBar)
                {
                    slot.hpBar.maxValue = maxHp;
                    slot.hpBar.value = Mathf.Max(0, mon.hp);
                }
                if (slot.hpText) slot.hpText.text = $"{Mathf.Max(0, mon.hp)}/{maxHp}";

                int idx = i;
                if (slot.slotButton)
                {
                    slot.slotButton.onClick.RemoveAllListeners();
                    slot.slotButton.onClick.AddListener(() => HandleSlotClick(idx));
                }
            }
        }

        public void Hide()
        {
            if (panel) panel.SetActive(false);
            _selectedIndex = -1;
        }

        private void HandleSlotClick(int index)
        {
            if (_selectedIndex < 0)
            {
                _selectedIndex = index;
                OnSlotSelected?.Invoke(index);
            }
            else if (_selectedIndex == index)
            {
                _selectedIndex = -1;
            }
            else
            {
                OnSwapRequested?.Invoke(_selectedIndex, index);
                _selectedIndex = -1;
            }
        }
    }
}
