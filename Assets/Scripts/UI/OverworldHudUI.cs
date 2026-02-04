using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nebula
{
    public class OverworldHudUI : MonoBehaviour
    {
        [Serializable]
        public class PartyMiniCard
        {
            public GameObject root;
            public TMP_Text nameText;
            public TMP_Text levelText;
            public Slider hpBar;
        }

        [Header("Party")]
        public PartyMiniCard[] partyCards;

        [Header("Info")]
        public TMP_Text moneyText;
        public TMP_Text locationText;

        [Header("Auto-hide")]
        [Tooltip("Hide the HUD when dialogue or battle is active.")]
        public bool autoHide = true;

        private void OnEnable()
        {
            Progression.OnChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            Progression.OnChanged -= Refresh;
        }

        private void Update()
        {
            if (!autoHide) return;

            bool shouldHide = false;

            // Hide during dialogue
            if (DialogueManager.Instance != null)
                shouldHide = DialogueManager.Instance.IsOpen;

            // Hide during battle scene
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "BattleScreen")
                shouldHide = true;

            gameObject.SetActive(!shouldHide);
        }

        private void Refresh()
        {
            if (!Progression.IsLoaded) return;

            if (moneyText) moneyText.text = $"Money: {Progression.Money}";

            var data = Progression.Data;
            var catalog = MonsterCatalog.Instance;

            for (int i = 0; i < partyCards.Length; i++)
            {
                var card = partyCards[i];
                if (card == null || card.root == null) continue;

                int partyIdx = (i < data.partyIndices.Count) ? data.partyIndices[i] : -1;
                var owned = (partyIdx >= 0 && partyIdx < data.roster.Count) ? data.roster[partyIdx] : null;

                if (owned == null)
                {
                    card.root.SetActive(false);
                    continue;
                }

                card.root.SetActive(true);
                var def = catalog != null ? catalog.GetByMonsterId(owned.monsterId) : null;

                string name = def != null ? def.displayName : owned.monsterId.ToString();
                if (!string.IsNullOrEmpty(owned.nickname)) name = owned.nickname;
                if (card.nameText) card.nameText.text = name;
                if (card.levelText) card.levelText.text = $"Lv.{owned.level}";
                if (card.hpBar)
                {
                    int maxHp = def != null
                        ? Mathf.Max(1, Mathf.RoundToInt(def.maxHP + def.hpGrowth * (owned.level - 1)))
                        : 1;
                    card.hpBar.maxValue = maxHp;
                    card.hpBar.value = Mathf.Max(0, owned.currentHp);
                }
            }
        }

        public void SetLocation(string text)
        {
            if (locationText) locationText.text = text;
        }
    }
}
