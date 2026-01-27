using System.Collections.Generic;
using UnityEngine;

namespace Nebula
{
    /// <summary>
    /// Data definition for ship profile cards.
    /// Used by the card display system to show ship information.
    /// </summary>
    [CreateAssetMenu(menuName = "Nebula/Cards/Ship Definition", fileName = "ShipDefinition")]
    public class ShipDefinition : ScriptableObject, ICardData
    {
        // ICardData implementation
        public string DisplayName => displayName;
        public Sprite CardSprite => shipSprite;
        public Sprite BackgroundSprite => background;

        [Header("Identity")]
        [Tooltip("Display name shown on the card")]
        public string displayName = "Unknown Vessel";

        [Tooltip("Ship class/type (e.g., 'Fighter', 'Freighter', 'Cruiser')")]
        public string shipClass = "";

        [Tooltip("Unique ID for save/load and lookup")]
        public string shipId = "";

        [Header("Visuals")]
        [Tooltip("Ship sprite shown on the card")]
        public Sprite shipSprite;

        [Tooltip("Optional background image behind the card")]
        public Sprite background;

        [Header("Stats")]
        [Tooltip("Hull/armor rating")]
        [Min(1)] public int hull = 100;

        [Tooltip("Shield capacity")]
        [Min(0)] public int shields = 50;

        [Tooltip("Speed rating")]
        [Min(1)] public int speed = 10;

        [Tooltip("Cargo capacity")]
        [Min(0)] public int cargo = 20;

        [Header("Presentation")]
        [Tooltip("Short description or flavor text")]
        [TextArea(2, 4)]
        public string description = "";

        [Header("Upgrades")]
        [Tooltip("List of upgrade IDs currently installed on this ship")]
        public List<UpgradeId> installedUpgrades = new();
    }
}
