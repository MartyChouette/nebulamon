using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nebula
{
    public class BestiaryUI : MonoBehaviour
    {
        [Serializable]
        public class EntrySlot
        {
            public GameObject root;
            public Image portrait;
            public TMP_Text nameText;
            public TMP_Text elementText;
            public TMP_Text statsText;
            public TMP_Text descriptionText;
        }

        [Header("Layout")]
        public GameObject panel;
        public EntrySlot[] entrySlots;

        [Header("Header")]
        public TMP_Text headerText;

        [Header("Navigation")]
        public Button prevPageButton;
        public Button nextPageButton;
        public Button closeButton;

        [Header("Unknown Appearance")]
        public Color silhouetteColor = Color.black;

        private int _page;
        private int _totalPages;

        private void OnEnable()
        {
            _page = 0;
            Refresh();

            if (closeButton)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() =>
                {
                    if (panel) panel.SetActive(false);
                });
            }

            if (prevPageButton)
            {
                prevPageButton.onClick.RemoveAllListeners();
                prevPageButton.onClick.AddListener(() => { _page = Mathf.Max(0, _page - 1); Refresh(); });
            }

            if (nextPageButton)
            {
                nextPageButton.onClick.RemoveAllListeners();
                nextPageButton.onClick.AddListener(() => { _page = Mathf.Min(_totalPages - 1, _page + 1); Refresh(); });
            }
        }

        public void Show()
        {
            if (panel) panel.SetActive(true);
            _page = 0;
            Refresh();
        }

        private void Refresh()
        {
            var catalog = MonsterCatalog.Instance;
            if (catalog == null)
            {
                HideAllSlots();
                return;
            }

            int total = catalog.allMonsters.Count;
            int perPage = entrySlots.Length;
            _totalPages = Mathf.Max(1, Mathf.CeilToInt((float)total / perPage));
            _page = Mathf.Clamp(_page, 0, _totalPages - 1);

            int startIdx = _page * perPage;

            if (headerText)
                headerText.text = $"Bestiary  Seen: {Progression.SeenCount()}  Caught: {Progression.CaughtCount()}";

            if (prevPageButton) prevPageButton.interactable = _page > 0;
            if (nextPageButton) nextPageButton.interactable = _page < _totalPages - 1;

            for (int i = 0; i < entrySlots.Length; i++)
            {
                var slot = entrySlots[i];
                if (slot == null || slot.root == null) continue;

                int monIdx = startIdx + i;
                if (monIdx >= total)
                {
                    slot.root.SetActive(false);
                    continue;
                }

                var def = catalog.allMonsters[monIdx];
                if (def == null)
                {
                    slot.root.SetActive(false);
                    continue;
                }

                slot.root.SetActive(true);

                bool seen = Progression.HasSeen(def.monsterId);
                bool caught = Progression.HasCaught(def.monsterId);

                if (!seen)
                {
                    // Unknown
                    if (slot.portrait)
                    {
                        slot.portrait.sprite = null;
                        slot.portrait.color = new Color(0, 0, 0, 0.2f);
                    }
                    if (slot.nameText) slot.nameText.text = "???";
                    if (slot.elementText) slot.elementText.text = "";
                    if (slot.statsText) slot.statsText.text = "";
                    if (slot.descriptionText) slot.descriptionText.text = "";
                }
                else if (!caught)
                {
                    // Seen but not caught: silhouette + name
                    if (slot.portrait)
                    {
                        slot.portrait.sprite = def.battleSprite;
                        slot.portrait.color = silhouetteColor;
                    }
                    if (slot.nameText) slot.nameText.text = def.displayName;
                    if (slot.elementText) slot.elementText.text = def.element.ToString();
                    if (slot.statsText) slot.statsText.text = "";
                    if (slot.descriptionText) slot.descriptionText.text = "";
                }
                else
                {
                    // Caught: full info
                    if (slot.portrait)
                    {
                        slot.portrait.sprite = def.battleSprite;
                        slot.portrait.color = Color.white;
                    }
                    if (slot.nameText) slot.nameText.text = def.displayName;
                    if (slot.elementText) slot.elementText.text = def.element.ToString();
                    if (slot.statsText)
                    {
                        slot.statsText.text =
                            $"HP {def.maxHP}  Spd {def.speed}\n" +
                            $"PAtk {def.physAttack}  PDef {def.physDefense}\n" +
                            $"EAtk {def.elemAttack}  EDef {def.elemDefense}";
                    }
                    if (slot.descriptionText)
                        slot.descriptionText.text = def.bestiaryDescription ?? "";
                }
            }
        }

        private void HideAllSlots()
        {
            for (int i = 0; i < entrySlots.Length; i++)
            {
                if (entrySlots[i]?.root != null) entrySlots[i].root.SetActive(false);
            }
        }
    }
}
