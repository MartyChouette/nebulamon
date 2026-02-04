using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nebula
{
    public class MoveTutorUI : MonoBehaviour
    {
        [Serializable]
        public class TutorSlot
        {
            public GameObject root;
            public Image icon;
            public TMP_Text nameText;
            public TMP_Text descText;
            public Button selectButton;
        }

        [Serializable]
        public class MonsterSlot
        {
            public GameObject root;
            public TMP_Text nameText;
            public TMP_Text levelText;
            public Button selectButton;
        }

        [Serializable]
        public class MoveSlot
        {
            public GameObject root;
            public TMP_Text moveNameText;
            public Button selectButton;
        }

        [Header("Step 1: Item List")]
        public GameObject itemListPanel;
        public TutorSlot[] tutorSlots;

        [Header("Step 2: Monster List")]
        public GameObject monsterListPanel;
        public MonsterSlot[] monsterSlots;
        public Button monsterBackButton;

        [Header("Step 3: Move Slot")]
        public GameObject moveSlotPanel;
        public MoveSlot[] moveSlots;
        public Button moveBackButton;

        [Header("Footer")]
        public TMP_Text messageText;
        public Button closeButton;

        private ItemDefinition _selectedItem;
        private int _selectedPartyIdx;
        private readonly List<(ItemDefinition def, int qty)> _tutorItems = new();
        private readonly List<int> _compatiblePartyIndices = new();

        private void OnEnable()
        {
            _selectedItem = null;
            _selectedPartyIdx = -1;
            ShowItemList();

            if (closeButton)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() => gameObject.SetActive(false));
            }
        }

        // ── Step 1: Show tutor items from inventory ──

        private void ShowItemList()
        {
            SetPanelActive(itemListPanel, true);
            SetPanelActive(monsterListPanel, false);
            SetPanelActive(moveSlotPanel, false);

            _tutorItems.Clear();

            var inventory = Progression.GetInventory();
            var catalog = ItemCatalog.Instance;

            if (catalog != null)
            {
                foreach (var slot in inventory)
                {
                    var def = catalog.GetById(slot.itemId);
                    if (def != null && def.category == ItemCategory.MoveTutor && def.taughtMove != null)
                        _tutorItems.Add((def, slot.quantity));
                }
            }

            for (int i = 0; i < tutorSlots.Length; i++)
            {
                var slot = tutorSlots[i];
                if (slot == null || slot.root == null) continue;

                if (i >= _tutorItems.Count)
                {
                    slot.root.SetActive(false);
                    continue;
                }

                var (itemDef, qty) = _tutorItems[i];
                slot.root.SetActive(true);

                if (slot.icon) slot.icon.sprite = itemDef.icon;
                if (slot.nameText) slot.nameText.text = $"{itemDef.displayName} x{qty}";
                if (slot.descText) slot.descText.text = $"Teaches: {itemDef.taughtMove.moveName}";

                if (slot.selectButton)
                {
                    slot.selectButton.onClick.RemoveAllListeners();
                    var captured = itemDef;
                    slot.selectButton.onClick.AddListener(() => SelectItem(captured));
                }
            }

            if (messageText)
            {
                messageText.text = _tutorItems.Count > 0
                    ? "Choose a move tutor item."
                    : "No move tutor items in inventory.";
            }
        }

        private void SelectItem(ItemDefinition item)
        {
            _selectedItem = item;
            ShowMonsterList();
        }

        // ── Step 2: Show compatible party monsters ──

        private void ShowMonsterList()
        {
            SetPanelActive(itemListPanel, false);
            SetPanelActive(monsterListPanel, true);
            SetPanelActive(moveSlotPanel, false);

            _compatiblePartyIndices.Clear();

            var data = Progression.Data;
            var catalog = MonsterCatalog.Instance;

            if (data != null && _selectedItem != null)
            {
                for (int i = 0; i < data.partyIndices.Count; i++)
                {
                    int rosterIdx = data.partyIndices[i];
                    if (rosterIdx < 0 || rosterIdx >= data.roster.Count) continue;

                    var owned = data.roster[rosterIdx];

                    // Check compatibility
                    if (_selectedItem.compatibleMonsters != null && _selectedItem.compatibleMonsters.Count > 0)
                    {
                        if (!_selectedItem.compatibleMonsters.Contains(owned.monsterId))
                            continue;
                    }

                    // Skip if already knows the move
                    if (_selectedItem.taughtMove != null &&
                        owned.knownMoveNames.Contains(_selectedItem.taughtMove.moveName))
                        continue;

                    _compatiblePartyIndices.Add(i);
                }
            }

            for (int i = 0; i < monsterSlots.Length; i++)
            {
                var slot = monsterSlots[i];
                if (slot == null || slot.root == null) continue;

                if (i >= _compatiblePartyIndices.Count)
                {
                    slot.root.SetActive(false);
                    continue;
                }

                int partyPos = _compatiblePartyIndices[i];
                int rosterIdx = data.partyIndices[partyPos];
                var owned = data.roster[rosterIdx];
                var def = catalog != null ? catalog.GetByMonsterId(owned.monsterId) : null;

                slot.root.SetActive(true);

                string name = def != null ? def.displayName : owned.monsterId.ToString();
                if (!string.IsNullOrEmpty(owned.nickname)) name = owned.nickname;
                if (slot.nameText) slot.nameText.text = name;
                if (slot.levelText) slot.levelText.text = $"Lv. {owned.level}";

                if (slot.selectButton)
                {
                    slot.selectButton.onClick.RemoveAllListeners();
                    int capturedPartyPos = partyPos;
                    slot.selectButton.onClick.AddListener(() => SelectMonster(capturedPartyPos));
                }
            }

            if (monsterBackButton)
            {
                monsterBackButton.onClick.RemoveAllListeners();
                monsterBackButton.onClick.AddListener(ShowItemList);
            }

            if (messageText)
            {
                messageText.text = _compatiblePartyIndices.Count > 0
                    ? $"Who should learn {_selectedItem.taughtMove.moveName}?"
                    : "No compatible monsters in party.";
            }
        }

        private void SelectMonster(int partyPos)
        {
            _selectedPartyIdx = partyPos;
            ShowMoveSlots();
        }

        // ── Step 3: Pick move slot to replace ──

        private void ShowMoveSlots()
        {
            SetPanelActive(itemListPanel, false);
            SetPanelActive(monsterListPanel, false);
            SetPanelActive(moveSlotPanel, true);

            var data = Progression.Data;
            int rosterIdx = data.partyIndices[_selectedPartyIdx];
            var owned = data.roster[rosterIdx];

            for (int i = 0; i < moveSlots.Length; i++)
            {
                var slot = moveSlots[i];
                if (slot == null || slot.root == null) continue;

                if (i >= 4)
                {
                    slot.root.SetActive(false);
                    continue;
                }

                slot.root.SetActive(true);

                string moveName = (i < owned.knownMoveNames.Count) ? owned.knownMoveNames[i] : "(empty)";
                if (slot.moveNameText) slot.moveNameText.text = moveName;

                if (slot.selectButton)
                {
                    slot.selectButton.onClick.RemoveAllListeners();
                    int capturedSlot = i;
                    slot.selectButton.onClick.AddListener(() => ReplaceMove(capturedSlot));
                }
            }

            if (moveBackButton)
            {
                moveBackButton.onClick.RemoveAllListeners();
                moveBackButton.onClick.AddListener(ShowMonsterList);
            }

            if (messageText) messageText.text = $"Replace which move with {_selectedItem.taughtMove.moveName}?";
        }

        private void ReplaceMove(int moveSlotIdx)
        {
            if (_selectedItem == null || _selectedItem.taughtMove == null) return;

            var data = Progression.Data;
            int rosterIdx = data.partyIndices[_selectedPartyIdx];
            var owned = data.roster[rosterIdx];

            string newMoveName = _selectedItem.taughtMove.moveName;

            // Expand list if needed
            while (owned.knownMoveNames.Count <= moveSlotIdx)
                owned.knownMoveNames.Add("");

            string replaced = owned.knownMoveNames[moveSlotIdx];
            owned.knownMoveNames[moveSlotIdx] = newMoveName;

            // Consume item
            Progression.RemoveItem(_selectedItem.itemId);
            Progression.Save();

            string msg = string.IsNullOrEmpty(replaced) || replaced == "(empty)"
                ? $"Learned {newMoveName}!"
                : $"Replaced {replaced} with {newMoveName}!";

            if (messageText) messageText.text = msg;

            // Return to item list after a short beat
            _selectedItem = null;
            _selectedPartyIdx = -1;
            ShowItemList();
        }

        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }
    }
}
