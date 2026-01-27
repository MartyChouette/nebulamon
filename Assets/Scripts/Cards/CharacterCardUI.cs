using UnityEngine;
using TMPro;

namespace Nebula
{
    /// <summary>
    /// UI component for displaying character/NPC profile cards.
    /// Shows portrait, name, title, and description.
    /// </summary>
    public class CharacterCardUI : BaseCardUI
    {
        [Header("Character-Specific Elements")]
        [Tooltip("Text for the character's title/role")]
        [SerializeField] private TMP_Text titleText;

        [Tooltip("Text for the character's description")]
        [SerializeField] private TMP_Text descriptionText;

        private CharacterDefinition _currentData;

        /// <summary>
        /// Returns the currently displayed character definition, or null if hidden.
        /// </summary>
        public CharacterDefinition CurrentData => _currentData;

        /// <summary>
        /// Shows the character card with the given data.
        /// </summary>
        public void Show(CharacterDefinition data)
        {
            if (data == null)
            {
                Hide();
                return;
            }

            _currentData = data;
            ShowCommon(data);

            // Set character-specific fields
            if (titleText != null)
            {
                titleText.text = data.title ?? "";
                titleText.gameObject.SetActive(!string.IsNullOrEmpty(data.title));
            }

            if (descriptionText != null)
            {
                descriptionText.text = data.description ?? "";
                descriptionText.gameObject.SetActive(!string.IsNullOrEmpty(data.description));
            }
        }

        /// <summary>
        /// Updates the card display if already showing this character.
        /// Useful for dynamic data changes.
        /// </summary>
        public void Refresh()
        {
            if (_currentData != null && IsVisible)
            {
                Show(_currentData);
            }
        }

        protected override void ClearData()
        {
            base.ClearData();
            _currentData = null;

            if (titleText != null)
            {
                titleText.text = "";
            }

            if (descriptionText != null)
            {
                descriptionText.text = "";
            }
        }
    }
}
