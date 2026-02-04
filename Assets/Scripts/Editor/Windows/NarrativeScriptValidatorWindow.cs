using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Nebula.Editor
{
    public class NarrativeScriptValidatorWindow : EditorWindow
    {
        private NarrativeScript _script;
        private string _rawText = "";
        private Vector2 _scrollPos;
        private List<ValidationResult> _results = new();
        private bool _hasValidated;

        private enum Severity { Pass, Warning, Error }

        private struct ValidationResult
        {
            public Severity severity;
            public string message;
        }

        [MenuItem("Nebula/Tools/Narrative Script Validator")]
        public static void ShowWindow()
        {
            var window = GetWindow<NarrativeScriptValidatorWindow>("Narrative Validator");
            window.minSize = new Vector2(400, 300);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Narrative Script Validator", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Input: NarrativeScript asset OR raw text
            _script = (NarrativeScript)EditorGUILayout.ObjectField("NarrativeScript Asset", _script, typeof(NarrativeScript), false);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Or paste raw script text:");
            _rawText = EditorGUILayout.TextArea(_rawText, GUILayout.MinHeight(80));

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Validate", GUILayout.Height(28)))
            {
                RunValidation();
            }

            // Results
            if (_hasValidated)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);

                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

                if (_results.Count == 0)
                {
                    var prevColor = GUI.color;
                    GUI.color = Color.green;
                    EditorGUILayout.LabelField("All checks passed!", EditorStyles.largeLabel);
                    GUI.color = prevColor;
                }

                foreach (var r in _results)
                {
                    Color color;
                    string prefix;
                    switch (r.severity)
                    {
                        case Severity.Error:
                            color = new Color(1f, 0.3f, 0.3f);
                            prefix = "ERROR";
                            break;
                        case Severity.Warning:
                            color = new Color(1f, 0.85f, 0.2f);
                            prefix = "WARN ";
                            break;
                        default:
                            color = new Color(0.3f, 1f, 0.3f);
                            prefix = "PASS ";
                            break;
                    }

                    var prevCol = GUI.color;
                    GUI.color = color;
                    EditorGUILayout.LabelField($"[{prefix}] {r.message}");
                    GUI.color = prevCol;
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void RunValidation()
        {
            _results.Clear();
            _hasValidated = true;

            string scriptText = "";

            if (_script != null)
            {
                scriptText = _script.GetScriptText();
            }
            else if (!string.IsNullOrWhiteSpace(_rawText))
            {
                scriptText = _rawText;
            }
            else
            {
                _results.Add(new ValidationResult { severity = Severity.Error, message = "No script provided. Assign a NarrativeScript asset or paste raw text." });
                return;
            }

            if (string.IsNullOrWhiteSpace(scriptText))
            {
                _results.Add(new ValidationResult { severity = Severity.Error, message = "Script text is empty." });
                return;
            }

            // 1. Syntax: try parsing
            var logHandler = new LogCapture();
            var oldLogger = Debug.unityLogger.logHandler;
            Debug.unityLogger.logHandler = logHandler;

            List<NarrativeCommand> commands = null;
            try
            {
                commands = NarrativeScriptParser.Parse(scriptText);
            }
            catch (System.Exception ex)
            {
                _results.Add(new ValidationResult { severity = Severity.Error, message = $"Parse exception: {ex.Message}" });
            }
            finally
            {
                Debug.unityLogger.logHandler = oldLogger;
            }

            // Check for parser warnings
            foreach (var log in logHandler.Warnings)
            {
                _results.Add(new ValidationResult { severity = Severity.Warning, message = log });
            }
            foreach (var log in logHandler.Errors)
            {
                _results.Add(new ValidationResult { severity = Severity.Error, message = log });
            }

            if (commands == null || commands.Count == 0)
            {
                if (_results.Count == 0)
                    _results.Add(new ValidationResult { severity = Severity.Warning, message = "Script parsed to zero commands." });
                return;
            }

            _results.Add(new ValidationResult { severity = Severity.Pass, message = $"Parsed {commands.Count} commands." });

            // 2. Unclosed multiline dialogue check
            CheckUnclosedDialogue(scriptText);

            // 3. Asset reference checks (only if NarrativeScript asset provided)
            if (_script != null)
            {
                CheckAssetReferences(commands);
            }

            // 4. Label/Jump consistency
            CheckLabelJumpConsistency(commands);

            // 5. Empty dialogue warnings
            CheckEmptyDialogue(commands);
        }

        private void CheckUnclosedDialogue(string text)
        {
            var lines = text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            bool inMultiline = false;
            int startLine = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#")) continue;

                if (inMultiline)
                {
                    if (trimmed.EndsWith("\""))
                        inMultiline = false;
                    continue;
                }

                // Check for multiline start: Speaker: "text without closing quote
                if (trimmed.Contains(":") && trimmed.Contains("\""))
                {
                    int firstQuote = trimmed.IndexOf('"');
                    int lastQuote = trimmed.LastIndexOf('"');
                    if (firstQuote == lastQuote)
                    {
                        inMultiline = true;
                        startLine = i + 1;
                    }
                }
            }

            if (inMultiline)
            {
                _results.Add(new ValidationResult { severity = Severity.Error, message = $"Unclosed multiline dialogue starting near line {startLine}. Missing closing quote." });
            }
            else
            {
                _results.Add(new ValidationResult { severity = Severity.Pass, message = "No unclosed multiline dialogue." });
            }
        }

        private void CheckAssetReferences(List<NarrativeCommand> commands)
        {
            int missingCount = 0;

            foreach (var cmd in commands)
            {
                if (string.IsNullOrWhiteSpace(cmd.target)) continue;

                switch (cmd.type)
                {
                    case NarrativeCommandType.ShowCharacter:
                        if (_script.FindCharacter(cmd.target) == null)
                        {
                            _results.Add(new ValidationResult { severity = Severity.Warning, message = $"Character '{cmd.target}' not found in asset reference list." });
                            missingCount++;
                        }
                        break;

                    case NarrativeCommandType.ShowMonster:
                        if (_script.FindMonster(cmd.target) == null)
                        {
                            _results.Add(new ValidationResult { severity = Severity.Warning, message = $"Monster '{cmd.target}' not found in asset reference list." });
                            missingCount++;
                        }
                        break;

                    case NarrativeCommandType.ShowShip:
                        if (_script.FindShip(cmd.target) == null)
                        {
                            _results.Add(new ValidationResult { severity = Severity.Warning, message = $"Ship '{cmd.target}' not found in asset reference list." });
                            missingCount++;
                        }
                        break;

                    case NarrativeCommandType.StartBattle:
                        if (_script.FindEnemy(cmd.target) == null)
                        {
                            _results.Add(new ValidationResult { severity = Severity.Warning, message = $"Enemy '{cmd.target}' not found in asset reference list." });
                            missingCount++;
                        }
                        break;
                }
            }

            if (missingCount == 0)
            {
                _results.Add(new ValidationResult { severity = Severity.Pass, message = "All asset references resolved." });
            }
        }

        private void CheckLabelJumpConsistency(List<NarrativeCommand> commands)
        {
            var labels = new HashSet<string>();
            var jumps = new HashSet<string>();

            foreach (var cmd in commands)
            {
                if (cmd.type == NarrativeCommandType.Label && !string.IsNullOrWhiteSpace(cmd.target))
                    labels.Add(cmd.target);
                if (cmd.type == NarrativeCommandType.Jump && !string.IsNullOrWhiteSpace(cmd.target))
                    jumps.Add(cmd.target);
            }

            // Missing labels (jump targets that don't exist)
            bool anyMissing = false;
            foreach (var j in jumps)
            {
                if (!labels.Contains(j))
                {
                    _results.Add(new ValidationResult { severity = Severity.Error, message = $"Jump target '{j}' has no matching label." });
                    anyMissing = true;
                }
            }

            // Unused labels
            foreach (var l in labels)
            {
                if (!jumps.Contains(l))
                {
                    _results.Add(new ValidationResult { severity = Severity.Warning, message = $"Label '{l}' is defined but never jumped to." });
                }
            }

            if (!anyMissing && jumps.Count > 0)
            {
                _results.Add(new ValidationResult { severity = Severity.Pass, message = "All jump targets have matching labels." });
            }
            else if (jumps.Count == 0 && labels.Count == 0)
            {
                _results.Add(new ValidationResult { severity = Severity.Pass, message = "No labels or jumps used." });
            }
        }

        private void CheckEmptyDialogue(List<NarrativeCommand> commands)
        {
            int emptyCount = 0;
            foreach (var cmd in commands)
            {
                if ((cmd.type == NarrativeCommandType.Say || cmd.type == NarrativeCommandType.Narrate) &&
                    string.IsNullOrWhiteSpace(cmd.text))
                {
                    emptyCount++;
                }
            }

            if (emptyCount > 0)
            {
                _results.Add(new ValidationResult { severity = Severity.Warning, message = $"{emptyCount} dialogue command(s) have empty text." });
            }
            else
            {
                _results.Add(new ValidationResult { severity = Severity.Pass, message = "No empty dialogue commands." });
            }
        }

        /// <summary>
        /// Captures Debug.Log/Warning/Error calls during parsing.
        /// </summary>
        private class LogCapture : ILogHandler
        {
            public List<string> Warnings = new();
            public List<string> Errors = new();
            private ILogHandler _default = Debug.unityLogger.logHandler;

            public void LogException(System.Exception exception, Object context)
            {
                Errors.Add($"Exception: {exception.Message}");
            }

            public void LogFormat(LogType logType, Object context, string format, params object[] args)
            {
                string msg = args != null && args.Length > 0 ? string.Format(format, args) : format;

                switch (logType)
                {
                    case LogType.Warning:
                        Warnings.Add(msg);
                        break;
                    case LogType.Error:
                    case LogType.Assert:
                    case LogType.Exception:
                        Errors.Add(msg);
                        break;
                }
            }
        }
    }
}
