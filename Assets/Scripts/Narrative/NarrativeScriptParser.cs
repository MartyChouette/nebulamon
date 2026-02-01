using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Nebula
{
    /// <summary>
    /// Parses simple narrative script text into commands.
    ///
    /// Format examples:
    ///   # This is a comment
    ///
    ///   [show character: Pirate]
    ///   [show monster: SpaceBeast position=right]
    ///   [fade in: 1.5]
    ///   [shake screen: 0.3]
    ///   [wait: 2]
    ///   [wait for input]
    ///   [battle: pirate_fight]
    ///
    ///   Pirate: "Halt! Stop right there!"
    ///   Player: "I don't think so!"
    ///   : "Narrator text without speaker"
    ///
    /// Text effects in dialogue:
    ///   "This is <shake>shaky</shake> text!"
    ///   "This is <wave>wavy</wave> text!"
    ///   "This is <rainbow>colorful</rainbow> text!"
    ///   "This is <slow>slower</slow> text!"
    ///   "This is <fast>faster</fast> text!"
    ///   "This is <pause=0.5>with a pause</pause>!"
    ///   "This is <color=#FF0000>red</color> text!"
    /// </summary>
    public static class NarrativeScriptParser
    {
        // Regex patterns
        private static readonly Regex CommandPattern = new Regex(
            @"^\s*\[([^\]]+)\]\s*$",
            RegexOptions.Compiled);

        private static readonly Regex DialoguePattern = new Regex(
            @"^\s*([^:]*)\s*:\s*""([^""]*)""\s*$",
            RegexOptions.Compiled);

        private static readonly Regex DialogueMultilinePattern = new Regex(
            @"^\s*([^:]*)\s*:\s*""([^""]*)$",
            RegexOptions.Compiled);

        /// <summary>
        /// Parse script text into a list of commands.
        /// </summary>
        public static List<NarrativeCommand> Parse(string scriptText)
        {
            var commands = new List<NarrativeCommand>();
            if (string.IsNullOrWhiteSpace(scriptText)) return commands;

            var lines = scriptText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            bool inMultilineDialogue = false;
            string multilineSpeaker = "";
            string multilineText = "";

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                if (trimmed.StartsWith("#")) continue;

                // Handle multiline dialogue continuation
                if (inMultilineDialogue)
                {
                    if (trimmed.EndsWith("\""))
                    {
                        multilineText += " " + trimmed.Substring(0, trimmed.Length - 1);
                        commands.Add(CreateDialogueCommand(multilineSpeaker, multilineText));
                        inMultilineDialogue = false;
                    }
                    else
                    {
                        multilineText += " " + trimmed;
                    }
                    continue;
                }

                // Try to parse as command [...]
                var cmdMatch = CommandPattern.Match(trimmed);
                if (cmdMatch.Success)
                {
                    var cmd = ParseCommand(cmdMatch.Groups[1].Value);
                    if (cmd != null) commands.Add(cmd);
                    continue;
                }

                // Try to parse as dialogue Speaker: "Text"
                var dlgMatch = DialoguePattern.Match(trimmed);
                if (dlgMatch.Success)
                {
                    string speaker = dlgMatch.Groups[1].Value.Trim();
                    string text = dlgMatch.Groups[2].Value;
                    commands.Add(CreateDialogueCommand(speaker, text));
                    continue;
                }

                // Try to parse as multiline dialogue start
                var multiMatch = DialogueMultilinePattern.Match(trimmed);
                if (multiMatch.Success)
                {
                    multilineSpeaker = multiMatch.Groups[1].Value.Trim();
                    multilineText = multiMatch.Groups[2].Value;
                    inMultilineDialogue = true;
                    continue;
                }

                // Unknown line - log warning
                Debug.LogWarning($"[NarrativeParser] Unknown line format at line {i + 1}: {trimmed}");
            }

            // Warn if script ended mid-multiline dialogue
            if (inMultilineDialogue)
            {
                Debug.LogWarning($"[NarrativeParser] Script ended with unclosed multiline dialogue (speaker: {multilineSpeaker}). Did you forget a closing quote?");
                commands.Add(CreateDialogueCommand(multilineSpeaker, multilineText));
            }

            return commands;
        }

        private static NarrativeCommand CreateDialogueCommand(string speaker, string text)
        {
            return new NarrativeCommand
            {
                type = string.IsNullOrWhiteSpace(speaker) ? NarrativeCommandType.Narrate : NarrativeCommandType.Say,
                speaker = speaker,
                text = text
            };
        }

        private static NarrativeCommand ParseCommand(string commandText)
        {
            // Parse: "command: argument param=value param2=value2"
            // Or: "command" (no arguments)

            commandText = commandText.Trim();

            // Split command from arguments
            string cmdName;
            string argsText = "";

            int colonIndex = commandText.IndexOf(':');
            if (colonIndex >= 0)
            {
                cmdName = commandText.Substring(0, colonIndex).Trim().ToLowerInvariant();
                argsText = commandText.Substring(colonIndex + 1).Trim();
            }
            else
            {
                // Check for space-separated
                int spaceIndex = commandText.IndexOf(' ');
                if (spaceIndex >= 0)
                {
                    cmdName = commandText.Substring(0, spaceIndex).Trim().ToLowerInvariant();
                    argsText = commandText.Substring(spaceIndex + 1).Trim();
                }
                else
                {
                    cmdName = commandText.ToLowerInvariant();
                }
            }

            // Parse parameters from argsText
            var (mainArg, parameters) = ParseArguments(argsText);

            var cmd = new NarrativeCommand
            {
                target = mainArg,
                parameters = parameters
            };

            // Map command names to types
            switch (cmdName)
            {
                // Show/Hide
                case "show character":
                case "showcharacter":
                case "show char":
                    cmd.type = NarrativeCommandType.ShowCharacter;
                    break;

                case "hide character":
                case "hidecharacter":
                case "hide char":
                    cmd.type = NarrativeCommandType.HideCharacter;
                    break;

                case "show monster":
                case "showmonster":
                    cmd.type = NarrativeCommandType.ShowMonster;
                    break;

                case "hide monster":
                case "hidemonster":
                    cmd.type = NarrativeCommandType.HideMonster;
                    break;

                case "show ship":
                case "showship":
                    cmd.type = NarrativeCommandType.ShowShip;
                    break;

                case "hide ship":
                case "hideship":
                    cmd.type = NarrativeCommandType.HideShip;
                    break;

                case "hide all":
                case "hideall":
                case "clear":
                    cmd.type = NarrativeCommandType.HideAll;
                    break;

                // Movement/Animation
                case "move":
                case "move sprite":
                    cmd.type = NarrativeCommandType.MoveSprite;
                    break;

                case "animate":
                case "anim":
                    cmd.type = NarrativeCommandType.AnimateSprite;
                    break;

                case "flip":
                    cmd.type = NarrativeCommandType.FlipSprite;
                    break;

                // Screen Effects
                case "fade in":
                case "fadein":
                    cmd.type = NarrativeCommandType.FadeIn;
                    cmd.duration = TryParseFloat(mainArg, 1f);
                    break;

                case "fade out":
                case "fadeout":
                    cmd.type = NarrativeCommandType.FadeOut;
                    cmd.duration = TryParseFloat(mainArg, 1f);
                    break;

                case "flash":
                    cmd.type = NarrativeCommandType.Flash;
                    cmd.duration = TryParseFloat(mainArg, 0.2f);
                    break;

                case "shake":
                case "shake screen":
                case "screenshake":
                    cmd.type = NarrativeCommandType.ShakeScreen;
                    cmd.duration = TryParseFloat(mainArg, 0.3f);
                    break;

                // Timing
                case "wait":
                case "pause":
                    cmd.type = NarrativeCommandType.Wait;
                    cmd.duration = TryParseFloat(mainArg, 1f);
                    break;

                case "wait for input":
                case "waitforinput":
                case "input":
                    cmd.type = NarrativeCommandType.WaitForInput;
                    break;

                // Battle
                case "battle":
                case "start battle":
                case "fight":
                    cmd.type = NarrativeCommandType.StartBattle;
                    break;

                // Audio
                case "sound":
                case "play sound":
                case "sfx":
                    cmd.type = NarrativeCommandType.PlaySound;
                    break;

                case "music":
                case "play music":
                case "bgm":
                    cmd.type = NarrativeCommandType.PlayMusic;
                    break;

                case "stop music":
                case "stopmusic":
                    cmd.type = NarrativeCommandType.StopMusic;
                    break;

                // Flow
                case "label":
                    cmd.type = NarrativeCommandType.Label;
                    break;

                case "jump":
                case "goto":
                    cmd.type = NarrativeCommandType.Jump;
                    break;

                case "end":
                case "stop":
                    cmd.type = NarrativeCommandType.End;
                    break;

                // Flags
                case "set":
                case "setflag":
                    cmd.type = NarrativeCommandType.SetFlag;
                    break;

                case "if":
                case "ifflag":
                    cmd.type = NarrativeCommandType.IfFlag;
                    break;

                default:
                    Debug.LogWarning($"[NarrativeParser] Unknown command: {cmdName}");
                    return null;
            }

            // Parse position if specified
            if (parameters.TryGetValue("position", out string posStr) ||
                parameters.TryGetValue("pos", out posStr) ||
                parameters.TryGetValue("at", out posStr))
            {
                cmd.position = ParsePosition(posStr);
            }

            // Parse effect if specified
            if (parameters.TryGetValue("effect", out string effectStr))
            {
                cmd.effect = effectStr;
            }

            return cmd;
        }

        private static (string mainArg, Dictionary<string, string> parameters) ParseArguments(string argsText)
        {
            var parameters = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(argsText))
                return ("", parameters);

            // Find key=value pairs
            var kvRegex = new Regex(@"(\w+)\s*=\s*(\S+)");
            var matches = kvRegex.Matches(argsText);

            foreach (Match m in matches)
            {
                parameters[m.Groups[1].Value.ToLowerInvariant()] = m.Groups[2].Value;
            }

            // Remove key=value pairs to get the main argument
            string mainArg = kvRegex.Replace(argsText, "").Trim();

            return (mainArg, parameters);
        }

        private static Vector2 ParsePosition(string posStr)
        {
            posStr = posStr.ToLowerInvariant().Trim();

            // Named positions
            switch (posStr)
            {
                case "left": return new Vector2(0.2f, 0.5f);
                case "right": return new Vector2(0.8f, 0.5f);
                case "center": return new Vector2(0.5f, 0.5f);
                case "top": return new Vector2(0.5f, 0.8f);
                case "bottom": return new Vector2(0.5f, 0.2f);
                case "topleft": return new Vector2(0.2f, 0.8f);
                case "topright": return new Vector2(0.8f, 0.8f);
                case "bottomleft": return new Vector2(0.2f, 0.2f);
                case "bottomright": return new Vector2(0.8f, 0.2f);
            }

            // Try to parse as x,y
            var parts = posStr.Split(',');
            if (parts.Length == 2 &&
                float.TryParse(parts[0].Trim(), out float x) &&
                float.TryParse(parts[1].Trim(), out float y))
            {
                return new Vector2(x, y);
            }

            return new Vector2(0.5f, 0.5f); // Default center
        }

        private static float TryParseFloat(string str, float defaultValue)
        {
            if (float.TryParse(str, out float result))
                return result;
            return defaultValue;
        }
    }
}
