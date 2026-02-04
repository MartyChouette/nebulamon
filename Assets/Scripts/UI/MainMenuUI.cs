using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nebula
{
    /// <summary>
    /// Accessible menu controller:
    /// - Switches Main <-> Options panels
    /// - Ensures a valid selected UI element (keyboard/gamepad friendly)
    /// - Supports a Back/Cancel action (Escape / B) to close Options
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject optionsPanel;

        [Header("Default Focus (important for accessibility)")]
        [SerializeField] private Selectable mainFirstSelected;     // e.g. Start button
        [SerializeField] private Selectable optionsFirstSelected;  // e.g. Master Volume slider or Back button

        [Header("Save Slots (optional)")]
        [SerializeField] private SaveSlotPickerUI slotPicker;

        [Header("Scene")]
        [SerializeField] private string startSceneName = "Game";

        [Header("Input System (UI Cancel / Back)")]
        [Tooltip("Reference your UI Cancel/Back action (typically Escape / Gamepad B).")]
        [SerializeField] private InputActionReference cancelAction;

        [Header("Quit")]
        [SerializeField] private bool showQuitInEditorAsStop = true;

        private void OnEnable()
        {
            // Make sure you always have a selected element when the scene loads.
            ShowMain();

            if (cancelAction != null)
                cancelAction.action.Enable();
        }

        private void OnDisable()
        {
            if (cancelAction != null)
                cancelAction.action.Disable();
        }

        private void Update()
        {
            if (cancelAction == null) return;

            if (cancelAction.action.WasPressedThisFrame())
            {
                // If options open, back closes it. If main menu is open, do nothing.
                if (optionsPanel != null && optionsPanel.activeSelf)
                    ShowMain();
            }
        }

        // Hook these up to your UI Buttons
        public void OnStartPressed()
        {
            if (string.IsNullOrWhiteSpace(startSceneName))
            {
                Debug.LogError($"{nameof(MainMenuUI)}: startSceneName is empty.");
                return;
            }

            if (slotPicker != null)
            {
                slotPicker.Show(slot =>
                {
                    Progression.LoadSlot(slot);
                    SceneManager.LoadScene(startSceneName);
                }, allowEmpty: true);
                return;
            }

            SceneManager.LoadScene(startSceneName);
        }

        public void OnOptionsPressed() => ShowOptions();

        public void OnBackPressed() => ShowMain();

        public void OnQuitPressed()
        {
#if UNITY_EDITOR
            if (showQuitInEditorAsStop)
                UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void ShowMain()
        {
            if (mainPanel != null) mainPanel.SetActive(true);
            if (optionsPanel != null) optionsPanel.SetActive(false);

            SetSelected(mainFirstSelected);
        }

        public void ShowOptions()
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(true);

            SetSelected(optionsFirstSelected);
        }

        private static void SetSelected(Selectable selectable)
        {
            if (selectable == null) return;

            // Clear + set on next frame helps avoid �selection didn�t stick� issues.
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
            }
        }
    }
}
