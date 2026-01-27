using UnityEngine;
using UnityEngine.UI;

namespace Nebula
{
    /// <summary>
    /// Auto-configures button visual states for clear selection feedback.
    /// Add to any Button to instantly get proper highlight colors.
    /// </summary>
    [RequireComponent(typeof(Button))]
    [ExecuteInEditMode]
    public class ButtonStyler : MonoBehaviour
    {
        [Header("Auto-Apply Colors")]
        [SerializeField] private bool applyOnAwake = true;

        [Header("Color Scheme")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightedColor = new Color(1f, 1f, 0.8f, 1f); // Light yellow
        [SerializeField] private Color pressedColor = new Color(0.8f, 0.8f, 0.4f, 1f); // Dark yellow
        [SerializeField] private Color selectedColor = new Color(1f, 1f, 0f, 1f); // Bright yellow
        [SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        [Header("Timing")]
        [SerializeField] private float fadeDuration = 0.15f;

        private Button _button;

        private void Awake()
        {
            if (applyOnAwake)
            {
                ApplyStyle();
            }
        }

        private void OnValidate()
        {
            // Apply in editor when values change
            ApplyStyle();
        }

        [ContextMenu("Apply Style")]
        public void ApplyStyle()
        {
            _button = GetComponent<Button>();
            if (_button == null) return;

            var colors = _button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = highlightedColor;
            colors.pressedColor = pressedColor;
            colors.selectedColor = selectedColor;
            colors.disabledColor = disabledColor;
            colors.fadeDuration = fadeDuration;
            colors.colorMultiplier = 1f;

            _button.colors = colors;
        }

        /// <summary>
        /// Apply a preset color scheme.
        /// </summary>
        public void ApplyPreset(ButtonPreset preset)
        {
            switch (preset)
            {
                case ButtonPreset.Yellow:
                    normalColor = Color.white;
                    highlightedColor = new Color(1f, 1f, 0.8f, 1f);
                    pressedColor = new Color(0.8f, 0.8f, 0.4f, 1f);
                    selectedColor = new Color(1f, 1f, 0f, 1f);
                    break;

                case ButtonPreset.Cyan:
                    normalColor = Color.white;
                    highlightedColor = new Color(0.8f, 1f, 1f, 1f);
                    pressedColor = new Color(0.4f, 0.8f, 0.8f, 1f);
                    selectedColor = new Color(0f, 1f, 1f, 1f);
                    break;

                case ButtonPreset.Green:
                    normalColor = Color.white;
                    highlightedColor = new Color(0.8f, 1f, 0.8f, 1f);
                    pressedColor = new Color(0.4f, 0.8f, 0.4f, 1f);
                    selectedColor = new Color(0.5f, 1f, 0.5f, 1f);
                    break;

                case ButtonPreset.Orange:
                    normalColor = Color.white;
                    highlightedColor = new Color(1f, 0.9f, 0.8f, 1f);
                    pressedColor = new Color(0.9f, 0.6f, 0.3f, 1f);
                    selectedColor = new Color(1f, 0.7f, 0.2f, 1f);
                    break;
            }

            ApplyStyle();
        }

        public enum ButtonPreset
        {
            Yellow,
            Cyan,
            Green,
            Orange
        }
    }
}
