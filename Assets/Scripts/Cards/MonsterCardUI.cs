using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Nebula
{
    /// <summary>
    /// UI component for displaying monster profile cards.
    /// Shows portrait, name, element, and key stats.
    /// Can display either a MonsterDefinition (template) or MonsterInstance (runtime).
    /// </summary>
    public class MonsterCardUI : BaseCardUI
    {
        [Header("Monster-Specific Elements")]
        [Tooltip("Text for the monster's element type")]
        [SerializeField] private TMP_Text elementText;

        [Tooltip("Text for HP display (current/max or just max)")]
        [SerializeField] private TMP_Text hpText;

        [Tooltip("Text for speed stat")]
        [SerializeField] private TMP_Text speedText;

        [Tooltip("Text for attack stats")]
        [SerializeField] private TMP_Text attackText;

        [Tooltip("Text for defense stats")]
        [SerializeField] private TMP_Text defenseText;

        [Tooltip("Optional element icon image")]
        [SerializeField] private Image elementIcon;

        private MonsterDefinition _currentDef;
        private MonsterInstance _currentInstance;

        /// <summary>
        /// Returns the currently displayed monster definition.
        /// </summary>
        public MonsterDefinition CurrentDefinition => _currentDef;

        /// <summary>
        /// Returns the currently displayed monster instance (if showing runtime data).
        /// </summary>
        public MonsterInstance CurrentInstance => _currentInstance;

        /// <summary>
        /// Shows the monster card with definition data (template stats).
        /// </summary>
        public void Show(MonsterDefinition data)
        {
            if (data == null)
            {
                Hide();
                return;
            }

            _currentDef = data;
            _currentInstance = null;
            ShowCommon(data);
            PopulateStats(data, null);
        }

        /// <summary>
        /// Shows the monster card with instance data (runtime HP, status, etc.).
        /// </summary>
        public void Show(MonsterInstance instance)
        {
            if (instance == null || instance.def == null)
            {
                Hide();
                return;
            }

            _currentDef = instance.def;
            _currentInstance = instance;
            ShowCommon(instance.def);
            PopulateStats(instance.def, instance);
        }

        /// <summary>
        /// Updates the card display with current data.
        /// </summary>
        public void Refresh()
        {
            if (!IsVisible) return;

            if (_currentInstance != null)
            {
                Show(_currentInstance);
            }
            else if (_currentDef != null)
            {
                Show(_currentDef);
            }
        }

        private void PopulateStats(MonsterDefinition def, MonsterInstance instance)
        {
            // Element
            if (elementText != null)
            {
                elementText.text = def.element.ToString();
            }

            // HP: show current/max if instance, else just max
            if (hpText != null)
            {
                if (instance != null)
                {
                    hpText.text = $"HP: {instance.hp}/{def.maxHP}";
                }
                else
                {
                    hpText.text = $"HP: {def.maxHP}";
                }
            }

            // Speed
            if (speedText != null)
            {
                speedText.text = $"SPD: {def.speed}";
            }

            // Attack (combined physical and elemental)
            if (attackText != null)
            {
                attackText.text = $"ATK: {def.physAttack}/{def.elemAttack}";
            }

            // Defense (combined physical and elemental)
            if (defenseText != null)
            {
                defenseText.text = $"DEF: {def.physDefense}/{def.elemDefense}";
            }
        }

        protected override void ClearData()
        {
            base.ClearData();
            _currentDef = null;
            _currentInstance = null;

            if (elementText != null) elementText.text = "";
            if (hpText != null) hpText.text = "";
            if (speedText != null) speedText.text = "";
            if (attackText != null) attackText.text = "";
            if (defenseText != null) defenseText.text = "";
        }
    }
}
