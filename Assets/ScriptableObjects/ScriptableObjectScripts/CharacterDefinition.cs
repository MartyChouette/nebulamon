using UnityEngine;

namespace Nebula
{
    /// <summary>
    /// Data definition for character/NPC profile cards.
    /// Used by the card display system to show character information during dialogue.
    /// </summary>
    [CreateAssetMenu(menuName = "Nebula/Cards/Character Definition", fileName = "CharacterDefinition")]
    public class CharacterDefinition : ScriptableObject, ICardData
    {
        // ICardData implementation
        public string DisplayName => displayName;
        public Sprite CardSprite => portrait;
        public Sprite BackgroundSprite => background;

        [Header("Identity")]
        [Tooltip("Display name shown on the card")]
        public string displayName = "Unknown";

        [Tooltip("Character's title or role (e.g., 'Merchant', 'Pilot', 'Engineer')")]
        public string title = "";

        [Tooltip("Unique ID for dialogue and progression lookups")]
        public string characterId = "";

        [Header("Visuals")]
        [Tooltip("Portrait sprite shown on the character card")]
        public Sprite portrait;

        [Tooltip("Optional background image behind the card (scene plate/vignette)")]
        public Sprite background;

        [Header("Presentation")]
        [Tooltip("Short description or flavor text")]
        [TextArea(2, 4)]
        public string description = "";

        [Tooltip("Character's home planet (for progression/romance tracking). -1 = no planet.")]
        public int homePlanetIndex = -1;

        [Header("Audio")]
        [Tooltip("Default chirp profile for dialogue")]
        public NPCChirpProfile chirpProfile;
    }
}
