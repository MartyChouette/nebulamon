using UnityEngine;

namespace Nebula
{
    /// <summary>
    /// Interface for data that can be displayed on a card.
    /// Provides a unified contract for the card display system.
    /// </summary>
    public interface ICardData
    {
        /// <summary>Name displayed on the card header.</summary>
        string DisplayName { get; }

        /// <summary>Primary visual for the card (portrait/sprite).</summary>
        Sprite CardSprite { get; }

        /// <summary>Optional background sprite layer. Null = no background.</summary>
        Sprite BackgroundSprite { get; }
    }

    /// <summary>
    /// Enum defining the types of cards the system can display.
    /// </summary>
    public enum CardType
    {
        Character,
        Monster,
        Ship
    }
}
