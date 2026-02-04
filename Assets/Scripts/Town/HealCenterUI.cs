using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nebula
{
    public class HealCenterUI : MonoBehaviour
    {
        [Serializable]
        public class PartySlot
        {
            public GameObject root;
            public TMP_Text nameText;
            public TMP_Text levelText;
            public Slider hpBar;
        }

        public PartySlot[] slots;
        public Button healAllButton;
        public Button closeButton;
        public TMP_Text messageText;

        private void OnEnable()
        {
            Refresh();

            if (closeButton)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() => gameObject.SetActive(false));
            }

            if (healAllButton)
            {
                healAllButton.onClick.RemoveAllListeners();
                healAllButton.onClick.AddListener(HealAll);
            }
        }

        private void Refresh()
        {
            var data = Progression.Data;
            if (data == null) { HideAllSlots(); return; }

            var catalog = MonsterCatalog.Instance;
            bool anyDamaged = false;

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.root == null) continue;

                // Map slot index to party member
                int partyIdx = (i < data.partyIndices.Count) ? data.partyIndices[i] : -1;
                var owned = (partyIdx >= 0 && partyIdx < data.roster.Count) ? data.roster[partyIdx] : null;

                if (owned == null)
                {
                    slot.root.SetActive(false);
                    continue;
                }

                var def = catalog != null ? catalog.GetByMonsterId(owned.monsterId) : null;
                int maxHp = CalcMaxHp(def, owned.level);

                slot.root.SetActive(true);

                string name = def != null ? def.displayName : owned.monsterId.ToString();
                if (!string.IsNullOrEmpty(owned.nickname)) name = owned.nickname;
                if (slot.nameText) slot.nameText.text = name;
                if (slot.levelText) slot.levelText.text = $"Lv. {owned.level}";
                if (slot.hpBar)
                {
                    slot.hpBar.maxValue = maxHp;
                    slot.hpBar.value = Mathf.Max(0, owned.currentHp);
                }

                if (owned.currentHp < maxHp) anyDamaged = true;
            }

            if (healAllButton) healAllButton.interactable = anyDamaged;
            if (messageText) messageText.text = anyDamaged ? "Your monsters need healing!" : "Everyone is healthy!";
        }

        private void HealAll()
        {
            var data = Progression.Data;
            if (data == null) return;

            var catalog = MonsterCatalog.Instance;

            for (int i = 0; i < data.partyIndices.Count; i++)
            {
                int idx = data.partyIndices[i];
                if (idx < 0 || idx >= data.roster.Count) continue;

                var owned = data.roster[idx];
                var def = catalog != null ? catalog.GetByMonsterId(owned.monsterId) : null;
                owned.currentHp = CalcMaxHp(def, owned.level);
            }

            Progression.Save();
            Refresh();

            if (messageText) messageText.text = "All healed up!";
        }

        private static int CalcMaxHp(MonsterDefinition def, int level)
        {
            if (def == null) return 1;
            return Mathf.Max(1, Mathf.RoundToInt(def.maxHP + def.hpGrowth * (level - 1)));
        }

        private void HideAllSlots()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i]?.root != null) slots[i].root.SetActive(false);
            }
        }
    }
}
