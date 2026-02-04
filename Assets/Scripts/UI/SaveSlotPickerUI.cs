using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nebula
{
    public class SaveSlotPickerUI : MonoBehaviour
    {
        [Serializable]
        public class SlotButton
        {
            public Button button;
            public TMP_Text labelText;
            public TMP_Text detailText;
        }

        public GameObject panel;
        public SlotButton[] slotButtons;
        public Button backButton;

        private Action<int> _onPicked;
        private bool _allowEmpty;

        public void Show(Action<int> onSlotPicked, bool allowEmpty = true)
        {
            _onPicked = onSlotPicked;
            _allowEmpty = allowEmpty;

            if (panel) panel.SetActive(true);
            Refresh();

            if (backButton)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(() =>
                {
                    if (panel) panel.SetActive(false);
                });
            }
        }

        public void Hide()
        {
            if (panel) panel.SetActive(false);
        }

        private void Refresh()
        {
            for (int i = 0; i < slotButtons.Length; i++)
            {
                var sb = slotButtons[i];
                if (sb == null || sb.button == null) continue;

                int slot = i + 1; // slots 1, 2, 3
                var preview = Progression.GetSlotPreview(slot);

                if (preview == null)
                {
                    // Empty slot
                    if (sb.labelText) sb.labelText.text = $"Slot {slot}";
                    if (sb.detailText) sb.detailText.text = "Empty";
                    sb.button.interactable = _allowEmpty;
                }
                else
                {
                    if (sb.labelText) sb.labelText.text = $"Slot {slot}";

                    int hours = Mathf.FloorToInt(preview.playTimeSeconds / 3600f);
                    int minutes = Mathf.FloorToInt((preview.playTimeSeconds % 3600f) / 60f);
                    string starterStr = preview.starterMonster != MonsterId.None
                        ? preview.starterMonster.ToString()
                        : "---";

                    if (sb.detailText)
                        sb.detailText.text = $"Starter: {starterStr}  Money: {preview.money}  Party: {preview.rosterCount}  Time: {hours}h {minutes}m";

                    sb.button.interactable = true;
                }

                sb.button.onClick.RemoveAllListeners();
                int capturedSlot = slot;
                sb.button.onClick.AddListener(() =>
                {
                    Hide();
                    _onPicked?.Invoke(capturedSlot);
                });
            }
        }
    }
}
