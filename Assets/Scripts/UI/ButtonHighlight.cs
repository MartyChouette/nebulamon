using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Nebula
{
    /// <summary>
    /// Makes button selection obvious with visual feedback.
    /// Attach to any Button to get clear highlight when selected.
    ///
    /// Features:
    /// - Scale pulse on selection
    /// - Color change on selection
    /// - Optional selection indicator (arrow, bracket, etc.)
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class ButtonHighlight : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Scale Effect")]
        [Tooltip("Scale multiplier when selected")]
        [SerializeField] private float selectedScale = 1.1f;
        [SerializeField] private float scaleSpeed = 10f;

        [Header("Color Effect")]
        [Tooltip("Tint color when selected")]
        [SerializeField] private Color selectedColor = Color.yellow;
        [Tooltip("Apply color to this Graphic (Image or Text). If null, tries to find one.")]
        [SerializeField] private Graphic targetGraphic;

        [Header("Text Effect")]
        [Tooltip("Optional: Add prefix when selected (e.g., '> ')")]
        [SerializeField] private string selectedPrefix = "> ";
        [Tooltip("Optional: Add suffix when selected (e.g., ' <')")]
        [SerializeField] private string selectedSuffix = " <";
        [SerializeField] private TMP_Text targetText;

        [Header("Indicator")]
        [Tooltip("Optional: GameObject to show when selected (arrow, bracket, etc.)")]
        [SerializeField] private GameObject selectionIndicator;

        private Vector3 _originalScale;
        private Color _originalColor;
        private string _originalText;
        private bool _isSelected;
        private Selectable _selectable;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
            _originalScale = transform.localScale;

            // Find target graphic if not assigned
            if (targetGraphic == null)
                targetGraphic = GetComponent<Image>();

            if (targetGraphic != null)
                _originalColor = targetGraphic.color;

            // Find text if not assigned
            if (targetText == null)
                targetText = GetComponentInChildren<TMP_Text>();

            if (targetText != null)
                _originalText = targetText.text;

            // Hide indicator by default
            if (selectionIndicator != null)
                selectionIndicator.SetActive(false);
        }

        private void Update()
        {
            // Smooth scale transition
            Vector3 targetScale = _isSelected ? _originalScale * selectedScale : _originalScale;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * scaleSpeed);
        }

        public void OnSelect(BaseEventData eventData)
        {
            SetSelected(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            SetSelected(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Also highlight on mouse hover
            SetSelected(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Only unhighlight if not keyboard/gamepad selected
            if (EventSystem.current.currentSelectedGameObject != gameObject)
            {
                SetSelected(false);
            }
        }

        private void SetSelected(bool selected)
        {
            _isSelected = selected;

            // Color
            if (targetGraphic != null)
            {
                targetGraphic.color = selected ? selectedColor : _originalColor;
            }

            // Text prefix/suffix
            if (targetText != null && !string.IsNullOrEmpty(_originalText))
            {
                if (selected)
                {
                    targetText.text = selectedPrefix + _originalText + selectedSuffix;
                }
                else
                {
                    targetText.text = _originalText;
                }
            }

            // Indicator
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(selected);
            }
        }

        private void OnDisable()
        {
            // Reset on disable
            SetSelected(false);
            transform.localScale = _originalScale;
        }

        /// <summary>
        /// Call this if you change the button text at runtime.
        /// </summary>
        public void RefreshOriginalText()
        {
            if (targetText != null)
            {
                // Remove prefix/suffix if present
                string current = targetText.text;
                if (current.StartsWith(selectedPrefix))
                    current = current.Substring(selectedPrefix.Length);
                if (current.EndsWith(selectedSuffix))
                    current = current.Substring(0, current.Length - selectedSuffix.Length);

                _originalText = current;
            }
        }
    }
}
