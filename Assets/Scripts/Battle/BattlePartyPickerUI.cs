using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nebula
{
    public class BattlePartyPickerUI : MonoBehaviour
    {
        [Serializable]
        public class PartySlot
        {
            public GameObject root;
            public TMP_Text nameText;
            public TMP_Text levelText;
            public Slider hpBar;
            public Button selectButton;
        }

        public PartySlot[] slots;
        public Button backButton;
        public GameObject panel;

        private Action<int> _onPicked;
        private Action _onBack;

        private void Awake()
        {
            if (panel) panel.SetActive(false);
        }

        public void Show(BattleSide side, int excludeIndex, Action<int> onPicked, Action onBack)
        {
            _onPicked = onPicked;
            _onBack = onBack;

            if (panel) panel.SetActive(true);

            var aliveIndices = side.GetAliveIndicesExcept(excludeIndex);

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.root == null) continue;

                if (i >= aliveIndices.Count)
                {
                    slot.root.SetActive(false);
                    continue;
                }

                int partyIdx = aliveIndices[i];
                var mon = side.party[partyIdx];

                slot.root.SetActive(true);
                if (slot.nameText) slot.nameText.text = mon.def.displayName;
                if (slot.levelText) slot.levelText.text = $"Lv. {mon.level}";
                if (slot.hpBar)
                {
                    slot.hpBar.maxValue = mon.EffectiveMaxHP();
                    slot.hpBar.value = Mathf.Max(0, mon.hp);
                }

                int idx = partyIdx;
                if (slot.selectButton)
                {
                    slot.selectButton.onClick.RemoveAllListeners();
                    slot.selectButton.onClick.AddListener(() =>
                    {
                        Hide();
                        _onPicked?.Invoke(idx);
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
