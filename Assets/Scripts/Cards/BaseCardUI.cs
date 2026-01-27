using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Nebula
{
    /// <summary>
    /// Base class for all card UI panels.
    /// Provides common functionality: show/hide, background layer, portrait display.
    /// Derived classes add type-specific fields (stats, etc.).
    /// </summary>
    public abstract class BaseCardUI : MonoBehaviour
    {
        [Header("Root")]
        [Tooltip("Root GameObject to toggle visibility. If null, uses this.gameObject.")]
        [SerializeField] protected GameObject root;

        [Header("Common Elements")]
        [Tooltip("Image component for the main portrait/sprite")]
        [SerializeField] protected Image portraitImage;

        [Tooltip("Text component for the display name")]
        [SerializeField] protected TMP_Text nameText;

        [Header("Background Layer")]
        [Tooltip("Optional background image layer behind the card")]
        [SerializeField] protected Image backgroundImage;

        [Tooltip("Root object for the background (to toggle visibility separately)")]
        [SerializeField] protected GameObject backgroundRoot;

        /// <summary>True if the card is currently visible.</summary>
        public bool IsVisible => GetRoot().activeInHierarchy;

        protected virtual void Awake()
        {
            Hide();
        }

        /// <summary>
        /// Hides this card panel.
        /// </summary>
        public virtual void Hide()
        {
            GetRoot().SetActive(false);
            ClearData();
        }

        /// <summary>
        /// Shows the card with common ICardData fields.
        /// Override in derived classes to handle type-specific data.
        /// </summary>
        protected virtual void ShowCommon(ICardData data)
        {
            if (data == null)
            {
                Debug.LogWarning($"[{GetType().Name}] ShowCommon called with null data.");
                Hide();
                return;
            }

            GetRoot().SetActive(true);

            // Set portrait
            if (portraitImage != null)
            {
                portraitImage.sprite = data.CardSprite;
                portraitImage.enabled = data.CardSprite != null;
            }

            // Set name
            if (nameText != null)
            {
                nameText.text = data.DisplayName ?? "";
            }

            // Handle background layer
            SetBackground(data.BackgroundSprite);
        }

        /// <summary>
        /// Sets the optional background sprite layer.
        /// </summary>
        protected void SetBackground(Sprite bg)
        {
            if (backgroundRoot != null)
            {
                backgroundRoot.SetActive(bg != null);
            }

            if (backgroundImage != null)
            {
                backgroundImage.sprite = bg;
                backgroundImage.enabled = bg != null;
            }
        }

        /// <summary>
        /// Clears all data references. Override to clear type-specific data.
        /// </summary>
        protected virtual void ClearData()
        {
            if (portraitImage != null)
            {
                portraitImage.sprite = null;
                portraitImage.enabled = false;
            }

            if (nameText != null)
            {
                nameText.text = "";
            }

            SetBackground(null);
        }

        /// <summary>
        /// Gets the root GameObject for visibility toggling.
        /// </summary>
        protected GameObject GetRoot()
        {
            return root != null ? root : gameObject;
        }
    }
}
