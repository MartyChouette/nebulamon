using UnityEngine;
using TMPro;

namespace Nebula
{
    /// <summary>
    /// UI component for displaying ship profile cards.
    /// Shows ship sprite, name, class, stats, and description.
    /// </summary>
    public class ShipCardUI : BaseCardUI
    {
        [Header("Ship-Specific Elements")]
        [Tooltip("Text for the ship's class/type")]
        [SerializeField] private TMP_Text classText;

        [Tooltip("Text for hull stat")]
        [SerializeField] private TMP_Text hullText;

        [Tooltip("Text for shields stat")]
        [SerializeField] private TMP_Text shieldsText;

        [Tooltip("Text for speed stat")]
        [SerializeField] private TMP_Text speedText;

        [Tooltip("Text for cargo capacity")]
        [SerializeField] private TMP_Text cargoText;

        [Tooltip("Text for description")]
        [SerializeField] private TMP_Text descriptionText;

        private ShipDefinition _currentData;

        /// <summary>
        /// Returns the currently displayed ship definition.
        /// </summary>
        public ShipDefinition CurrentData => _currentData;

        /// <summary>
        /// Shows the ship card with the given data.
        /// </summary>
        public void Show(ShipDefinition data)
        {
            if (data == null)
            {
                Hide();
                return;
            }

            _currentData = data;
            ShowCommon(data);
            PopulateStats(data);
        }

        /// <summary>
        /// Updates the card display with current data.
        /// </summary>
        public void Refresh()
        {
            if (_currentData != null && IsVisible)
            {
                Show(_currentData);
            }
        }

        private void PopulateStats(ShipDefinition data)
        {
            // Class
            if (classText != null)
            {
                classText.text = data.shipClass ?? "";
                classText.gameObject.SetActive(!string.IsNullOrEmpty(data.shipClass));
            }

            // Hull
            if (hullText != null)
            {
                hullText.text = $"Hull: {data.hull}";
            }

            // Shields
            if (shieldsText != null)
            {
                shieldsText.text = $"Shields: {data.shields}";
            }

            // Speed
            if (speedText != null)
            {
                speedText.text = $"Speed: {data.speed}";
            }

            // Cargo
            if (cargoText != null)
            {
                cargoText.text = $"Cargo: {data.cargo}";
            }

            // Description
            if (descriptionText != null)
            {
                descriptionText.text = data.description ?? "";
                descriptionText.gameObject.SetActive(!string.IsNullOrEmpty(data.description));
            }
        }

        protected override void ClearData()
        {
            base.ClearData();
            _currentData = null;

            if (classText != null) classText.text = "";
            if (hullText != null) hullText.text = "";
            if (shieldsText != null) shieldsText.text = "";
            if (speedText != null) speedText.text = "";
            if (cargoText != null) cargoText.text = "";
            if (descriptionText != null) descriptionText.text = "";
        }
    }
}
