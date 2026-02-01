using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

namespace Nebula
{
    public class DialogueManager : MonoBehaviour
    {
        [Header("Database")]
        [SerializeField] private DialogueDatabaseCSV database;

        [Header("UI")]
        [SerializeField] private DialogueUI ui;

        [Header("Input")]
        [SerializeField] private InputActionReference submitAction; // advance
        [SerializeField] private InputActionReference cancelAction; // optional close

        [Header("Typewriter")]
        [SerializeField] private float charsPerSecond = 45f;

        [Header("Chirps")]
        [SerializeField] private AudioSource chirpSource;
        [SerializeField] private int chirpEveryNChars = 3;

        [Header("Default Chirp Profiles")]
        [SerializeField] private List<NPCChirpProfile> profiles = new List<NPCChirpProfile>();

        [Header("Card Integration")]
        [Tooltip("If true, shows the speaker's character card when dialogue starts (if CharacterDefinition is assigned)")]
        [SerializeField] private bool showCharacterCardOnDialogue = true;

        private readonly Dictionary<string, NPCChirpProfile> _profileByName = new Dictionary<string, NPCChirpProfile>();

        private List<DialogueLine> _lines;
        private int _index;
        private Coroutine _typing;
        private bool _isOpen;
        private bool _lineFullyShown;
        private DialogueSpeaker _activeSpeaker;

        // ✅ Proper singleton
        public static DialogueManager Instance { get; private set; }

        // ✅ Prevent “Interact (X) also Submit (X)” from instantly skipping/closing
        private float _ignoreSubmitUntilUnscaled = 0f;
        private int _openedFrame = -999;
    
        private void Awake()
        {
            // Singleton
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[DialogueManager] Duplicate found on '{name}', destroying this one.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // Validate UI
            if (ui == null)
            {
                ui = FindFirstObjectByType<DialogueUI>(FindObjectsInactive.Include);
                Debug.LogError("[DialogueManager] UI is NOT assigned in inspector.", this);
            }
            else
            {
                if (ui.root == null)
                    Debug.LogError("[DialogueManager] DialogueUI.root is NULL. Assign it to your DialoguePanel object (NOT the whole Canvas).", ui);

                if (ui.speakerLabel == null)
                    Debug.LogWarning("[DialogueManager] DialogueUI.speakerLabel is NULL (speaker name won't show).", ui);

                if (ui.bodyText == null)
                    Debug.LogWarning("[DialogueManager] DialogueUI.bodyText is NULL (text won't show; dialogue may instantly advance).", ui);

                ui.SetVisible(false);
                ui.ClearChoices();
            }

            // Validate database
            if (database == null)
            {
                Debug.LogError("[DialogueManager] Database is NOT assigned in inspector.", this);
            }
            else if (database.csvFile == null)
            {
                Debug.LogError("[DialogueManager] Database.csvFile is NULL. Assign a CSV TextAsset.", database);
            }

            // Build chirp profile lookup
            _profileByName.Clear();
            for (int i = 0; i < profiles.Count; i++)
            {
                if (profiles[i] == null)
                {
                    // This is a common cause of SerializedObjectNotCreatableException spam
                    Debug.LogWarning($"[DialogueManager] profiles list has a NULL element at index {i}. Remove it in the inspector.", this);
                    continue;
                }
                _profileByName[profiles[i].name] = profiles[i];
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (Instance == this) Instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (ui == null)
            {
                ui = FindFirstObjectByType<DialogueUI>(FindObjectsInactive.Include);
                if (ui != null)
                {
                    ui.SetVisible(false);
                    ui.ClearChoices();
                }
            }
            if (chirpSource == null && ui != null)
            {
                chirpSource = ui.GetComponentInChildren<AudioSource>();
            }
        }

        private void OnEnable()
        {
            if (submitAction != null) submitAction.action.Enable();
            if (cancelAction != null) cancelAction.action.Enable();
        }

        private void OnDisable()
        {
            if (submitAction != null) submitAction.action.Disable();
            if (cancelAction != null) cancelAction.action.Disable();
        }

        private void Update()
        {
            if (!_isOpen) return;

            // ✅ Ignore submit/cancel for a tiny window right after opening
            if (Time.unscaledTime < _ignoreSubmitUntilUnscaled) return;

            // Extra guard: ignore submit on the exact frame we opened
            if (Time.frameCount == _openedFrame) return;

            if (submitAction != null && submitAction.action.WasPressedThisFrame())
            {
                if (!_lineFullyShown)
                {
                    // finish the current line instantly
                    FinishTyping();
                }
                else
                {
                    Next();
                }
            }

            if (cancelAction != null && cancelAction.action.WasPressedThisFrame())
            {
                // optional: allow cancel to close
                // Close();
            }
        }

        public void StartConversation(DialogueSpeaker speaker, string conversationId)
        {
            Debug.Log($"[DialogueManager] StartConversation speaker='{(speaker ? speaker.name : "null")}' convoId='{conversationId}'");

            if (database == null || ui == null)
            {
                Debug.LogError("[DialogueManager] database or ui not assigned.", this);
                return;
            }

            if (ui.root == null)
            {
                Debug.LogError("[DialogueManager] ui.root is NULL (DialogueUI.root must be assigned).", ui);
                return;
            }

            if (database.csvFile == null)
            {
                Debug.LogError("[DialogueManager] database.csvFile is NULL (assign your CSV TextAsset).", database);
                return;
            }

            if (string.IsNullOrWhiteSpace(conversationId))
            {
                Debug.LogError("[DialogueManager] conversationId is empty.", this);
                return;
            }

            // Make sure CSV is parsed
            database.BuildIfNeeded();

            if (!database.TryGetConversation(conversationId, out _lines) || _lines == null || _lines.Count == 0)
            {
                Debug.LogWarning($"[DialogueManager] conversation '{conversationId}' not found or empty in CSV '{database.csvFile.name}'.");
                return;
            }

            _activeSpeaker = speaker;
            _index = 0;
            _isOpen = true;

            // ✅ This is the crucial fix for your “press X but nothing happens” / instant skip
            _openedFrame = Time.frameCount;
            _ignoreSubmitUntilUnscaled = Time.unscaledTime + 0.12f;

            ui.SetVisible(true);
            ui.ClearChoices();

            // Show character card if speaker has one and setting is enabled
            if (showCharacterCardOnDialogue && speaker != null && speaker.characterDefinition != null)
            {
                if (CardDisplayManager.Instance != null)
                {
                    CardDisplayManager.Instance.ShowCharacterForDialogue(speaker.characterDefinition);
                }
            }

            // Kill any previous typing
            if (_typing != null) StopCoroutine(_typing);
            _typing = null;

            ShowLine(_lines[_index]);
        }

        private void ShowLine(DialogueLine line)
        {
            if (line == null)
            {
                Debug.LogWarning("[DialogueManager] ShowLine got a null line; closing.");
                Close();
                return;
            }

            ui.ClearChoices();

            if (ui.speakerLabel != null) ui.speakerLabel.text = line.speaker;
            if (ui.bodyText != null) ui.bodyText.text = "";

            // Choices line?
            if (TryParseChoiceLine(line.text, out var choices))
            {
                _lineFullyShown = true;
                if (ui.bodyText != null) ui.bodyText.text = "";

                SpawnChoiceButtons(choices);

                // ✅ prevent “Interact press” from immediately triggering Next() and skipping the choices
                _ignoreSubmitUntilUnscaled = Time.unscaledTime + 0.12f;
                return;
            }

            // Normal line
            _lineFullyShown = false;
            if (_typing != null) StopCoroutine(_typing);
            _typing = StartCoroutine(TypeRoutine(line));
        }

        private IEnumerator TypeRoutine(DialogueLine line)
        {
            string full = line.text ?? "";
            int chirpCounter = 0;

            if (ui.bodyText == null)
            {
                Debug.LogWarning("[DialogueManager] ui.bodyText is NULL; marking line fully shown (this can cause instant advance).", ui);
                _lineFullyShown = true;
                _typing = null;
                yield break;
            }

            ui.bodyText.text = "";
            for (int i = 0; i < full.Length; i++)
            {
                ui.bodyText.text += full[i];

                if (!char.IsWhiteSpace(full[i]))
                {
                    chirpCounter++;
                    if (chirpCounter >= Mathf.Max(1, chirpEveryNChars))
                    {
                        chirpCounter = 0;
                        PlayChirpForLine(line);
                    }
                }

                // Use realtime so timescale changes don't break dialogue
                yield return new WaitForSecondsRealtime(1f / Mathf.Max(1f, charsPerSecond));
            }

            _lineFullyShown = true;
            _typing = null;
        }

        private void FinishTyping()
        {
            if (_typing != null) StopCoroutine(_typing);
            _typing = null;

            if (_lines != null && _index >= 0 && _index < _lines.Count && ui.bodyText != null)
                ui.bodyText.text = _lines[_index].text ?? "";

            _lineFullyShown = true;
        }

        private void Next()
        {
            if (_lines == null) { Close(); return; }

            _index++;
            if (_index >= _lines.Count)
            {
                Close();
                return;
            }

            ShowLine(_lines[_index]);
        }

        private void Close()
        {
            Debug.Log("[DialogueManager] Close()");

            _isOpen = false;
            _lines = null;
            _activeSpeaker = null;

            if (_typing != null) StopCoroutine(_typing);
            _typing = null;

            if (ui != null)
            {
                ui.ClearChoices();
                ui.SetVisible(false);
            }

            // Notify card system that dialogue has ended
            if (CardDisplayManager.Instance != null)
            {
                CardDisplayManager.Instance.OnDialogueEnded();
            }
        }

        private void PlayChirpForLine(DialogueLine line)
        {
            if (chirpSource == null) return;

            NPCChirpProfile profile = null;

            // 1) explicit by name in CSV
            if (!string.IsNullOrWhiteSpace(line.chirpProfile) && _profileByName.TryGetValue(line.chirpProfile, out var found))
                profile = found;

            // 2) fallback to speaker default
            if (profile == null && _activeSpeaker != null)
                profile = _activeSpeaker.defaultChirpProfile;

            if (profile == null) return;

            var clip = profile.Pick();
            if (clip == null) return;

            chirpSource.pitch = profile.PickPitch();
            chirpSource.PlayOneShot(clip, 0.8f);
        }

        // ---- choices ----
        // Format: CHOICE: Label -> convo | Label2 -> convo2 | Bye -> END
        private bool TryParseChoiceLine(string text, out List<(string label, string next)> choices)
        {
            choices = null;
            if (string.IsNullOrWhiteSpace(text)) return false;

            text = text.Trim();
            if (!text.StartsWith("CHOICE:", System.StringComparison.OrdinalIgnoreCase))
                return false;

            string body = text.Substring("CHOICE:".Length).Trim();
            var parts = body.Split('|');

            choices = new List<(string label, string next)>();
            for (int i = 0; i < parts.Length; i++)
            {
                var p = parts[i].Trim();
                if (string.IsNullOrWhiteSpace(p)) continue;

                var arrow = p.Split(new[] { "->" }, System.StringSplitOptions.RemoveEmptyEntries);
                if (arrow.Length != 2) continue;

                string label = arrow[0].Trim();
                string next = arrow[1].Trim();

                choices.Add((label, next));
            }

            return choices.Count > 0;
        }

        private void SpawnChoiceButtons(List<(string label, string next)> choices)
        {
            if (ui.choicesRoot == null || ui.choiceButtonPrefab == null)
            {
                Debug.LogWarning("[DialogueManager] choicesRoot or choiceButtonPrefab not assigned.", ui);
                return;
            }

            for (int i = 0; i < choices.Count; i++)
            {
                var c = choices[i];

                var btn = Instantiate(ui.choiceButtonPrefab, ui.choicesRoot);
                var tmp = btn.GetComponentInChildren<TMP_Text>();
                if (tmp != null) tmp.text = c.label;

                btn.onClick.AddListener(() =>
                {
                    if (c.next.Equals("END", System.StringComparison.OrdinalIgnoreCase))
                    {
                        Close();
                    }
                    else
                    {
                        StartConversation(_activeSpeaker, c.next);
                    }
                });

                if (i == 0) btn.Select();
            }
        }
    }
}
