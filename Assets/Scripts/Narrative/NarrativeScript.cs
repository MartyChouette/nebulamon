using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nebula
{
    /// <summary>
    /// Parsed narrative script data.
    /// Created from .story text files by NarrativeScriptParser.
    /// </summary>
    [CreateAssetMenu(menuName = "Nebula/Narrative/Story Script", fileName = "NewStory")]
    public class NarrativeScript : ScriptableObject
    {
        [Header("Source")]
        [Tooltip("The raw text file to parse (optional - can also paste directly)")]
        public TextAsset sourceFile;

        [TextArea(10, 30)]
        [Tooltip("Raw script text (parsed on play)")]
        public string scriptText;

        [Header("Asset References")]
        [Tooltip("Characters that can be referenced by name in this script")]
        public List<CharacterDefinition> characters = new();

        [Tooltip("Monsters that can be referenced by name in this script")]
        public List<MonsterDefinition> monsters = new();

        [Tooltip("Ships that can be referenced by name in this script")]
        public List<ShipDefinition> ships = new();

        [Tooltip("Enemies/battles that can be triggered by name")]
        public List<EnemyDefinition> enemies = new();

        /// <summary>
        /// Gets the script text from either the source file or the direct text field.
        /// </summary>
        public string GetScriptText()
        {
            if (sourceFile != null && !string.IsNullOrWhiteSpace(sourceFile.text))
                return sourceFile.text;
            return scriptText ?? "";
        }

        /// <summary>
        /// Find a character by name (case-insensitive).
        /// </summary>
        public CharacterDefinition FindCharacter(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            foreach (var c in characters)
            {
                if (c != null && c.displayName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return c;
                if (c != null && c.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return c;
            }
            return null;
        }

        /// <summary>
        /// Find a monster by name (case-insensitive).
        /// </summary>
        public MonsterDefinition FindMonster(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            foreach (var m in monsters)
            {
                if (m != null && m.displayName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return m;
                if (m != null && m.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return m;
            }
            return null;
        }

        /// <summary>
        /// Find a ship by name (case-insensitive).
        /// </summary>
        public ShipDefinition FindShip(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            foreach (var s in ships)
            {
                if (s != null && s.displayName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return s;
                if (s != null && s.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return s;
            }
            return null;
        }

        /// <summary>
        /// Find an enemy by name (case-insensitive).
        /// </summary>
        public EnemyDefinition FindEnemy(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            foreach (var e in enemies)
            {
                if (e != null && e.displayName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return e;
                if (e != null && e.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return e;
            }
            return null;
        }
    }

    /// <summary>
    /// A single command/action in the narrative.
    /// </summary>
    [Serializable]
    public class NarrativeCommand
    {
        public NarrativeCommandType type;
        public string target;      // Character/monster/ship name
        public string text;        // Dialogue text or parameter
        public string speaker;     // Speaker name for dialogue
        public float duration;     // For waits, fades, etc.
        public Vector2 position;   // For positioning (0-1 normalized)
        public string effect;      // Effect name (shake, flash, etc.)
        public Dictionary<string, string> parameters = new();
    }

    public enum NarrativeCommandType
    {
        // Dialogue
        Say,              // Speaker says text
        Narrate,          // Narrator text (no speaker)

        // Cards/Sprites
        ShowCharacter,
        HideCharacter,
        ShowMonster,
        HideMonster,
        ShowShip,
        HideShip,
        HideAll,

        // Sprite Animation
        MoveSprite,       // Move sprite to position
        AnimateSprite,    // Play sprite animation
        FlipSprite,       // Flip sprite horizontally

        // Screen Effects
        FadeIn,
        FadeOut,
        Flash,
        ShakeScreen,

        // Timing
        Wait,
        WaitForInput,

        // Battle
        StartBattle,

        // Flow
        Label,            // Jump target
        Jump,             // Jump to label
        End,              // End script

        // Audio
        PlaySound,
        PlayMusic,
        StopMusic,

        // Variables (for branching later)
        SetFlag,
        IfFlag,
    }
}
