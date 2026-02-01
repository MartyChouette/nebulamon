using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace Nebula
{
    /// <summary>
    /// Simple, direct battle story sequencer.
    /// Use coroutines to script story moments with readable commands.
    ///
    /// Example usage:
    ///     yield return story.ShowCharacter(captain);
    ///     yield return story.Say("Captain", "We've got company!");
    ///     yield return story.ShowMonster(enemyMonster);
    ///     yield return story.Wait(0.5f);
    ///     yield return story.HideCharacter();
    /// </summary>
    public class BattleStoryDirector : MonoBehaviour
    {
        public static BattleStoryDirector Instance { get; private set; }

        [Header("UI References")]
        [Tooltip("Text element for story dialogue (can be separate from battle UI)")]
        [SerializeField] private TMP_Text storyText;

        [Tooltip("Root object to show/hide for story text panel")]
        [SerializeField] private GameObject storyTextPanel;

        [Header("Timing")]
        [SerializeField] private float defaultTextSpeed = 45f;
        [SerializeField] private float defaultPauseBetweenLines = 0.3f;

        [Header("Input")]
        [SerializeField] private InputActionReference advanceAction;

        [Header("Audio")]
        [SerializeField] private AudioSource chirpSource;
        [SerializeField] private int chirpEveryNChars = 3;

        private bool _isPlaying;
        private bool _skipRequested;

        public bool IsPlaying => _isPlaying;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Start with story panel hidden
            if (storyTextPanel != null)
                storyTextPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (advanceAction != null) advanceAction.action.Enable();
        }

        private void OnDisable()
        {
            if (advanceAction != null) advanceAction.action.Disable();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #region Simple Commands (return IEnumerator for yield return chaining)

        /// <summary>Show a character card.</summary>
        public IEnumerator ShowCharacter(CharacterDefinition character, bool waitForCard = false)
        {
            if (CardDisplayManager.Instance != null && character != null)
            {
                CardDisplayManager.Instance.ShowCharacter(character);
            }
            if (waitForCard)
                yield return new WaitForSeconds(0.2f);
            else
                yield return null;
        }

        /// <summary>Hide the character card.</summary>
        public IEnumerator HideCharacter()
        {
            if (CardDisplayManager.Instance != null)
            {
                CardDisplayManager.Instance.HideCharacter();
            }
            yield return null;
        }

        /// <summary>Show a monster card (definition).</summary>
        public IEnumerator ShowMonster(MonsterDefinition monster, bool waitForCard = false)
        {
            if (CardDisplayManager.Instance != null && monster != null)
            {
                CardDisplayManager.Instance.ShowMonster(monster);
            }
            if (waitForCard)
                yield return new WaitForSeconds(0.2f);
            else
                yield return null;
        }

        /// <summary>Show a monster card (instance with HP/status).</summary>
        public IEnumerator ShowMonster(MonsterInstance monster, bool waitForCard = false)
        {
            if (CardDisplayManager.Instance != null && monster != null)
            {
                CardDisplayManager.Instance.ShowMonster(monster);
            }
            if (waitForCard)
                yield return new WaitForSeconds(0.2f);
            else
                yield return null;
        }

        /// <summary>Hide the monster card.</summary>
        public IEnumerator HideMonster()
        {
            if (CardDisplayManager.Instance != null)
            {
                CardDisplayManager.Instance.HideMonster();
            }
            yield return null;
        }

        /// <summary>Show a ship card.</summary>
        public IEnumerator ShowShip(ShipDefinition ship, bool waitForCard = false)
        {
            if (CardDisplayManager.Instance != null && ship != null)
            {
                CardDisplayManager.Instance.ShowShip(ship);
            }
            if (waitForCard)
                yield return new WaitForSeconds(0.2f);
            else
                yield return null;
        }

        /// <summary>Hide the ship card.</summary>
        public IEnumerator HideShip()
        {
            if (CardDisplayManager.Instance != null)
            {
                CardDisplayManager.Instance.HideShip();
            }
            yield return null;
        }

        /// <summary>Hide all cards.</summary>
        public IEnumerator HideAllCards()
        {
            if (CardDisplayManager.Instance != null)
            {
                CardDisplayManager.Instance.HideAll();
            }
            yield return null;
        }

        /// <summary>Display story text with typewriter effect.</summary>
        public IEnumerator Say(string speaker, string text, NPCChirpProfile chirpProfile = null)
        {
            _isPlaying = true;
            _skipRequested = false;

            if (storyTextPanel != null)
                storyTextPanel.SetActive(true);

            if (storyText != null)
            {
                // Show speaker name if provided
                string prefix = string.IsNullOrEmpty(speaker) ? "" : $"<b>{speaker}:</b> ";
                yield return TypeText(prefix + text, chirpProfile);
            }

            // Wait for player input or auto-advance
            yield return WaitForAdvance();

            _isPlaying = false;
        }

        /// <summary>Display text instantly (no typewriter).</summary>
        public IEnumerator SayInstant(string speaker, string text)
        {
            if (storyTextPanel != null)
                storyTextPanel.SetActive(true);

            if (storyText != null)
            {
                string prefix = string.IsNullOrEmpty(speaker) ? "" : $"<b>{speaker}:</b> ";
                storyText.text = prefix + text;
            }

            yield return WaitForAdvance();
        }

        /// <summary>Clear and hide the story text.</summary>
        public IEnumerator ClearText()
        {
            if (storyText != null)
                storyText.text = "";

            if (storyTextPanel != null)
                storyTextPanel.SetActive(false);

            yield return null;
        }

        /// <summary>Wait for a duration.</summary>
        public IEnumerator Wait(float seconds)
        {
            yield return new WaitForSeconds(seconds);
        }

        /// <summary>Wait for player input (any key/button).</summary>
        public IEnumerator WaitForInput()
        {
            yield return WaitForAdvance();
        }

        #endregion

        #region Sequence Helpers

        /// <summary>
        /// Play a sequence of story steps.
        /// </summary>
        public IEnumerator PlaySequence(params Func<IEnumerator>[] steps)
        {
            foreach (var step in steps)
            {
                if (step != null)
                    yield return step();
            }
        }

        /// <summary>
        /// Run a custom action (for flexibility).
        /// </summary>
        public IEnumerator Do(Action action)
        {
            action?.Invoke();
            yield return null;
        }

        #endregion

        #region Internal

        private IEnumerator TypeText(string text, NPCChirpProfile chirpProfile)
        {
            if (storyText == null) yield break;

            storyText.text = "";
            int chirpCounter = 0;

            for (int i = 0; i < text.Length; i++)
            {
                // Skip on input
                if (_skipRequested)
                {
                    storyText.text = text;
                    yield break;
                }

                storyText.text += text[i];

                // Chirp
                if (!char.IsWhiteSpace(text[i]) && chirpProfile != null && chirpSource != null)
                {
                    chirpCounter++;
                    if (chirpCounter >= chirpEveryNChars)
                    {
                        chirpCounter = 0;
                        var clip = chirpProfile.Pick();
                        if (clip != null)
                        {
                            chirpSource.pitch = chirpProfile.PickPitch();
                            chirpSource.PlayOneShot(clip, 0.6f);
                        }
                    }
                }

                yield return new WaitForSecondsRealtime(1f / Mathf.Max(1f, defaultTextSpeed));
            }
        }

        private IEnumerator WaitForAdvance()
        {
            yield return new WaitForSeconds(defaultPauseBetweenLines);

            // Wait for any input
            while (advanceAction == null || !advanceAction.action.WasPressedThisFrame())
            {
                yield return null;
            }

            // Small debounce
            yield return new WaitForSecondsRealtime(0.05f);
        }

        /// <summary>Call this to skip current typewriter text.</summary>
        public void RequestSkip()
        {
            _skipRequested = true;
        }

        #endregion
    }
}
