using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;

namespace Nebula
{
    /// <summary>
    /// Plays narrative scripts with all effects.
    /// This is the main component for running story sequences.
    ///
    /// Usage:
    ///   1. Create a NarrativeScript asset (or use a .txt file)
    ///   2. Assign character/monster/ship references
    ///   3. Call NarrativePlayer.Instance.Play(script)
    /// </summary>
    public class NarrativePlayer : MonoBehaviour
    {
        public static NarrativePlayer Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private NarrativeTextEffects textEffects;

        [Header("Typewriter Settings")]
        [SerializeField] private float defaultCharsPerSecond = 40f;
        [SerializeField] private float slowMultiplier = 0.5f;
        [SerializeField] private float fastMultiplier = 2f;

        [Header("Input")]
        [SerializeField] private InputActionReference advanceAction;

        [Header("Audio")]
        [SerializeField] private AudioSource chirpSource;
        [SerializeField] private NPCChirpProfile defaultChirp;
        [SerializeField] private int chirpEveryNChars = 3;

        [Header("Events")]
        public UnityEvent onScriptStart;
        public UnityEvent onScriptEnd;
        public UnityEvent<string> onBattleTriggered; // Passes enemy name

        // State
        private bool _isPlaying;
        private bool _skipRequested;
        private bool _waitingForInput;
        private NarrativeScript _currentScript;
        private List<NarrativeCommand> _commands;
        private int _commandIndex;
        private Dictionary<string, int> _labels = new();

        public bool IsPlaying => _isPlaying;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Find text effects if not assigned
            if (textEffects == null && dialogueText != null)
                textEffects = dialogueText.GetComponent<NarrativeTextEffects>();

            // Hide dialogue panel initially
            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (advanceAction != null) advanceAction.action.Enable();
        }

        private void OnDisable()
        {
            if (advanceAction != null) advanceAction.action.Disable();
        }

        private void Update()
        {
            // Handle skip/advance input
            if (_isPlaying && advanceAction != null && advanceAction.action.WasPressedThisFrame())
            {
                if (_waitingForInput)
                {
                    _waitingForInput = false;
                }
                else
                {
                    _skipRequested = true;
                }
            }
        }

        #region Public API

        /// <summary>
        /// Play a narrative script.
        /// </summary>
        public void Play(NarrativeScript script)
        {
            if (script == null)
            {
                Debug.LogError("[NarrativePlayer] Cannot play null script.");
                return;
            }

            _currentScript = script;
            string scriptText = script.GetScriptText();

            if (string.IsNullOrWhiteSpace(scriptText))
            {
                Debug.LogWarning("[NarrativePlayer] Script text is empty.");
                return;
            }

            _commands = NarrativeScriptParser.Parse(scriptText);
            if (_commands.Count == 0)
            {
                Debug.LogWarning("[NarrativePlayer] No commands parsed from script.");
                return;
            }

            // Build label index
            _labels.Clear();
            for (int i = 0; i < _commands.Count; i++)
            {
                if (_commands[i].type == NarrativeCommandType.Label)
                {
                    _labels[_commands[i].target.ToLowerInvariant()] = i;
                }
            }

            StartCoroutine(PlayRoutine());
        }

        /// <summary>
        /// Play a script from raw text.
        /// </summary>
        public void PlayText(string scriptText, NarrativeScript assetRefs = null)
        {
            _currentScript = assetRefs;
            _commands = NarrativeScriptParser.Parse(scriptText);

            if (_commands.Count == 0)
            {
                Debug.LogWarning("[NarrativePlayer] No commands parsed from text.");
                return;
            }

            // Build label index
            _labels.Clear();
            for (int i = 0; i < _commands.Count; i++)
            {
                if (_commands[i].type == NarrativeCommandType.Label)
                {
                    _labels[_commands[i].target.ToLowerInvariant()] = i;
                }
            }

            StartCoroutine(PlayRoutine());
        }

        /// <summary>
        /// Stop the current script.
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;
            StopAllCoroutines();
            HideDialogue();

            if (CardDisplayManager.Instance != null)
                CardDisplayManager.Instance.HideAll();
        }

        /// <summary>
        /// Skip to the end of the current text/wait.
        /// </summary>
        public void Skip()
        {
            _skipRequested = true;
        }

        #endregion

        #region Playback

        private IEnumerator PlayRoutine()
        {
            _isPlaying = true;
            _commandIndex = 0;

            onScriptStart?.Invoke();

            while (_isPlaying && _commandIndex < _commands.Count)
            {
                var cmd = _commands[_commandIndex];
                yield return ExecuteCommand(cmd);
                _commandIndex++;
            }

            _isPlaying = false;
            HideDialogue();

            onScriptEnd?.Invoke();
        }

        private IEnumerator ExecuteCommand(NarrativeCommand cmd)
        {
            switch (cmd.type)
            {
                // Dialogue
                case NarrativeCommandType.Say:
                    yield return ShowDialogue(cmd.speaker, cmd.text);
                    break;

                case NarrativeCommandType.Narrate:
                    yield return ShowDialogue("", cmd.text);
                    break;

                // Cards/Sprites
                case NarrativeCommandType.ShowCharacter:
                    ShowCharacter(cmd.target);
                    yield return new WaitForSeconds(0.15f);
                    break;

                case NarrativeCommandType.HideCharacter:
                    HideCharacter();
                    break;

                case NarrativeCommandType.ShowMonster:
                    ShowMonster(cmd.target);
                    yield return new WaitForSeconds(0.15f);
                    break;

                case NarrativeCommandType.HideMonster:
                    HideMonster();
                    break;

                case NarrativeCommandType.ShowShip:
                    ShowShip(cmd.target);
                    yield return new WaitForSeconds(0.15f);
                    break;

                case NarrativeCommandType.HideShip:
                    HideShip();
                    break;

                case NarrativeCommandType.HideAll:
                    HideAllCards();
                    break;

                // Screen Effects
                case NarrativeCommandType.FadeIn:
                    if (ScreenEffects.Instance != null)
                        yield return ScreenEffects.Instance.FadeIn(cmd.duration);
                    break;

                case NarrativeCommandType.FadeOut:
                    if (ScreenEffects.Instance != null)
                        yield return ScreenEffects.Instance.FadeOut(cmd.duration);
                    break;

                case NarrativeCommandType.Flash:
                    if (ScreenEffects.Instance != null)
                        yield return ScreenEffects.Instance.FlashAsync(cmd.duration);
                    break;

                case NarrativeCommandType.ShakeScreen:
                    if (ScreenEffects.Instance != null)
                        yield return ScreenEffects.Instance.ShakeAsync(cmd.duration);
                    break;

                // Timing
                case NarrativeCommandType.Wait:
                    yield return WaitWithSkip(cmd.duration);
                    break;

                case NarrativeCommandType.WaitForInput:
                    yield return WaitForInput();
                    break;

                // Battle
                case NarrativeCommandType.StartBattle:
                    _isPlaying = false; // Stop script
                    onBattleTriggered?.Invoke(cmd.target);
                    // The battle system should take over from here
                    break;

                // Flow
                case NarrativeCommandType.Jump:
                    if (_labels.TryGetValue(cmd.target.ToLowerInvariant(), out int labelIndex))
                    {
                        _commandIndex = labelIndex; // Will be incremented after this
                    }
                    else
                    {
                        Debug.LogWarning($"[NarrativePlayer] Label not found: {cmd.target}");
                    }
                    break;

                case NarrativeCommandType.End:
                    _isPlaying = false;
                    break;

                case NarrativeCommandType.Label:
                    // Labels are just markers, do nothing
                    break;
            }
        }

        #endregion

        #region Dialogue

        private IEnumerator ShowDialogue(string speaker, string text)
        {
            // Show panel
            if (dialoguePanel != null)
                dialoguePanel.SetActive(true);

            // Set speaker
            if (speakerText != null)
            {
                speakerText.text = speaker ?? "";
                speakerText.gameObject.SetActive(!string.IsNullOrEmpty(speaker));
            }

            // Parse and set text with effects
            string cleanText = text;
            if (textEffects != null)
            {
                cleanText = textEffects.SetTextWithEffects(text);
            }
            else
            {
                cleanText = NarrativeTextHelper.StripNarrativeTags(text);
            }

            // Typewriter effect
            yield return TypeText(cleanText, text);

            // Wait for input to advance
            yield return WaitForInput();
        }

        private IEnumerator TypeText(string cleanText, string originalText)
        {
            if (dialogueText == null) yield break;

            _skipRequested = false;

            // Check for <instant> tag
            if (originalText.Contains("<instant>"))
            {
                dialogueText.text = cleanText;
                dialogueText.maxVisibleCharacters = cleanText.Length;
                yield break;
            }

            dialogueText.text = cleanText;
            dialogueText.maxVisibleCharacters = 0;

            // Parse speed modifiers from the original text
            var speedZones = ParseSpeedZones(originalText);

            float charsPerSec = defaultCharsPerSecond;
            int chirpCounter = 0;

            for (int i = 0; i < cleanText.Length; i++)
            {
                if (_skipRequested)
                {
                    dialogueText.maxVisibleCharacters = cleanText.Length;
                    _skipRequested = false;
                    yield break;
                }

                dialogueText.maxVisibleCharacters = i + 1;

                // Chirp
                char c = cleanText[i];
                if (!char.IsWhiteSpace(c) && chirpSource != null && defaultChirp != null)
                {
                    chirpCounter++;
                    if (chirpCounter >= chirpEveryNChars)
                    {
                        chirpCounter = 0;
                        var clip = defaultChirp.Pick();
                        if (clip != null)
                        {
                            chirpSource.pitch = defaultChirp.PickPitch();
                            chirpSource.PlayOneShot(clip, 0.5f);
                        }
                    }
                }

                // Apply speed modifier for current character position
                float multiplier = GetSpeedMultiplier(speedZones, i);
                yield return new WaitForSecondsRealtime(1f / (charsPerSec * multiplier));
            }
        }

        private void HideDialogue()
        {
            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);

            if (dialogueText != null)
                dialogueText.text = "";

            if (textEffects != null)
                textEffects.ClearEffects();
        }

        #endregion

        #region Cards

        private void ShowCharacter(string name)
        {
            if (CardDisplayManager.Instance == null || _currentScript == null) return;

            var character = _currentScript.FindCharacter(name);
            if (character != null)
            {
                CardDisplayManager.Instance.ShowCharacter(character);
            }
            else
            {
                Debug.LogWarning($"[NarrativePlayer] Character not found: {name}");
            }
        }

        private void HideCharacter()
        {
            CardDisplayManager.Instance?.HideCharacter();
        }

        private void ShowMonster(string name)
        {
            if (CardDisplayManager.Instance == null || _currentScript == null) return;

            var monster = _currentScript.FindMonster(name);
            if (monster != null)
            {
                CardDisplayManager.Instance.ShowMonster(monster);
            }
            else
            {
                Debug.LogWarning($"[NarrativePlayer] Monster not found: {name}");
            }
        }

        private void HideMonster()
        {
            CardDisplayManager.Instance?.HideMonster();
        }

        private void ShowShip(string name)
        {
            if (CardDisplayManager.Instance == null || _currentScript == null) return;

            var ship = _currentScript.FindShip(name);
            if (ship != null)
            {
                CardDisplayManager.Instance.ShowShip(ship);
            }
            else
            {
                Debug.LogWarning($"[NarrativePlayer] Ship not found: {name}");
            }
        }

        private void HideShip()
        {
            CardDisplayManager.Instance?.HideShip();
        }

        private void HideAllCards()
        {
            CardDisplayManager.Instance?.HideAll();
        }

        #endregion

        #region Helpers

        private IEnumerator WaitWithSkip(float duration)
        {
            _skipRequested = false;
            float elapsed = 0;

            while (elapsed < duration && !_skipRequested)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            _skipRequested = false;
        }

        private IEnumerator WaitForInput()
        {
            _waitingForInput = true;
            _skipRequested = false;

            // Small delay to prevent instant skip
            yield return new WaitForSecondsRealtime(0.1f);

            while (_waitingForInput && !_skipRequested)
            {
                yield return null;
            }

            _waitingForInput = false;
            _skipRequested = false;

            // Small delay after input
            yield return new WaitForSecondsRealtime(0.05f);
        }

        private struct SpeedZone
        {
            public int startIndex;
            public int endIndex;
            public float multiplier;
        }

        private List<SpeedZone> ParseSpeedZones(string originalText)
        {
            var zones = new List<SpeedZone>();
            if (string.IsNullOrEmpty(originalText)) return zones;

            // Track clean-text index as we walk the original text
            int cleanIndex = 0;
            int i = 0;

            while (i < originalText.Length)
            {
                if (originalText[i] == '<')
                {
                    int tagEnd = originalText.IndexOf('>', i);
                    if (tagEnd == -1) { cleanIndex++; i++; continue; }

                    string tag = originalText.Substring(i, tagEnd - i + 1);
                    string tagLower = tag.ToLowerInvariant();

                    if (tagLower == "<slow>" || tagLower == "<fast>")
                    {
                        float mult = tagLower == "<slow>" ? slowMultiplier : fastMultiplier;
                        string closeTag = tagLower == "<slow>" ? "</slow>" : "</fast>";
                        int closeIdx = originalText.IndexOf(closeTag, tagEnd + 1, System.StringComparison.OrdinalIgnoreCase);
                        if (closeIdx != -1)
                        {
                            int startClean = cleanIndex;
                            string inner = originalText.Substring(tagEnd + 1, closeIdx - tagEnd - 1);
                            int innerCleanLen = NarrativeTextHelper.StripNarrativeTags(inner).Length;
                            zones.Add(new SpeedZone { startIndex = startClean, endIndex = startClean + innerCleanLen - 1, multiplier = mult });
                            cleanIndex += innerCleanLen;
                            i = closeIdx + closeTag.Length;
                            continue;
                        }
                    }

                    // Skip other tags (they don't contribute to clean text length)
                    i = tagEnd + 1;
                    continue;
                }

                cleanIndex++;
                i++;
            }

            return zones;
        }

        private static float GetSpeedMultiplier(List<SpeedZone> zones, int charIndex)
        {
            for (int i = 0; i < zones.Count; i++)
            {
                if (charIndex >= zones[i].startIndex && charIndex <= zones[i].endIndex)
                    return zones[i].multiplier;
            }
            return 1f;
        }

        #endregion
    }
}
