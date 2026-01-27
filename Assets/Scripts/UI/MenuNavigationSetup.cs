using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Nebula
{
    /// <summary>
    /// Ensures proper navigation setup for menus.
    /// Attach to a parent containing buttons to auto-configure navigation.
    ///
    /// Also ensures there's always a selected button when using keyboard/gamepad.
    /// </summary>
    public class MenuNavigationSetup : MonoBehaviour
    {
        [Header("Navigation")]
        [Tooltip("First button to select when menu activates")]
        [SerializeField] private Selectable firstSelected;

        [Tooltip("Auto-find buttons in children if not manually specified")]
        [SerializeField] private bool autoFindButtons = true;

        [Tooltip("Navigation mode for found buttons")]
        [SerializeField] private Navigation.Mode navigationMode = Navigation.Mode.Vertical;

        [Header("Selection Persistence")]
        [Tooltip("Re-select a button if nothing is selected (keyboard/gamepad friendly)")]
        [SerializeField] private bool ensureSelectionExists = true;

        [Tooltip("Delay before checking selection (prevents fighting with other scripts)")]
        [SerializeField] private float selectionCheckDelay = 0.1f;

        private List<Selectable> _buttons = new List<Selectable>();
        private Selectable _lastSelected;
        private float _nextSelectionCheck;

        private void OnEnable()
        {
            if (autoFindButtons)
            {
                SetupNavigation();
            }

            // Select first button
            if (firstSelected != null)
            {
                SelectButton(firstSelected);
            }
            else if (_buttons.Count > 0)
            {
                SelectButton(_buttons[0]);
            }

            _nextSelectionCheck = Time.unscaledTime + selectionCheckDelay;
        }

        private void Update()
        {
            if (!ensureSelectionExists) return;
            if (Time.unscaledTime < _nextSelectionCheck) return;

            _nextSelectionCheck = Time.unscaledTime + selectionCheckDelay;

            // Check if we lost selection (common when clicking background)
            var current = EventSystem.current?.currentSelectedGameObject;

            if (current == null || !current.activeInHierarchy)
            {
                // Try to restore last selection, or select first button
                if (_lastSelected != null && _lastSelected.gameObject.activeInHierarchy && _lastSelected.interactable)
                {
                    SelectButton(_lastSelected);
                }
                else if (firstSelected != null && firstSelected.gameObject.activeInHierarchy && firstSelected.interactable)
                {
                    SelectButton(firstSelected);
                }
                else if (_buttons.Count > 0)
                {
                    // Find first active interactable button
                    foreach (var btn in _buttons)
                    {
                        if (btn != null && btn.gameObject.activeInHierarchy && btn.interactable)
                        {
                            SelectButton(btn);
                            break;
                        }
                    }
                }
            }
            else
            {
                // Track current selection
                var selectable = current.GetComponent<Selectable>();
                if (selectable != null)
                {
                    _lastSelected = selectable;
                }
            }
        }

        private void SetupNavigation()
        {
            _buttons.Clear();

            // Find all selectable children
            var selectables = GetComponentsInChildren<Selectable>(true);
            foreach (var s in selectables)
            {
                if (s.gameObject != gameObject) // Don't include self
                {
                    _buttons.Add(s);
                }
            }

            if (_buttons.Count == 0) return;

            // Set up navigation based on mode
            for (int i = 0; i < _buttons.Count; i++)
            {
                var nav = _buttons[i].navigation;
                nav.mode = navigationMode;

                if (navigationMode == Navigation.Mode.Explicit)
                {
                    // Set up explicit navigation
                    if (navigationMode == Navigation.Mode.Vertical || navigationMode == Navigation.Mode.Explicit)
                    {
                        nav.selectOnUp = i > 0 ? _buttons[i - 1] : _buttons[_buttons.Count - 1];
                        nav.selectOnDown = i < _buttons.Count - 1 ? _buttons[i + 1] : _buttons[0];
                    }

                    if (navigationMode == Navigation.Mode.Horizontal || navigationMode == Navigation.Mode.Explicit)
                    {
                        nav.selectOnLeft = i > 0 ? _buttons[i - 1] : _buttons[_buttons.Count - 1];
                        nav.selectOnRight = i < _buttons.Count - 1 ? _buttons[i + 1] : _buttons[0];
                    }
                }

                _buttons[i].navigation = nav;
            }

            // Set first selected if not assigned
            if (firstSelected == null && _buttons.Count > 0)
            {
                firstSelected = _buttons[0];
            }
        }

        private void SelectButton(Selectable button)
        {
            if (button == null || EventSystem.current == null) return;

            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(button.gameObject);
            _lastSelected = button;
        }

        /// <summary>
        /// Call this after dynamically adding/removing buttons.
        /// </summary>
        public void RefreshNavigation()
        {
            SetupNavigation();
        }

        /// <summary>
        /// Manually select a specific button.
        /// </summary>
        public void Select(Selectable button)
        {
            SelectButton(button);
        }
    }
}
